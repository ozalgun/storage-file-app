using Microsoft.Extensions.Logging;
using Moq;
using StorageFileApp.Application.DTOs;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Application.Services;
using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Entities.FileEntity;
using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Domain.Services;
using StorageFileApp.Domain.ValueObjects;

namespace StorageFileApp.Application.Tests.Services;

public class FileChunkingApplicationServiceTests
{
    private readonly Mock<IFileRepository> _fileRepositoryMock;
    private readonly Mock<IChunkRepository> _chunkRepositoryMock;
    private readonly Mock<IStorageProviderRepository> _storageProviderRepositoryMock;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<IStorageProviderFactory> _storageProviderFactoryMock;
    private readonly Mock<IStorageStrategyService> _storageStrategyServiceMock;
    private readonly Mock<IFileChunkingDomainService> _chunkingServiceMock;
    private readonly Mock<IFileIntegrityDomainService> _integrityServiceMock;
    private readonly Mock<IFileValidationDomainService> _validationServiceMock;
    private readonly Mock<IStorageStrategyDomainService> _strategyServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<FileChunkingApplicationService>> _loggerMock;
    private readonly FileChunkingApplicationService _service;

    public FileChunkingApplicationServiceTests()
    {
        _fileRepositoryMock = new Mock<IFileRepository>();
        _chunkRepositoryMock = new Mock<IChunkRepository>();
        _storageProviderRepositoryMock = new Mock<IStorageProviderRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _storageProviderFactoryMock = new Mock<IStorageProviderFactory>();
        _storageStrategyServiceMock = new Mock<IStorageStrategyService>();
        _chunkingServiceMock = new Mock<IFileChunkingDomainService>();
        _integrityServiceMock = new Mock<IFileIntegrityDomainService>();
        _validationServiceMock = new Mock<IFileValidationDomainService>();
        _strategyServiceMock = new Mock<IStorageStrategyDomainService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<FileChunkingApplicationService>>();

        _service = new FileChunkingApplicationService(
            _fileRepositoryMock.Object,
            _chunkRepositoryMock.Object,
            _storageProviderRepositoryMock.Object,
            _storageServiceMock.Object,
            _storageProviderFactoryMock.Object,
            _storageStrategyServiceMock.Object,
            _chunkingServiceMock.Object,
            _integrityServiceMock.Object,
            _validationServiceMock.Object,
            _strategyServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessFileChunkingAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var file = CreateValidFile(fileId);
        var fileBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var storageProviders = CreateValidStorageProviders(2);
        var chunks = CreateValidChunks(fileId, 2);

        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync(file);
        _storageProviderRepositoryMock.Setup(x => x.GetActiveProvidersAsync())
            .ReturnsAsync(storageProviders);
        _chunkingServiceMock.Setup(x => x.CalculateOptimalChunkSize(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()))
            .Returns(4L);
        _chunkingServiceMock.Setup(x => x.CreateChunks(It.IsAny<File>(), It.IsAny<long>()))
            .Returns(chunks);
        _storageStrategyServiceMock.Setup(x => x.SelectStorageProviderAsync(It.IsAny<FileChunk>(), It.IsAny<IEnumerable<StorageProvider>>()))
            .ReturnsAsync(storageProviders.First());
        _storageProviderFactoryMock.Setup(x => x.GetStorageService(It.IsAny<StorageProvider>()))
            .Returns(_storageServiceMock.Object);
        _storageServiceMock.Setup(x => x.StoreChunkAsync(It.IsAny<FileChunk>(), It.IsAny<byte[]>()))
            .ReturnsAsync(true);
        _integrityServiceMock.Setup(x => x.CalculateFileChecksumAsync(It.IsAny<Stream>()))
            .ReturnsAsync("calculated-checksum");
        _chunkRepositoryMock.Setup(x => x.AddAsync(It.IsAny<FileChunk>()))
            .Returns(Task.CompletedTask);
        _fileRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<File>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var request = new ProcessFileChunkingRequest(fileId, fileBytes);

        // Act
        var result = await _service.ProcessFileChunkingAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.ChunkCount.Should().Be(2);
    }

    [Fact]
    public async Task ProcessFileChunkingAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var fileBytes = new byte[] { 1, 2, 3, 4 };

        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync((File?)null);

        var request = new ProcessFileChunkingRequest(fileId, fileBytes);

        // Act
        var result = await _service.ProcessFileChunkingAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("File not found");
    }

