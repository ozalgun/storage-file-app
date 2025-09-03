using StorageFileApp.Domain.ValueObjects;

namespace StorageFileApp.Domain.Tests.ValueObjects;

public class SimpleFileMetadataTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateFileMetadata()
    {
        // Arrange
        var contentType = "text/plain";
        var description = "Test file description";

        // Act
        var metadata = new FileMetadata(contentType, description);

        // Assert
        Assert.Equal(contentType, metadata.ContentType);
        Assert.Equal(description, metadata.Description);
        Assert.NotNull(metadata.CustomProperties);
        Assert.Empty(metadata.CustomProperties);
    }

    [Fact]
    public void Constructor_WithNullDescription_ShouldCreateFileMetadata()
    {
        // Arrange
        var contentType = "text/plain";
        string? description = null;

        // Act
        var metadata = new FileMetadata(contentType, description);

        // Assert
        Assert.Equal(contentType, metadata.ContentType);
        Assert.Null(metadata.Description);
        Assert.NotNull(metadata.CustomProperties);
        Assert.Empty(metadata.CustomProperties);
    }

    [Fact]
    public void Constructor_WithEmptyContentType_ShouldThrowException()
    {
        // Arrange
        string? contentType = null;
        var description = "Test file description";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileMetadata(contentType!, description));
    }

    [Fact]
    public void AddCustomProperty_WithValidKeyValue_ShouldAddProperty()
    {
        // Arrange
        var metadata = new FileMetadata("text/plain", "Test file");
        var key = "author";
        var value = "John Doe";

        // Act
        metadata.AddCustomProperty(key, value);

        // Assert
        Assert.True(metadata.CustomProperties.ContainsKey(key));
        Assert.Equal(value, metadata.CustomProperties[key]);
    }

    [Fact]
    public void AddCustomProperty_WithExistingKey_ShouldUpdateProperty()
    {
        // Arrange
        var metadata = new FileMetadata("text/plain", "Test file");
        var key = "author";
        var originalValue = "John Doe";
        var newValue = "Jane Smith";

        metadata.AddCustomProperty(key, originalValue);

        // Act
        metadata.AddCustomProperty(key, newValue);

        // Assert
        Assert.Equal(newValue, metadata.CustomProperties[key]);
    }

    [Fact]
    public void AddCustomProperty_WithEmptyKey_ShouldThrowException()
    {
        // Arrange
        var metadata = new FileMetadata("text/plain", "Test file");
        var key = "";
        var value = "value";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => metadata.AddCustomProperty(key, value));
    }

    [Fact]
    public void AddCustomProperty_WithWhitespaceKey_ShouldThrowException()
    {
        // Arrange
        var metadata = new FileMetadata("text/plain", "Test file");
        var key = "   ";
        var value = "value";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => metadata.AddCustomProperty(key, value));
    }

    [Fact]
    public void RemoveCustomProperty_WithExistingKey_ShouldRemoveProperty()
    {
        // Arrange
        var metadata = new FileMetadata("text/plain", "Test file");
        var key = "author";
        var value = "John Doe";
        metadata.AddCustomProperty(key, value);

        // Act
        metadata.RemoveCustomProperty(key);

        // Assert
        Assert.False(metadata.CustomProperties.ContainsKey(key));
    }

    [Fact]
    public void RemoveCustomProperty_WithNonExistentKey_ShouldNotThrowException()
    {
        // Arrange
        var metadata = new FileMetadata("text/plain", "Test file");
        var nonExistentKey = "nonExistentKey";

        // Act & Assert
        var exception = Record.Exception(() => metadata.RemoveCustomProperty(nonExistentKey));
        Assert.Null(exception);
    }
}
