using StorageFileApp.Application.DTOs;
using StorageFileApp.Application.UseCases;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Services;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Domain.Entities.ChunkEntity;
using Microsoft.Extensions.Logging;
using DomainChunkInfo = StorageFileApp.Domain.Services.ChunkInfo;

namespace StorageFileApp.Application.Services;

public class FileChunkingApplicationService(
    IFileRepository fileRepository,
    IChunkRepository chunkRepository,
    IStorageProviderRepository storageProviderRepository,
    IStorageService storageService,
    IFileChunkingDomainService chunkingService,
    IFileStreamingDomainService streamingService,
    IFileMergingDomainService mergingService,
    IFileIntegrityDomainService integrityService,
    IChunkOptimizationDomainService optimizationService,
    IStorageProviderFactory storageProviderFactory,
    IStorageStrategyService storageStrategyService,
    IUnitOfWork unitOfWork,
    ILogger<FileChunkingApplicationService> logger)
    : IFileChunkingUseCase
{
    private readonly IFileRepository _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
    private readonly IChunkRepository _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
    private readonly IStorageProviderRepository _storageProviderRepository = storageProviderRepository ?? throw new ArgumentNullException(nameof(storageProviderRepository));
    private readonly IStorageService _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    private readonly IFileChunkingDomainService _chunkingService = chunkingService ?? throw new ArgumentNullException(nameof(chunkingService));
    private readonly IFileStreamingDomainService _streamingService = streamingService ?? throw new ArgumentNullException(nameof(streamingService));
    private readonly IFileMergingDomainService _mergingService = mergingService ?? throw new ArgumentNullException(nameof(mergingService));
    private readonly IFileIntegrityDomainService _integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
    private readonly IChunkOptimizationDomainService _optimizationService = optimizationService ?? throw new ArgumentNullException(nameof(optimizationService));
    private readonly IStorageProviderFactory _storageProviderFactory = storageProviderFactory ?? throw new ArgumentNullException(nameof(storageProviderFactory));
    private readonly IStorageStrategyService _storageStrategyService = storageStrategyService ?? throw new ArgumentNullException(nameof(storageStrategyService));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ILogger<FileChunkingApplicationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<ChunkingResult> ChunkFileAsync(ChunkFileRequest request)
    {
        try
        {
            _logger.LogInformation("Starting file chunking process for file ID: {FileId}", request.FileId);
            
            var startTime = DateTime.UtcNow;
            
            // 1. Get file from repository
            var file = await _fileRepository.GetByIdAsync(request.FileId);
            if (file == null)
            {
                return new ChunkingResult(false, ErrorMessage: "File not found");
            }
            
            // 2. Get available storage providers
            var availableProviders = await _storageProviderRepository.GetAvailableProvidersAsync();
            _logger.LogInformation("Found {ProviderCount} available storage providers", availableProviders.Count());
            if (!availableProviders.Any())
            {
                _logger.LogWarning("No available storage providers found for chunking");
                return new ChunkingResult(false, ErrorMessage: "No available storage providers");
            }
            
            // 3. Calculate optimal chunk size
            var chunkSize = request.ChunkSize ?? _chunkingService.CalculateOptimalChunkSize(file.Size);
            _logger.LogInformation("Calculated chunk size: {ChunkSize} bytes", chunkSize);
            
            // 4. Create chunks using domain service
            var chunkInfos = _chunkingService.CalculateOptimalChunks(file.Size);
            _logger.LogInformation("Created {ChunkCount} chunk infos", chunkInfos.Count());
            
            // 5. Process file using streaming or traditional approach
            var chunks = new List<DTOs.ChunkInfo>();
            
            if (request.FileStream != null)
            {
                // Memory-efficient streaming processing with parallel execution
                _logger.LogInformation("Using memory-efficient streaming processing for file size: {FileSize} bytes", file.Size);
                
                var streamingResults = await _streamingService.ProcessFileStreamAsync(
                    request.FileStream, 
                    file.Size, 
                    availableProviders);
                
                // Process results in parallel
                var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
                var processingTasks = streamingResults.Select(async result =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        if (result.IsSuccess)
                        {
                            // Create FileChunk entity
                            var fileChunk = new FileChunk(
                                file.Id,
                                result.Order,
                                result.Size,
                                result.Checksum,
                                result.StorageProviderId
                            );
                            
                            // Get storage provider and service
                            var assignedProvider = availableProviders.FirstOrDefault(p => p.Id == result.StorageProviderId);
                            if (assignedProvider != null)
                            {
                                var storageService = _storageProviderFactory.GetStorageService(assignedProvider);
                                
                                // Store chunk data (in real implementation, this would use the actual chunk data)
                                var storeResult = await storageService.StoreChunkAsync(fileChunk, new byte[0]);
                                
                                if (storeResult)
                                {
                                    fileChunk.UpdateStatus(ChunkStatus.Stored);
                                    _logger.LogInformation("Successfully processed chunk {ChunkOrder} in {ProcessingTime}ms", 
                                        result.Order, result.ProcessingTime.TotalMilliseconds);
                                }
                                else
                                {
                                    fileChunk.UpdateStatus(ChunkStatus.Failed);
                                    _logger.LogError("Failed to store chunk {ChunkOrder}", result.Order);
                                }
                            }
                            else
                            {
                                fileChunk.UpdateStatus(ChunkStatus.Failed);
                                _logger.LogError("Storage provider {ProviderId} not found for chunk {ChunkOrder}", 
                                    result.StorageProviderId, result.Order);
                            }
                            
                            // Save to repository
                            await _chunkRepository.AddAsync(fileChunk);
                            
                            return new DTOs.ChunkInfo(
                                fileChunk.Id,
                                fileChunk.Order,
                                fileChunk.Size,
                                fileChunk.StorageProviderId,
                                fileChunk.Status,
                                fileChunk.CreatedAt
                            );
                        }
                        else
                        {
                            _logger.LogError("Failed to process chunk {ChunkOrder}: {ErrorMessage}", 
                                result.Order, result.ErrorMessage);
                            return null;
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                
                var chunkResults = await Task.WhenAll(processingTasks);
                chunks.AddRange(chunkResults.Where(c => c != null)!);
            }
            else if (request.FileBytes != null)
            {
                // Traditional processing for small files with parallel execution
                _logger.LogInformation("Using traditional processing for file size: {FileSize} bytes", file.Size);
                
                var providerIds = availableProviders.Select(p => p.Id).ToList();
                var fileChunks = _chunkingService.CreateChunks(file, chunkInfos, providerIds);
                
                // Process chunks in parallel
                var semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
                var processingTasks = fileChunks.Select(async chunk =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        // Calculate chunk data
                        var chunkData = new byte[chunk.Size];
                        var offset = chunk.Order * chunkSize;
                        Array.Copy(request.FileBytes, offset, chunkData, 0, chunk.Size);
                        
                        // Calculate chunk checksum
                        using var stream = new MemoryStream(chunkData);
                        var chunkChecksum = await _integrityService.CalculateFileChecksumAsync(stream);
                        
                        // Get the storage provider that was already assigned by domain service
                        var assignedProvider = availableProviders.FirstOrDefault(p => p.Id == chunk.StorageProviderId);
                        if (assignedProvider == null)
                        {
                            _logger.LogError("Assigned storage provider {ProviderId} not found for chunk {ChunkOrder}", 
                                chunk.StorageProviderId, chunk.Order);
                            return null;
                        }
                        
                        _logger.LogInformation("Using assigned provider: Chunk {ChunkOrder} -> Provider {ProviderName} (Type: {ProviderType})", 
                            chunk.Order, assignedProvider.Name, assignedProvider.Type);
                        
                        // Update chunk with checksum
                        var updatedChunk = new FileChunk(chunk.FileId, chunk.Order, chunk.Size, chunkChecksum, assignedProvider.Id);
                        
                        // Get the appropriate storage service for the assigned provider
                        var storageService = _storageProviderFactory.GetStorageService(assignedProvider);
                        
                        // Store chunk data to assigned storage provider
                        var storeResult = await storageService.StoreChunkAsync(updatedChunk, chunkData);
                        
                        if (storeResult)
                        {
                            updatedChunk.UpdateStatus(ChunkStatus.Stored);
                            _logger.LogInformation("Successfully stored chunk {ChunkId} to {ProviderName}", 
                                updatedChunk.Id, assignedProvider.Name);
                        }
                        else
                        {
                            updatedChunk.UpdateStatus(ChunkStatus.Failed);
                            _logger.LogError("Failed to store chunk {ChunkId} to {ProviderName}", 
                                updatedChunk.Id, assignedProvider.Name);
                        }
                        
                        // Save chunk to repository
                        await _chunkRepository.AddAsync(updatedChunk);
                        return new DTOs.ChunkInfo(updatedChunk.Id, updatedChunk.Order, updatedChunk.Size, updatedChunk.StorageProviderId, updatedChunk.Status, updatedChunk.CreatedAt);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                
                var chunkResults = await Task.WhenAll(processingTasks);
                chunks.AddRange(chunkResults.Where(c => c != null)!);
            }
            else
            {
                return new ChunkingResult(false, ErrorMessage: "Either FileStream or FileBytes must be provided");
            }
            
            // 6. Update file status
            file.UpdateStatus(FileStatus.Processing);
            await _fileRepository.UpdateAsync(file);
            
            // 7. Save changes
            await _unitOfWork.SaveChangesAsync();
            
            var processingTime = DateTime.UtcNow - startTime;
            
            _logger.LogInformation("File chunking process completed for file ID: {FileId} in {ProcessingTime}ms", 
                request.FileId, processingTime.TotalMilliseconds);
            
            return new ChunkingResult(true, Chunks: chunks, ProcessingTime: processingTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while chunking file ID: {FileId}", request.FileId);
            return new ChunkingResult(false, ErrorMessage: ex.Message);
        }
    }
    
    public async Task<MergingResult> MergeChunksAsync(MergeChunksRequest request)
    {
        try
        {
            _logger.LogInformation("Starting chunk merging process for file ID: {FileId}", request.FileId);
            
            var startTime = DateTime.UtcNow;
            
            // 1. Get file from repository
            var file = await _fileRepository.GetByIdAsync(request.FileId);
            if (file == null)
            {
                return new MergingResult(false, ErrorMessage: "File not found");
            }
            
            // 2. Get all chunks for the file
            var chunks = await _chunkRepository.GetByFileIdAsync(request.FileId);
            if (!chunks.Any())
            {
                return new MergingResult(false, ErrorMessage: "No chunks found for file");
            }
            
            // 3. Check if all chunks are stored
            var allStored = chunks.All(c => c.Status == ChunkStatus.Stored);
            if (!allStored)
            {
                return new MergingResult(false, ErrorMessage: "Not all chunks are stored yet");
            }
            
            // 4. Retrieve chunk data from storage
            var chunkDataList = new List<byte[]>();
            foreach (var chunk in chunks.OrderBy(c => c.Order))
            {
                var chunkData = await _storageService.RetrieveChunkAsync(chunk);
                if (chunkData == null)
                {
                    return new MergingResult(false, ErrorMessage: $"Failed to retrieve chunk {chunk.Order}");
                }
                chunkDataList.Add(chunkData);
            }
            
            // 5. Merge chunks using domain service
            var mergedData = _mergingService.MergeChunks(chunkDataList);
            
            // 6. Validate integrity
            var isValid = await _integrityService.ValidateFileIntegrityAsync(file, chunks, chunkDataList);
            if (!isValid)
            {
                return new MergingResult(false, ErrorMessage: "File integrity validation failed");
            }
            
            // 7. Save merged file
            var outputPath = request.OutputPath ?? Path.Combine(Path.GetTempPath(), file.Name);
            
            // Check if outputPath is a directory, if so, create a file inside it
            if (Directory.Exists(outputPath))
            {
                outputPath = Path.Combine(outputPath, file.Name);
            }
            
            // Ensure the directory exists
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
                _logger.LogInformation("Created output directory: {OutputDirectory}", outputDirectory);
            }
            
            await File.WriteAllBytesAsync(outputPath, mergedData);
            _logger.LogInformation("File saved to: {OutputPath}", outputPath);
            
            // 8. Update file status
            file.UpdateStatus(FileStatus.Available);
            await _fileRepository.UpdateAsync(file);
            await _unitOfWork.SaveChangesAsync();
            
            var processingTime = DateTime.UtcNow - startTime;
            
            _logger.LogInformation("Chunk merging process completed for file ID: {FileId} in {ProcessingTime}ms", 
                request.FileId, processingTime.TotalMilliseconds);
            
            return new MergingResult(true, OutputPath: outputPath, ProcessingTime: processingTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while merging chunks for file ID: {FileId}", request.FileId);
            return new MergingResult(false, ErrorMessage: ex.Message);
        }
    }
    
    public async Task<ChunkValidationResult> ValidateChunksAsync(ValidateChunksRequest request)
    {
        try
        {
            _logger.LogInformation("Starting chunk validation process for file ID: {FileId}", request.FileId);
            
            var startTime = DateTime.UtcNow;
            
            // 1. Get file from repository
            var file = await _fileRepository.GetByIdAsync(request.FileId);
            if (file == null)
            {
                return new ChunkValidationResult(false, ErrorMessage: "File not found");
            }
            
            // 2. Get all chunks for the file
            var chunks = await _chunkRepository.GetByFileIdAsync(request.FileId);
            if (!chunks.Any())
            {
                return new ChunkValidationResult(false, ErrorMessage: "No chunks found for file");
            }
            
            // 3. Validate each chunk
            var validationResults = new List<ChunkValidationInfo>();
            var allValid = true;
            
            foreach (var chunk in chunks.OrderBy(c => c.Order))
            {
                try
                {
                    // Check if chunk exists in storage
                    var exists = await _storageService.ChunkExistsAsync(chunk);
                    if (!exists)
                    {
                        validationResults.Add(new ChunkValidationInfo(
                            ChunkId: chunk.Id,
                            Order: chunk.Order,
                            IsValid: false,
                            ErrorMessage: "Chunk not found in storage"
                        ));
                        allValid = false;
                        continue;
                    }
                    
                    // Retrieve chunk data for validation
                    var chunkData = await _storageService.RetrieveChunkAsync(chunk);
                    if (chunkData == null)
                    {
                        validationResults.Add(new ChunkValidationInfo(
                            ChunkId: chunk.Id,
                            Order: chunk.Order,
                            IsValid: false,
                            ErrorMessage: "Failed to retrieve chunk data for validation"
                        ));
                        allValid = false;
                        continue;
                    }
                    
                    // Validate chunk integrity
                    var isValid = await _storageService.ValidateChunkIntegrityAsync(chunk, chunkData);
                    validationResults.Add(new ChunkValidationInfo(
                        ChunkId: chunk.Id,
                        Order: chunk.Order,
                        IsValid: isValid,
                        ErrorMessage: isValid ? null : "Chunk integrity validation failed"
                    ));
                    
                    if (!isValid)
                    {
                        allValid = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error validating chunk {ChunkId}", chunk.Id);
                    validationResults.Add(new ChunkValidationInfo(
                        ChunkId: chunk.Id,
                        Order: chunk.Order,
                        IsValid: false,
                        ErrorMessage: ex.Message
                    ));
                    allValid = false;
                }
            }
            
            var processingTime = DateTime.UtcNow - startTime;
            
            _logger.LogInformation("Chunk validation process completed for file ID: {FileId} in {ProcessingTime}ms. Valid: {IsValid}", 
                request.FileId, processingTime.TotalMilliseconds, allValid);
            
            return new ChunkValidationResult(true, AllChunksValid: allValid, 
                ValidationResults: validationResults, ProcessingTime: processingTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while validating chunks for file ID: {FileId}", request.FileId);
            return new ChunkValidationResult(false, ErrorMessage: ex.Message);
        }
    }
    
    public async Task<FileStatusResult> GetFileStatusAsync(GetFileStatusRequest request)
    {
        try
        {
            _logger.LogInformation("Getting file status for file ID: {FileId}", request.FileId);
            
            // 1. Get file from repository
            var file = await _fileRepository.GetByIdAsync(request.FileId);
            if (file == null)
            {
                return new FileStatusResult(false, ErrorMessage: "File not found");
            }
            
            // 2. Get chunks for the file
            var chunks = await _chunkRepository.GetByFileIdAsync(request.FileId);
            
            // 3. Determine overall file status based on chunks
            var fileStatus = DetermineFileStatus(file, chunks);
            
            // 4. Get additional status information
            var chunkCount = chunks.Count();
            var storedChunkCount = chunks.Count((c) => c.Status == ChunkStatus.Stored);
            var totalSize = chunks.Sum((c) => c.Size);
            
            var additionalInfo = new Dictionary<string, object>
            {
                ["ChunkCount"] = chunkCount,
                ["StoredChunkCount"] = storedChunkCount,
                ["TotalSize"] = totalSize,
                ["CreatedAt"] = file.CreatedAt,
                ["LastModifiedAt"] = file.UpdatedAt ?? file.CreatedAt
            };
            
            var result = new FileStatusResult(true, Status: fileStatus, AdditionalInfo: additionalInfo);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting file status for ID: {FileId}", request.FileId);
            return new FileStatusResult(false, ErrorMessage: ex.Message);
        }
    }
    
    private static FileStatus DetermineFileStatus(Domain.Entities.FileEntity.File file, IEnumerable<FileChunk> chunks)
    {
        var chunkList = chunks.ToList();
        
        if (!chunkList.Any())
        {
            return FileStatus.Pending;
        }
        
        var allStored = chunkList.All(c => c.Status == ChunkStatus.Stored);
        var anyFailed = chunkList.Any(c => c.Status == ChunkStatus.Failed);
        var anyProcessing = chunkList.Any(c => c.Status == ChunkStatus.Processing);
        
        if (allStored)
        {
            return FileStatus.Available;
        }
        
        if (anyFailed)
        {
            return FileStatus.Failed;
        }
        
        if (anyProcessing)
        {
            return FileStatus.Processing;
        }
        
        return FileStatus.Pending;
    }
}