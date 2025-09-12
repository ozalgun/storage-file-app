using StorageFileApp.Application.DTOs;
using StorageFileApp.Application.UseCases;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Services;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Domain.Aggregates;
using StorageFileApp.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using FileEntity = StorageFileApp.Domain.Entities.FileEntity.File;
using DomainFileMetadata = StorageFileApp.Domain.ValueObjects.FileMetadata;

namespace StorageFileApp.Application.Services;

public class FileStorageApplicationService(
    IFileRepository fileRepository,
    IChunkRepository chunkRepository,
    IStorageProviderRepository storageProviderRepository,
    IStorageService storageService,
    IStorageProviderFactory storageProviderFactory,
    IFileChunkingDomainService chunkingService,
    IFileIntegrityDomainService integrityService,
    IFileValidationDomainService validationService,
    IFileChunkingUseCase chunkingUseCase,
    IUnitOfWork unitOfWork,
    ILogger<FileStorageApplicationService> logger)
    : IFileStorageUseCase
{
    private readonly IFileRepository _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
    private readonly IChunkRepository _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
    private readonly IStorageProviderRepository _storageProviderRepository = storageProviderRepository ?? throw new ArgumentNullException(nameof(storageProviderRepository));
    private readonly IStorageService _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    private readonly IStorageProviderFactory _storageProviderFactory = storageProviderFactory ?? throw new ArgumentNullException(nameof(storageProviderFactory));
    private readonly IFileChunkingDomainService _chunkingService = chunkingService ?? throw new ArgumentNullException(nameof(chunkingService));
    private readonly IFileIntegrityDomainService _integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
    private readonly IFileValidationDomainService _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
    private readonly IFileChunkingUseCase _chunkingUseCase = chunkingUseCase ?? throw new ArgumentNullException(nameof(chunkingUseCase));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ILogger<FileStorageApplicationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<FileStorageResult> StoreFileAsync(StoreFileRequest request, byte[] fileBytes, string? filePath = null)
    {
        try
        {
            _logger.LogInformation("Starting file storage process for file: {FileName}", request.FileName);
            
            // 1. Validate request
            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                return new FileStorageResult(false, ErrorMessage: "File name cannot be empty");
            }
            
            if (request.FileSize <= 0)
            {
                return new FileStorageResult(false, ErrorMessage: "File size must be greater than zero");
            }
            
            // 2. Create file metadata
            var metadata = new DomainFileMetadata(request.ContentType, request.Description);
            if (request.CustomProperties != null)
            {
                foreach (var prop in request.CustomProperties)
                {
                    metadata.AddCustomProperty(prop.Key, prop.Value);
                }
            }
            
            // 3. Calculate checksum from file bytes or file path
            string checksum;
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                // Use file path for large files (memory efficient)
                using var fileStream = File.OpenRead(filePath);
                checksum = await _integrityService.CalculateFileChecksumAsync(fileStream);
            }
            else
            {
                // Use file bytes for small files
                using var stream = new MemoryStream(fileBytes);
                checksum = await _integrityService.CalculateFileChecksumAsync(stream);
            }
            
            // 4. Create file entity
            var file = new FileEntity(request.FileName, request.FileSize, checksum, metadata);
            
            // 5. Validate file
            var validationResult = await _validationService.ValidateFileForStorageAsync(file);
            if (!validationResult.IsValid)
            {
                return new FileStorageResult(false, ErrorMessage: string.Join(", ", validationResult.Errors));
            }
            
            // 6. Save file to repository
            await _fileRepository.AddAsync(file);
            await _unitOfWork.SaveChangesAsync();
            
            // 7. Process file chunking if file is large enough
            _logger.LogInformation("File size: {FileSize} bytes, threshold: {Threshold} bytes", file.Size, 1024 * 1024);
            if (file.Size >= 1024 * 1024) // 1MB threshold (changed to >=)
            {
                _logger.LogInformation("Starting chunking process for large file: {FileName}, Size: {FileSize}", 
                    request.FileName, file.Size);
                
                // Use real streaming approach for large files
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    // Use actual file stream for memory efficiency
                    using var fileStream = File.OpenRead(filePath);
                    var chunkingRequest = new ChunkFileRequest(file.Id, FileStream: fileStream);
                    var chunkingResult = await _chunkingUseCase.ChunkFileAsync(chunkingRequest);
                    
                    if (!chunkingResult.Success)
                    {
                        _logger.LogWarning("Chunking failed for file {FileName}: {ErrorMessage}", 
                            request.FileName, chunkingResult.ErrorMessage);
                        // Continue with file storage even if chunking fails
                    }
                    else
                    {
                        _logger.LogInformation("Chunking completed for file: {FileName}, Chunks: {ChunkCount}", 
                            request.FileName, chunkingResult.Chunks?.Count ?? 0);
                    }
                }
                else
                {
                    // Fallback to traditional approach if file path not available
                    _logger.LogWarning("File path not available, using traditional chunking approach");
                    var chunkingRequest = new ChunkFileRequest(file.Id, FileBytes: fileBytes);
                    var chunkingResult = await _chunkingUseCase.ChunkFileAsync(chunkingRequest);
                    
                    if (!chunkingResult.Success)
                    {
                        _logger.LogWarning("Chunking failed for file {FileName}: {ErrorMessage}", 
                            request.FileName, chunkingResult.ErrorMessage);
                        // Continue with file storage even if chunking fails
                    }
                    else
                    {
                        _logger.LogInformation("Chunking completed for file: {FileName}, Chunks: {ChunkCount}", 
                            request.FileName, chunkingResult.Chunks?.Count ?? 0);
                    }
                }
            }
            
            _logger.LogInformation("File storage process completed for file: {FileName}, ID: {FileId}", 
                request.FileName, file.Id);
            
            return new FileStorageResult(true, FileId: file.Id, Warnings: validationResult.Warnings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while storing file: {FileName}", request.FileName);
            return new FileStorageResult(false, ErrorMessage: ex.Message);
        }
    }
    
    public async Task<FileRetrievalResult> RetrieveFileAsync(RetrieveFileRequest request)
    {
        try
        {
            _logger.LogInformation("Starting file retrieval process for file ID: {FileId}", request.FileId);
            
            // 1. Get file from repository
            var file = await _fileRepository.GetByIdAsync(request.FileId);
            if (file == null)
            {
                return new FileRetrievalResult(false, ErrorMessage: "File not found");
            }
            
            // 2. Get all chunks for the file
            var chunks = await _chunkRepository.GetByFileIdAsync(request.FileId);
            if (!chunks.Any())
            {
                return new FileRetrievalResult(false, ErrorMessage: "No chunks found for file");
            }
            
            // 3. Check if all chunks are stored
            var allStored = await _chunkRepository.AreAllChunksStoredAsync(request.FileId);
            if (!allStored)
            {
                return new FileRetrievalResult(false, ErrorMessage: "File chunks are not fully stored");
            }
            
            // 4. Retrieve chunk data
            var chunkDataList = new List<byte[]>();
            foreach (var chunk in chunks.OrderBy(c => c.Order))
            {
                try
                {
                    // Get the storage provider for this chunk
                    var storageProvider = await _storageProviderRepository.GetByIdAsync(chunk.StorageProviderId);
                    if (storageProvider != null)
                    {
                        // Get the appropriate storage service for this provider
                        var chunkStorageService = _storageProviderFactory.GetStorageService(storageProvider);
                        var chunkData = await chunkStorageService.RetrieveChunkAsync(chunk);
                        if (chunkData == null)
                        {
                            _logger.LogError("Failed to retrieve chunk {ChunkId} from {ProviderType} storage provider {ProviderName}", 
                                chunk.Id, storageProvider.Type, storageProvider.Name);
                            return new FileRetrievalResult(false, ErrorMessage: $"Failed to retrieve chunk {chunk.Order} from {storageProvider.Name}");
                        }
                        chunkDataList.Add(chunkData);
                        _logger.LogDebug("Retrieved chunk {ChunkId} from {ProviderType} storage provider {ProviderName}", 
                            chunk.Id, storageProvider.Type, storageProvider.Name);
                    }
                    else
                    {
                        _logger.LogError("Storage provider {ProviderId} not found for chunk {ChunkId}", 
                            chunk.StorageProviderId, chunk.Id);
                        return new FileRetrievalResult(false, ErrorMessage: $"Storage provider not found for chunk {chunk.Order}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving chunk {ChunkId}", chunk.Id);
                    return new FileRetrievalResult(false, ErrorMessage: $"Error retrieving chunk {chunk.Order}: {ex.Message}");
                }
            }
            
            // 5. Merge chunks
            var mergedData = new List<byte>();
            foreach (var chunkData in chunkDataList)
            {
                mergedData.AddRange(chunkData);
            }
            
            // 6. Validate integrity
            var isValid = await _integrityService.ValidateFileIntegrityAsync(file, chunks, chunkDataList);
            if (!isValid)
            {
                return new FileRetrievalResult(false, ErrorMessage: "File integrity validation failed");
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
            
            await File.WriteAllBytesAsync(outputPath, mergedData.ToArray());
            _logger.LogInformation("File saved to: {OutputPath}", outputPath);
            
            _logger.LogInformation("File retrieval process completed for file ID: {FileId}", request.FileId);
            
            return new FileRetrievalResult(true, FilePath: outputPath, 
                FileMetadata: new DTOs.FileMetadata(file.Metadata.ContentType, file.Metadata.Description, file.Metadata.CustomProperties));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving file ID: {FileId}", request.FileId);
            return new FileRetrievalResult(false, ErrorMessage: ex.Message);
        }
    }
    
    public async Task<FileDeletionResult> DeleteFileAsync(DeleteFileRequest request)
    {
        try
        {
            _logger.LogInformation("Starting file deletion process for file ID: {FileId}", request.FileId);
            
            // 1. Get file from repository
            var file = await _fileRepository.GetByIdAsync(request.FileId);
            if (file == null)
            {
                return new FileDeletionResult(false, ErrorMessage: "File not found");
            }
            
            // 2. Get all chunks for the file
            var chunks = await _chunkRepository.GetByFileIdAsync(request.FileId);
            
            // 3. Delete chunks from storage
            foreach (var chunk in chunks)
            {
                try
                {
                    // Get the storage provider for this chunk
                    var storageProvider = await _storageProviderRepository.GetByIdAsync(chunk.StorageProviderId);
                    if (storageProvider != null)
                    {
                        // Get the appropriate storage service for this provider
                        var chunkStorageService = _storageProviderFactory.GetStorageService(storageProvider);
                        await chunkStorageService.DeleteChunkAsync(chunk);
                        _logger.LogInformation("Deleted chunk {ChunkId} from {ProviderType} storage provider {ProviderName}", 
                            chunk.Id, storageProvider.Type, storageProvider.Name);
                    }
                    else
                    {
                        _logger.LogWarning("Storage provider {ProviderId} not found for chunk {ChunkId}", 
                            chunk.StorageProviderId, chunk.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete chunk {ChunkId} from storage", chunk.Id);
                    // Continue with other chunks even if one fails
                }
            }
            
            // 4. Delete chunks from repository
            foreach (var chunk in chunks)
            {
                await _chunkRepository.DeleteAsync(chunk);
            }
            
            // 5. Delete file from repository
            await _fileRepository.DeleteAsync(file);
            
            // 6. Save changes
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("File deletion process completed for file ID: {FileId}", request.FileId);
            
            return new FileDeletionResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting file ID: {FileId}", request.FileId);
            return new FileDeletionResult(false, ErrorMessage: ex.Message);
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
            var storedChunkCount = chunks.Count((c) => c.Status == Domain.Enums.ChunkStatus.Stored);
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
    
    private static FileStatus DetermineFileStatus(FileEntity file, IEnumerable<Domain.Entities.ChunkEntity.FileChunk> chunks)
    {
        var chunkList = chunks.ToList();
        
        if (!chunkList.Any())
        {
            return FileStatus.Pending;
        }
        
        var allStored = chunkList.All(c => c.Status == Domain.Enums.ChunkStatus.Stored);
        var anyFailed = chunkList.Any(c => c.Status == Domain.Enums.ChunkStatus.Failed);
        var anyProcessing = chunkList.Any(c => c.Status == Domain.Enums.ChunkStatus.Processing);
        
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
    
    public async Task<FileListResult> ListFilesAsync(ListFilesRequest request)
    {
        try
        {
            _logger.LogInformation("Listing files with page: {PageNumber}, size: {PageSize}", 
                request.PageNumber, request.PageSize);
            
            // 1. Get files from repository with pagination
            var (files, totalCount) = await _fileRepository.GetPagedAsync(request.PageNumber, request.PageSize);
            
            // 2. Convert to DTOs
            var fileDtos = new List<FileSummary>();
            foreach (var file in files)
            {
                var chunks = await _chunkRepository.GetByFileIdAsync(file.Id);
                var fileStatus = DetermineFileStatus(file, chunks);
                
                fileDtos.Add(new FileSummary(
                    Id: file.Id,
                    Name: file.Name,
                    Size: file.Size,
                    Status: fileStatus,
                    CreatedAt: file.CreatedAt,
                    UpdatedAt: file.CreatedAt // Using CreatedAt as UpdatedAt for now
                ));
            }
            
            var result = new FileListResult(true, Files: fileDtos, TotalCount: totalCount, 
                PageNumber: request.PageNumber, PageSize: request.PageSize);
            
            _logger.LogInformation("Listed {FileCount} files out of {TotalCount} total", 
                fileDtos.Count, totalCount);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while listing files");
            return new FileListResult(false, ErrorMessage: ex.Message);
        }
    }
}