    [Fact]
    public async Task ProcessFileChunkingAsync_WithNoActiveProviders_ShouldReturnFailure()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var file = CreateValidFile(fileId);
        var fileBytes = new byte[] { 1, 2, 3, 4 };
        var emptyProviders = Enumerable.Empty<StorageProvider>();

        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync(file);
        _storageProviderRepositoryMock.Setup(x => x.GetActiveProvidersAsync())
            .ReturnsAsync(emptyProviders);

        var request = new ProcessFileChunkingRequest(fileId, fileBytes);

        // Act
        var result = await _service.ProcessFileChunkingAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("No active storage providers available");
    }

    [Fact]
    public async Task ProcessFileChunkingAsync_WithChunkStorageFailure_ShouldReturnFailure()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var file = CreateValidFile(fileId);
        var fileBytes = new byte[] { 1, 2, 3, 4 };
        var storageProviders = CreateValidStorageProviders(1);
        var chunks = CreateValidChunks(fileId, 1);

        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync(file);
        _storageProviderRepositoryMock.Setup(x => x.GetActiveProvidersAsync())
            .ReturnsAsync(storageProviders);
        _chunkingServiceMock.Setup(x => x.CalculateOptimalChunkSize(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>()))
            .Returns(4L);
        _chunkingServiceMock.Setup(x => x.CreateChunks(It.IsAny<File>(), It.IsAny<long>()))
            .Returns(chunks);
        _storageStrategyServiceMock.Setup(x => x.SelectStorageProviderAsync(It.IsAny<FileChunk>(), It.IsAny<IEnumerable<StorageProvider>>()))
            .ReturnsAsync(storageProviders.First());
        _storageProviderFactoryMock.Setup(x => x.GetStorageService(It.IsAny<StorageProvider>()))
            .Returns(_storageServiceMock.Object);
        _storageServiceMock.Setup(x => x.StoreChunkAsync(It.IsAny<FileChunk>(), It.IsAny<byte[]>()))
            .ReturnsAsync(false); // Simulate storage failure
        _integrityServiceMock.Setup(x => x.CalculateFileChecksumAsync(It.IsAny<Stream>()))
            .ReturnsAsync("calculated-checksum");

        var request = new ProcessFileChunkingRequest(fileId, fileBytes);

        // Act
        var result = await _service.ProcessFileChunkingAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to store chunk");
    }

    [Fact]
    public async Task MergeChunksAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var file = CreateValidFile(fileId);
        var chunks = CreateValidChunks(fileId, 2);
        var storageProvider = CreateValidStorageProvider();

        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync(file);
        _chunkRepositoryMock.Setup(x => x.GetByFileIdAsync(fileId))
            .ReturnsAsync(chunks);
        _storageProviderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(storageProvider);
        _storageProviderFactoryMock.Setup(x => x.GetStorageService(It.IsAny<StorageProvider>()))
            .Returns(_storageServiceMock.Object);
        _storageServiceMock.Setup(x => x.RetrieveChunkAsync(It.IsAny<FileChunk>()))
            .ReturnsAsync(new byte[] { 1, 2, 3, 4 });
        _integrityServiceMock.Setup(x => x.ValidateFileIntegrityAsync(It.IsAny<File>(), It.IsAny<IEnumerable<FileChunk>>(), It.IsAny<IEnumerable<byte[]>>()))
            .ReturnsAsync(true);
        _fileRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<File>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var request = new MergeChunksRequest(fileId, "/tmp/output.txt");

        // Act
        var result = await _service.MergeChunksAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.OutputPath.Should().Be("/tmp/output.txt");
    }

    [Fact]
    public async Task MergeChunksAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync((File?)null);

        var request = new MergeChunksRequest(fileId, "/tmp/output.txt");

        // Act
        var result = await _service.MergeChunksAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("File not found");
    }

    [Fact]
    public async Task MergeChunksAsync_WithNoChunks_ShouldReturnFailure()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var file = CreateValidFile(fileId);
        var emptyChunks = Enumerable.Empty<FileChunk>();

        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync(file);
        _chunkRepositoryMock.Setup(x => x.GetByFileIdAsync(fileId))
            .ReturnsAsync(emptyChunks);

        var request = new MergeChunksRequest(fileId, "/tmp/output.txt");

        // Act
        var result = await _service.MergeChunksAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("No chunks found for file");
    }

    [Fact]
    public async Task MergeChunksAsync_WithIntegrityValidationFailure_ShouldReturnFailure()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var file = CreateValidFile(fileId);
        var chunks = CreateValidChunks(fileId, 1);
        var storageProvider = CreateValidStorageProvider();

        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync(file);
        _chunkRepositoryMock.Setup(x => x.GetByFileIdAsync(fileId))
            .ReturnsAsync(chunks);
        _storageProviderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(storageProvider);
        _storageProviderFactoryMock.Setup(x => x.GetStorageService(It.IsAny<StorageProvider>()))
            .Returns(_storageServiceMock.Object);
        _storageServiceMock.Setup(x => x.RetrieveChunkAsync(It.IsAny<FileChunk>()))
            .ReturnsAsync(new byte[] { 1, 2, 3, 4 });
        _integrityServiceMock.Setup(x => x.ValidateFileIntegrityAsync(It.IsAny<File>(), It.IsAny<IEnumerable<FileChunk>>(), It.IsAny<IEnumerable<byte[]>>()))
            .ReturnsAsync(false);

        var request = new MergeChunksRequest(fileId, "/tmp/output.txt");

        // Act
        var result = await _service.MergeChunksAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("File integrity validation failed");
    }

    [Fact]
    public async Task ValidateChunksAsync_WithValidChunks_ShouldReturnSuccess()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var file = CreateValidFile(fileId);
        var chunks = CreateValidChunks(fileId, 2);
        var storageProvider = CreateValidStorageProvider();

        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync(file);
        _chunkRepositoryMock.Setup(x => x.GetByFileIdAsync(fileId))
            .ReturnsAsync(chunks);
        _storageProviderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(storageProvider);
        _storageProviderFactoryMock.Setup(x => x.GetStorageService(It.IsAny<StorageProvider>()))
            .Returns(_storageServiceMock.Object);
        _storageServiceMock.Setup(x => x.ChunkExistsAsync(It.IsAny<FileChunk>()))
            .ReturnsAsync(true);
        _storageServiceMock.Setup(x => x.ValidateChunkIntegrityAsync(It.IsAny<FileChunk>(), It.IsAny<byte[]>()))
            .ReturnsAsync(true);

        var request = new ValidateChunksRequest(fileId);

        // Act
        var result = await _service.ValidateChunksAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.ValidChunkCount.Should().Be(2);
        result.InvalidChunkCount.Should().Be(0);
    }

    [Fact]
    public async Task ValidateChunksAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync((File?)null);

        var request = new ValidateChunksRequest(fileId);

        // Act
        var result = await _service.ValidateChunksAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("File not found");
    }

    private static File CreateValidFile(Guid fileId)
    {
        var metadata = new FileMetadata("text/plain", "Test file", new Dictionary<string, string>());
        return new File("test-file.txt", 1024L, "abc123", metadata) { Id = fileId };
    }

    private static IEnumerable<FileChunk> CreateValidChunks(Guid fileId, int count)
    {
        var chunks = new List<FileChunk>();
        for (int i = 0; i < count; i++)
        {
            var chunk = new FileChunk(fileId, i, 512L, $"chunk{i}", Guid.NewGuid());
            chunk.UpdateStatus(ChunkStatus.Stored);
            chunks.Add(chunk);
        }
        return chunks;
    }

    private static IEnumerable<StorageProvider> CreateValidStorageProviders(int count)
    {
        var providers = new List<StorageProvider>();
        for (int i = 0; i < count; i++)
        {
            var provider = new StorageProvider($"Provider {i}", StorageProviderType.FileSystem, $"BasePath=test{i}");
            providers.Add(provider);
        }
        return providers;
    }

    private static StorageProvider CreateValidStorageProvider()
    {
        return new StorageProvider("Test Provider", StorageProviderType.FileSystem, "BasePath=test");
    }
}
