using Microsoft.Extensions.Logging;
using Moq;
using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Infrastructure.Services;

namespace StorageFileApp.Infrastructure.Tests.Services;

public class FileSystemStorageServiceTests : IDisposable
{
    private readonly Mock<ILogger<FileSystemStorageService>> _loggerMock;
    private readonly string _testBasePath;
    private readonly FileSystemStorageService _service;

    public FileSystemStorageServiceTests()
    {
        _loggerMock = new Mock<ILogger<FileSystemStorageService>>();
        _testBasePath = Path.Combine(Path.GetTempPath(), "StorageFileAppTests", Guid.NewGuid().ToString());
        _service = new FileSystemStorageService(_loggerMock.Object, _testBasePath);
    }

    [Fact]
    public async Task StoreChunkAsync_WithValidChunk_ShouldStoreSuccessfully()
    {
        // Arrange
        var chunk = CreateValidChunk();
        var data = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var result = await _service.StoreChunkAsync(chunk, data);

        // Assert
        result.Should().BeTrue();
        
        var expectedPath = Path.Combine(_testBasePath, chunk.StorageProviderId.ToString(), chunk.FileId.ToString(), $"{chunk.Order:D6}.chunk");
        File.Exists(expectedPath).Should().BeTrue();
        
        var storedData = await File.ReadAllBytesAsync(expectedPath);
        storedData.Should().Equal(data);
    }

    [Fact]
    public async Task StoreChunkAsync_WithNullData_ShouldThrowException()
    {
        // Arrange
        var chunk = CreateValidChunk();
        byte[]? data = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.StoreChunkAsync(chunk, data!));
    }

    [Fact]
    public async Task StoreChunkAsync_WithEmptyData_ShouldStoreSuccessfully()
    {
        // Arrange
        var chunk = CreateValidChunk();
        var data = Array.Empty<byte>();

        // Act
        var result = await _service.StoreChunkAsync(chunk, data);

        // Assert
        result.Should().BeTrue();
        
        var expectedPath = Path.Combine(_testBasePath, chunk.StorageProviderId.ToString(), chunk.FileId.ToString(), $"{chunk.Order:D6}.chunk");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task RetrieveChunkAsync_WithExistingChunk_ShouldReturnData()
    {
        // Arrange
        var chunk = CreateValidChunk();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        await _service.StoreChunkAsync(chunk, data);

        // Act
        var result = await _service.RetrieveChunkAsync(chunk);

        // Assert
        result.Should().NotBeNull();
        result.Should().Equal(data);
    }

    [Fact]
    public async Task RetrieveChunkAsync_WithNonExistentChunk_ShouldReturnNull()
    {
        // Arrange
        var chunk = CreateValidChunk();

        // Act
        var result = await _service.RetrieveChunkAsync(chunk);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteChunkAsync_WithExistingChunk_ShouldDeleteSuccessfully()
    {
        // Arrange
        var chunk = CreateValidChunk();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        await _service.StoreChunkAsync(chunk, data);

        var expectedPath = Path.Combine(_testBasePath, chunk.StorageProviderId.ToString(), chunk.FileId.ToString(), $"{chunk.Order:D6}.chunk");
        File.Exists(expectedPath).Should().BeTrue();

        // Act
        var result = await _service.DeleteChunkAsync(chunk);

        // Assert
        result.Should().BeTrue();
        File.Exists(expectedPath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteChunkAsync_WithNonExistentChunk_ShouldReturnTrue()
    {
        // Arrange
        var chunk = CreateValidChunk();

        // Act
        var result = await _service.DeleteChunkAsync(chunk);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ChunkExistsAsync_WithExistingChunk_ShouldReturnTrue()
    {
        // Arrange
        var chunk = CreateValidChunk();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        await _service.StoreChunkAsync(chunk, data);

        // Act
        var result = await _service.ChunkExistsAsync(chunk);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ChunkExistsAsync_WithNonExistentChunk_ShouldReturnFalse()
    {
        // Arrange
        var chunk = CreateValidChunk();

        // Act
        var result = await _service.ChunkExistsAsync(chunk);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetChunkSizeAsync_WithExistingChunk_ShouldReturnCorrectSize()
    {
        // Arrange
        var chunk = CreateValidChunk();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        await _service.StoreChunkAsync(chunk, data);

        // Act
        var result = await _service.GetChunkSizeAsync(chunk);

        // Assert
        result.Should().Be(data.Length);
    }

    [Fact]
    public async Task GetChunkSizeAsync_WithNonExistentChunk_ShouldReturnZero()
    {
        // Arrange
        var chunk = CreateValidChunk();

        // Act
        var result = await _service.GetChunkSizeAsync(chunk);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task ValidateChunkIntegrityAsync_WithValidChecksum_ShouldReturnTrue()
    {
        // Arrange
        var chunk = CreateValidChunk();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var checksum = CalculateSha256(data);
        chunk.UpdateChecksum(checksum);

        // Act
        var result = await _service.ValidateChunkIntegrityAsync(chunk, data);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateChunkIntegrityAsync_WithInvalidChecksum_ShouldReturnFalse()
    {
        // Arrange
        var chunk = CreateValidChunk();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        chunk.UpdateChecksum("invalid-checksum");

        // Act
        var result = await _service.ValidateChunkIntegrityAsync(chunk, data);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestProviderConnectionAsync_WithValidProvider_ShouldReturnTrue()
    {
        // Arrange
        var provider = CreateValidStorageProvider();

        // Act
        var result = await _service.TestProviderConnectionAsync(provider);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableSpaceAsync_WithValidProvider_ShouldReturnPositiveValue()
    {
        // Arrange
        var provider = CreateValidStorageProvider();

        // Act
        var result = await _service.GetAvailableSpaceAsync(provider);

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task IsProviderHealthyAsync_WithValidProvider_ShouldReturnTrue()
    {
        // Arrange
        var provider = CreateValidStorageProvider();

        // Act
        var result = await _service.IsProviderHealthyAsync(provider);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DistributeChunksAsync_WithValidChunks_ShouldReturnSameChunks()
    {
        // Arrange
        var chunks = new[]
        {
            CreateValidChunk(0),
            CreateValidChunk(1),
            CreateValidChunk(2)
        };

        // Act
        var result = await _service.DistributeChunksAsync(chunks);

        // Assert
        result.Should().Equal(chunks);
    }

    [Fact]
    public async Task ReplicateChunkAsync_WithValidChunk_ShouldReplicateSuccessfully()
    {
        // Arrange
        var sourceChunk = CreateValidChunk();
        var targetProvider = CreateValidStorageProvider();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        await _service.StoreChunkAsync(sourceChunk, data);

        // Act
        var result = await _service.ReplicateChunkAsync(sourceChunk, targetProvider);

        // Assert
        result.Should().BeTrue();
        
        // Verify the replicated file exists
        var replicatedPath = Path.Combine(_testBasePath, targetProvider.Id.ToString(), sourceChunk.FileId.ToString(), $"{sourceChunk.Order:D6}.chunk");
        File.Exists(replicatedPath).Should().BeTrue();
    }

    private static FileChunk CreateValidChunk(int order = 0)
    {
        return new FileChunk(Guid.NewGuid(), order, 1024L, "test-checksum", Guid.NewGuid());
    }

    private static StorageProvider CreateValidStorageProvider()
    {
        return new StorageProvider("Test Provider", StorageProviderType.FileSystem, "BasePath=test");
    }

    private static string CalculateSha256(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToHexString(hash);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, true);
        }
    }
}
