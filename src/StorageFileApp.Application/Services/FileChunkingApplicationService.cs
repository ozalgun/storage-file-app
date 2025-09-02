using StorageFileApp.Application.DTOs;
using StorageFileApp.Application.UseCases;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Services;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Domain.Entities.ChunkEntity;
using Microsoft.Extensions.Logging;
using DomainChunkInfo = StorageFileApp.Domain.Services.ChunkInfo;

namespace StorageFileApp.Application.Services;

public class FileChunkingApplicationService : IFileChunkingUseCase
{
    private readonly IFileRepository _fileRepository;
    private readonly IChunkRepository _chunkRepository;
    private readonly IStorageProviderRepository _storageProviderRepository;
    private readonly IStorageService _storageService;
    private readonly IFileChunkingDomainService _chunkingService;
    private readonly IFileMergingDomainService _mergingService;
    private readonly IFileIntegrityDomainService _integrityService;
    private readonly IChunkOptimizationDomainService _optimizationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FileChunkingApplicationService> _logger;
    
    public FileChunkingApplicationService(
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
    {
        _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
        _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
        _storageProviderRepository = storageProviderRepository ?? throw new ArgumentNullException(nameof(storageProviderRepository));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _chunkingService = chunkingService ?? throw new ArgumentNullException(nameof(chunkingService));
        _mergingService = mergingService ?? throw new ArgumentNullException(nameof(mergingService));
        _integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
        _optimizationService = optimizationService ?? throw new ArgumentNullException(nameof(optimizationService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
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
    
    public Task<MergingResult> MergeChunksAsync(MergeChunksRequest request)
    {
        try
        {
            _logger.LogInformation("Starting chunk merging process for file ID: {FileId}", request.FileId);
            
            // TODO: Implement chunk merging logic
            var result = new MergingResult(true, OutputPath: "placeholder_path");
            
            _logger.LogInformation("Chunk merging process completed for file ID: {FileId}", request.FileId);
            
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while merging chunks for file ID: {FileId}", request.FileId);
            return Task.FromResult(new MergingResult(false, ErrorMessage: ex.Message));
        }
    }
    
    public Task<ChunkValidationResult> ValidateChunksAsync(ValidateChunksRequest request)
    {
        try
        {
            _logger.LogInformation("Starting chunk validation process for file ID: {FileId}", request.FileId);
            
            // TODO: Implement chunk validation logic
            var result = new ChunkValidationResult(true, AllChunksValid: true);
            
            _logger.LogInformation("Chunk validation process completed for file ID: {FileId}", request.FileId);
            
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while validating chunks for file ID: {FileId}", request.FileId);
            return Task.FromResult(new ChunkValidationResult(false, ErrorMessage: ex.Message));
        }
    }
}