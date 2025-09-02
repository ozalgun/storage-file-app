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
    IFileChunkingDomainService chunkingService,
    IFileIntegrityDomainService integrityService,
    IFileValidationDomainService validationService,
    IStorageStrategyDomainService strategyService,
    IUnitOfWork unitOfWork,
    ILogger<FileStorageApplicationService> logger)
    : IFileStorageUseCase
{
    private readonly IFileRepository _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
    private readonly IChunkRepository _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
    private readonly IStorageProviderRepository _storageProviderRepository = storageProviderRepository ?? throw new ArgumentNullException(nameof(storageProviderRepository));
    private readonly IStorageService _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
    private readonly IFileChunkingDomainService _chunkingService = chunkingService ?? throw new ArgumentNullException(nameof(chunkingService));
    private readonly IFileIntegrityDomainService _integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
    private readonly IFileValidationDomainService _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
    private readonly IStorageStrategyDomainService _strategyService = strategyService ?? throw new ArgumentNullException(nameof(strategyService));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ILogger<FileStorageApplicationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<FileStorageResult> StoreFileAsync(StoreFileRequest request)
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
            
            // 3. Create file entity
            var file = new FileEntity(request.FileName, request.FileSize, string.Empty, metadata);
            
            // 4. Validate file
            var validationResult = await _validationService.ValidateFileForStorageAsync(file);
            if (!validationResult.IsValid)
            {
                return new FileStorageResult(false, ErrorMessage: string.Join(", ", validationResult.Errors));
            }
            
            // 5. Save file to repository
            await _fileRepository.AddAsync(file);
            await _unitOfWork.SaveChangesAsync();
            
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
                var chunkData = await _storageService.RetrieveChunkAsync(chunk);
                if (chunkData == null)
                {
                    return new FileRetrievalResult(false, ErrorMessage: $"Failed to retrieve chunk {chunk.Order}");
                }
                chunkDataList.Add(chunkData);
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
            await File.WriteAllBytesAsync(outputPath, mergedData.ToArray());
            
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
    
    public Task<FileDeletionResult> DeleteFileAsync(DeleteFileRequest request)
    {
        try
        {
            _logger.LogInformation("Starting file deletion process for file ID: {FileId}", request.FileId);
            
            // TODO: Implement file deletion logic
            var result = new FileDeletionResult(true);
            
            _logger.LogInformation("File deletion process completed for file ID: {FileId}", request.FileId);
            
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting file ID: {FileId}", request.FileId);
            return Task.FromResult(new FileDeletionResult(false, ErrorMessage: ex.Message));
        }
    }
    
    public Task<FileStatusResult> GetFileStatusAsync(GetFileStatusRequest request)
    {
        try
        {
            _logger.LogInformation("Getting file status for file ID: {FileId}", request.FileId);
            
            // TODO: Implement file status retrieval
            var result = new FileStatusResult(true, Status: FileStatus.Pending);
            
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting file status for ID: {FileId}", request.FileId);
            return Task.FromResult(new FileStatusResult(false, ErrorMessage: ex.Message));
        }
    }
    
    public Task<FileListResult> ListFilesAsync(ListFilesRequest request)
    {
        try
        {
            _logger.LogInformation("Listing files with page: {PageNumber}, size: {PageSize}", 
                request.PageNumber, request.PageSize);
            
            // TODO: Implement file listing
            var result = new FileListResult(true, Files: [], TotalCount: 0, 
                PageNumber: request.PageNumber, PageSize: request.PageSize);
            
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while listing files");
            return Task.FromResult(new FileListResult(false, ErrorMessage: ex.Message));
        }
    }
}