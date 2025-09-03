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
using StorageFileApp.SharedKernel.Exceptions;

namespace StorageFileApp.Application.Tests.Services;

public class FileStorageApplicationServiceTests
{
    private readonly Mock<IFileRepository> _fileRepositoryMock;
    private readonly Mock<IChunkRepository> _chunkRepositoryMock;
    private readonly Mock<IStorageProviderRepository> _storageProviderRepositoryMock;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<IStorageProviderFactory> _storageProviderFactoryMock;
    private readonly Mock<IFileChunkingDomainService> _chunkingServiceMock;
    private readonly Mock<IFileIntegrityDomainService> _integrityServiceMock;
    private readonly Mock<IFileValidationDomainService> _validationServiceMock;
    private readonly Mock<IStorageStrategyDomainService> _strategyServiceMock;
    private readonly Mock<IFileChunkingUseCase> _chunkingUseCaseMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<FileStorageApplicationService>> _loggerMock;
    private readonly FileStorageApplicationService _service;

    public FileStorageApplicationServiceTests()
    {
        _fileRepositoryMock = new Mock<IFileRepository>();
        _chunkRepositoryMock = new Mock<IChunkRepository>();
        _storageProviderRepositoryMock = new Mock<IStorageProviderRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _storageProviderFactoryMock = new Mock<IStorageProviderFactory>();
        _chunkingServiceMock = new Mock<IFileChunkingDomainService>();
        _integrityServiceMock = new Mock<IFileIntegrityDomainService>();
        _validationServiceMock = new Mock<IFileValidationDomainService>();
        _strategyServiceMock = new Mock<IStorageStrategyDomainService>();
        _chunkingUseCaseMock = new Mock<IFileChunkingUseCase>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<FileStorageApplicationService>>();

        _service = new FileStorageApplicationService(
            _fileRepositoryMock.Object,
            _chunkRepositoryMock.Object,
            _storageProviderRepositoryMock.Object,
            _storageServiceMock.Object,
            _storageProviderFactoryMock.Object,
            _chunkingServiceMock.Object,
            _integrityServiceMock.Object,
            _validationServiceMock.Object,
            _strategyServiceMock.Object,
            _chunkingUseCaseMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RetrieveFileAsync_WithValidFileId_ShouldReturnSuccess()
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
        _chunkRepositoryMock.Setup(x => x.AreAllChunksStoredAsync(fileId))
            .ReturnsAsync(true);
        _storageProviderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(storageProvider);
        _storageProviderFactoryMock.Setup(x => x.GetStorageService(It.IsAny<StorageProvider>()))
            .Returns(_storageServiceMock.Object);
        _storageServiceMock.Setup(x => x.RetrieveChunkAsync(It.IsAny<FileChunk>()))
            .ReturnsAsync(new byte[] { 1, 2, 3, 4 });
        _integrityServiceMock.Setup(x => x.ValidateFileIntegrityAsync(It.IsAny<File>(), It.IsAny<IEnumerable<FileChunk>>(), It.IsAny<IEnumerable<byte[]>>()))
            .ReturnsAsync(true);

        var request = new RetrieveFileRequest(fileId, "/tmp/test.txt");

        // Act
        var result = await _service.RetrieveFileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.FilePath.Should().Be("/tmp/test.txt");
    }

    [Fact]
    public async Task RetrieveFileAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync((File?)null);

        var request = new RetrieveFileRequest(fileId, "/tmp/test.txt");

        // Act
        var result = await _service.RetrieveFileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("File not found");
    }

    [Fact]
    public async Task RetrieveFileAsync_WithNoChunks_ShouldReturnFailure()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var file = CreateValidFile(fileId);
        var emptyChunks = Enumerable.Empty<FileChunk>();

        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync(file);
        _chunkRepositoryMock.Setup(x => x.GetByFileIdAsync(fileId))
            .ReturnsAsync(emptyChunks);

        var request = new RetrieveFileRequest(fileId, "/tmp/test.txt");

