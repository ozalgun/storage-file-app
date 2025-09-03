using StorageFileApp.Domain.Entities.FileEntity;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Domain.ValueObjects;

namespace StorageFileApp.Domain.Tests.Entities;

public class SimpleFileTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateFile()
    {
        // Arrange
        var name = "test-file.txt";
        var size = 1024L;
        var checksum = "abc123";
        var metadata = new FileMetadata("text/plain", "Test file");

        // Act
        var file = new Domain.Entities.FileEntity.File(name, size, checksum, metadata);

        // Assert
        Assert.Equal(name, file.Name);
        Assert.Equal(size, file.Size);
        Assert.Equal(checksum, file.Checksum);
        Assert.Equal(FileStatus.Pending, file.Status);
        Assert.Equal(metadata, file.Metadata);
        Assert.NotEqual(Guid.Empty, file.Id);
        Assert.True(file.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        string? name = null;
        var size = 1024L;
        var checksum = "abc123";
        var metadata = new FileMetadata("text/plain", "Test file");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Domain.Entities.FileEntity.File(name!, size, checksum, metadata));
    }

    [Fact]
    public void Constructor_WithNegativeSize_ShouldAcceptNegativeSize()
    {
        // Arrange
        var name = "test-file.txt";
        var size = -1L;
        var checksum = "abc123";
        var metadata = new FileMetadata("text/plain", "Test file");

        // Act
        var file = new Domain.Entities.FileEntity.File(name, size, checksum, metadata);

        // Assert
        Assert.Equal(size, file.Size);
    }

    [Fact]
    public void UpdateStatus_WithValidStatus_ShouldUpdateStatus()
    {
        // Arrange
        var file = CreateValidFile();
        var newStatus = FileStatus.Available;

        // Act
        file.UpdateStatus(newStatus);

        // Assert
        Assert.Equal(newStatus, file.Status);
        Assert.True(file.UpdatedAt.HasValue);
    }

    [Fact]
    public void MarkAsAvailable_ShouldSetStatusToAvailable()
    {
        // Arrange
        var file = CreateValidFile();

        // Act
        file.MarkAsAvailable();

        // Assert
        Assert.Equal(FileStatus.Available, file.Status);
        Assert.True(file.UpdatedAt.HasValue);
    }

    [Fact]
    public void MarkAsFailed_ShouldSetStatusToFailed()
    {
        // Arrange
        var file = CreateValidFile();

        // Act
        file.MarkAsFailed();

        // Assert
        Assert.Equal(FileStatus.Failed, file.Status);
        Assert.True(file.UpdatedAt.HasValue);
    }

    [Fact]
    public void UpdateMetadata_WithValidMetadata_ShouldUpdateMetadata()
    {
        // Arrange
        var file = CreateValidFile();
        var newMetadata = new FileMetadata("application/pdf", "Updated description");

        // Act
        file.UpdateMetadata(newMetadata);

        // Assert
        Assert.Equal(newMetadata, file.Metadata);
        Assert.True(file.UpdatedAt.HasValue);
    }

    [Fact]
    public void UpdateMetadata_WithNullMetadata_ShouldThrowException()
    {
        // Arrange
        var file = CreateValidFile();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => file.UpdateMetadata(null!));
    }

    private static Domain.Entities.FileEntity.File CreateValidFile()
    {
        var metadata = new FileMetadata("text/plain", "Test file");
        return new Domain.Entities.FileEntity.File("test-file.txt", 1024L, "abc123", metadata);
    }
}
