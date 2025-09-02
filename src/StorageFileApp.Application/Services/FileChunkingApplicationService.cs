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
    IFileMergingDomainService mergingService,
    IFileIntegrityDomainService integrityService,
    IChunkOptimizationDomainService optimizationService,
    IUnitOfWork unitOfWork,
    ILogger<FileChunkingApplicationService> logger)
    : IFileChunkingUseCase
{
    private readonly IFileRepository _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
    private readonly IChunkRepository _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
    private readonly IStorageProviderRepository _storageProviderRepository = storageProviderRepository ?? throw new ArgumentNullException(nameof(storageProviderRepository));
    private readonly IStorageService _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    private readonly IFileChunkingDomainService _chunkingService = chunkingService ?? throw new ArgumentNullException(nameof(chunkingService));
    private readonly IFileMergingDomainService _mergingService = mergingService ?? throw new ArgumentNullException(nameof(mergingService));
    private readonly IFileIntegrityDomainService _integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
    private readonly IChunkOptimizationDomainService _optimizationService = optimizationService ?? throw new ArgumentNullException(nameof(optimizationService));
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
            if (!availableProviders.Any())
            {
                return new ChunkingResult(false, ErrorMessage: "No available storage providers");
            }
            
            // 3. Calculate optimal chunk size
            var chunkSize = request.ChunkSize ?? _chunkingService.CalculateOptimalChunkSize(file.Size);
            
            // 4. Create chunks using domain service
            var chunkInfos = _chunkingService.CalculateOptimalChunks(file.Size);
            
            // 5. Save chunks to repository
            var chunks = new List<DTOs.ChunkInfo>();
            var providerIds = availableProviders.Select(p => p.Id).ToList();
            var fileChunks = _chunkingService.CreateChunks(file, chunkInfos, providerIds);
            
            foreach (var chunk in fileChunks)
            {
                await _chunkRepository.AddAsync(chunk);
                chunks.Add(new DTOs.ChunkInfo(chunk.Id, chunk.Order, chunk.Size, chunk.StorageProviderId, chunk.Status, chunk.CreatedAt));
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
            await File.WriteAllBytesAsync(outputPath, mergedData);
            
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