        // Act
        var result = await _service.RetrieveFileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("No chunks found for file");
    }

    [Fact]
    public async Task RetrieveFileAsync_WithNotFullyStoredChunks_ShouldReturnFailure()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var file = CreateValidFile(fileId);
        var chunks = CreateValidChunks(fileId, 2);

        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync(file);
        _chunkRepositoryMock.Setup(x => x.GetByFileIdAsync(fileId))
            .ReturnsAsync(chunks);
        _chunkRepositoryMock.Setup(x => x.AreAllChunksStoredAsync(fileId))
            .ReturnsAsync(false);

        var request = new RetrieveFileRequest(fileId, "/tmp/test.txt");

        // Act
        var result = await _service.RetrieveFileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("File chunks are not fully stored");
    }

    [Fact]
    public async Task RetrieveFileAsync_WithChunkRetrievalFailure_ShouldReturnFailure()
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
        _chunkRepositoryMock.Setup(x => x.AreAllChunksStoredAsync(fileId))
            .ReturnsAsync(true);
        _storageProviderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(storageProvider);
        _storageProviderFactoryMock.Setup(x => x.GetStorageService(It.IsAny<StorageProvider>()))
            .Returns(_storageServiceMock.Object);
        _storageServiceMock.Setup(x => x.RetrieveChunkAsync(It.IsAny<FileChunk>()))
            .ReturnsAsync((byte[]?)null);

        var request = new RetrieveFileRequest(fileId, "/tmp/test.txt");

        // Act
        var result = await _service.RetrieveFileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to retrieve chunk");
    }

    [Fact]
    public async Task RetrieveFileAsync_WithIntegrityValidationFailure_ShouldReturnFailure()
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
        _chunkRepositoryMock.Setup(x => x.AreAllChunksStoredAsync(fileId))
            .ReturnsAsync(true);
        _storageProviderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(storageProvider);
        _storageProviderFactoryMock.Setup(x => x.GetStorageService(It.IsAny<StorageProvider>()))
            .Returns(_storageServiceMock.Object);
        _storageServiceMock.Setup(x => x.RetrieveChunkAsync(It.IsAny<FileChunk>()))
            .ReturnsAsync(new byte[] { 1, 2, 3, 4 });
        _integrityServiceMock.Setup(x => x.ValidateFileIntegrityAsync(It.IsAny<File>(), It.IsAny<IEnumerable<FileChunk>>(), It.IsAny<IEnumerable<byte[]>>()))
            .ReturnsAsync(false);

        var request = new RetrieveFileRequest(fileId, "/tmp/test.txt");

        // Act
        var result = await _service.RetrieveFileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("File integrity validation failed");
    }

    [Fact]
    public async Task DeleteFileAsync_WithValidFileId_ShouldReturnSuccess()
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
        _storageServiceMock.Setup(x => x.DeleteChunkAsync(It.IsAny<FileChunk>()))
            .ReturnsAsync(true);
        _chunkRepositoryMock.Setup(x => x.DeleteAsync(It.IsAny<FileChunk>()))
            .Returns(Task.CompletedTask);
        _fileRepositoryMock.Setup(x => x.DeleteAsync(It.IsAny<File>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var request = new DeleteFileRequest(fileId);

        // Act
        var result = await _service.DeleteFileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync((File?)null);

        var request = new DeleteFileRequest(fileId);

        // Act
        var result = await _service.DeleteFileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("File not found");
    }

    [Fact]
    public async Task DeleteFileAsync_WithChunkDeletionFailure_ShouldContinueAndReturnSuccess()
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
        _storageServiceMock.Setup(x => x.DeleteChunkAsync(It.IsAny<FileChunk>()))
            .ReturnsAsync(false); // Simulate deletion failure
        _chunkRepositoryMock.Setup(x => x.DeleteAsync(It.IsAny<FileChunk>()))
            .Returns(Task.CompletedTask);
        _fileRepositoryMock.Setup(x => x.DeleteAsync(It.IsAny<File>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var request = new DeleteFileRequest(fileId);

        // Act
        var result = await _service.DeleteFileAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(); // Should still succeed even if chunk deletion fails
    }

    [Fact]
    public async Task GetFileStatusAsync_WithValidFileId_ShouldReturnFileStatus()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var file = CreateValidFile(fileId);
        var chunks = CreateValidChunks(fileId, 2);

        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync(file);
        _chunkRepositoryMock.Setup(x => x.GetByFileIdAsync(fileId))
            .ReturnsAsync(chunks);

        var request = new GetFileStatusRequest(fileId);

        // Act
        var result = await _service.GetFileStatusAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.FileStatus.Should().NotBeNull();
        result.FileStatus!.FileId.Should().Be(fileId);
        result.FileStatus.FileName.Should().Be(file.Name);
        result.FileStatus.Status.Should().Be(file.Status.ToString());
    }

    [Fact]
    public async Task GetFileStatusAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        _fileRepositoryMock.Setup(x => x.GetByIdAsync(fileId))
            .ReturnsAsync((File?)null);

        var request = new GetFileStatusRequest(fileId);

        // Act
        var result = await _service.GetFileStatusAsync(request);

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

    private static StorageProvider CreateValidStorageProvider()
    {
        return new StorageProvider("Test Provider", StorageProviderType.FileSystem, "BasePath=test");
    }
}
