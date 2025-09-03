using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Infrastructure.Services;

namespace StorageFileApp.Infrastructure.Tests.Services;

public class StorageProviderFactoryTests
{
    private readonly Mock<ILogger<StorageProviderFactory>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<FileSystemStorageService> _fileSystemServiceMock;
    private readonly Mock<MinioS3StorageService> _s3ServiceMock;
    private readonly StorageProviderFactory _factory;

    public StorageProviderFactoryTests()
    {
        _loggerMock = new Mock<ILogger<StorageProviderFactory>>();
        _configurationMock = new Mock<IConfiguration>();
        _fileSystemServiceMock = new Mock<FileSystemStorageService>(Mock.Of<ILogger<FileSystemStorageService>>(), "test");
        _s3ServiceMock = new Mock<MinioS3StorageService>(Mock.Of<ILogger<MinioS3StorageService>>(), Mock.Of<IAmazonS3>(), "test");

        _factory = new StorageProviderFactory(
            _loggerMock.Object,
            _configurationMock.Object,
            _fileSystemServiceMock.Object,
            _s3ServiceMock.Object);
    }

    [Fact]
    public void GetStorageService_WithFileSystemProvider_ShouldReturnFileSystemService()
    {
        // Arrange
        var provider = new StorageProvider("Test FileSystem", StorageProviderType.FileSystem, "BasePath=test");

        // Act
        var result = _factory.GetStorageService(provider);

        // Assert
        result.Should().Be(_fileSystemServiceMock.Object);
    }

    [Fact]
    public void GetStorageService_WithMinIOProvider_ShouldReturnS3Service()
    {
        // Arrange
        var provider = new StorageProvider("Test MinIO", StorageProviderType.MinIO, "ServiceURL=test");

        // Act
        var result = _factory.GetStorageService(provider);

        // Assert
        result.Should().Be(_s3ServiceMock.Object);
    }

    [Fact]
    public void GetStorageService_WithUnsupportedProvider_ShouldThrowException()
    {
        // Arrange
        var provider = new StorageProvider("Test Database", StorageProviderType.Database, "ConnectionString=test");

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => _factory.GetStorageService(provider));
        exception.Message.Should().Contain("Storage provider type 'Database' is not supported");
    }

    [Fact]
    public void GetDefaultStorageService_WithFileSystemDefault_ShouldReturnFileSystemService()
    {
        // Arrange
        _configurationMock.Setup(x => x["StorageSettings:DefaultProvider"])
            .Returns("FileSystem");

        // Act
        var result = _factory.GetDefaultStorageService();

        // Assert
        result.Should().Be(_fileSystemServiceMock.Object);
    }

    [Fact]
    public void GetDefaultStorageService_WithMinIODefault_ShouldReturnS3Service()
    {
        // Arrange
        _configurationMock.Setup(x => x["StorageSettings:DefaultProvider"])
            .Returns("MinIO");

        // Act
        var result = _factory.GetDefaultStorageService();

        // Assert
        result.Should().Be(_s3ServiceMock.Object);
    }

    [Fact]
    public void GetDefaultStorageService_WithUnknownDefault_ShouldReturnFileSystemService()
    {
        // Arrange
        _configurationMock.Setup(x => x["StorageSettings:DefaultProvider"])
            .Returns("Unknown");

        // Act
        var result = _factory.GetDefaultStorageService();

        // Assert
        result.Should().Be(_fileSystemServiceMock.Object);
    }

    [Fact]
    public void GetDefaultStorageService_WithNullDefault_ShouldReturnFileSystemService()
    {
        // Arrange
        _configurationMock.Setup(x => x["StorageSettings:DefaultProvider"])
            .Returns((string?)null);

        // Act
        var result = _factory.GetDefaultStorageService();

        // Assert
        result.Should().Be(_fileSystemServiceMock.Object);
    }

    [Fact]
    public void GetAllStorageServices_WithFileSystemEnabled_ShouldReturnFileSystemService()
    {
        // Arrange
        _configurationMock.Setup(x => x["StorageSettings:FileSystemEnabled"])
            .Returns("true");
        _configurationMock.Setup(x => x["StorageSettings:MinIOEnabled"])
            .Returns("false");

        // Act
        var result = _factory.GetAllStorageServices();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(_fileSystemServiceMock.Object);
    }

    [Fact]
    public void GetAllStorageServices_WithMinIOEnabled_ShouldReturnS3Service()
    {
        // Arrange
        _configurationMock.Setup(x => x["StorageSettings:FileSystemEnabled"])
            .Returns("false");
        _configurationMock.Setup(x => x["StorageSettings:MinIOEnabled"])
            .Returns("true");

        // Act
        var result = _factory.GetAllStorageServices();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(_s3ServiceMock.Object);
    }

    [Fact]
    public void GetAllStorageServices_WithBothEnabled_ShouldReturnBothServices()
    {
        // Arrange
        _configurationMock.Setup(x => x["StorageSettings:FileSystemEnabled"])
            .Returns("true");
        _configurationMock.Setup(x => x["StorageSettings:MinIOEnabled"])
            .Returns("true");

        // Act
        var result = _factory.GetAllStorageServices();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(_fileSystemServiceMock.Object);
        result.Should().Contain(_s3ServiceMock.Object);
    }

    [Fact]
    public void GetAllStorageServices_WithBothDisabled_ShouldReturnEmpty()
    {
        // Arrange
        _configurationMock.Setup(x => x["StorageSettings:FileSystemEnabled"])
            .Returns("false");
        _configurationMock.Setup(x => x["StorageSettings:MinIOEnabled"])
            .Returns("false");

        // Act
        var result = _factory.GetAllStorageServices();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAllStorageServices_WithNullConfiguration_ShouldUseDefaults()
    {
        // Arrange
        _configurationMock.Setup(x => x["StorageSettings:FileSystemEnabled"])
            .Returns((string?)null);
        _configurationMock.Setup(x => x["StorageSettings:MinIOEnabled"])
            .Returns((string?)null);

        // Act
        var result = _factory.GetAllStorageServices();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(_fileSystemServiceMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StorageProviderFactory(
            null!,
            _configurationMock.Object,
            _fileSystemServiceMock.Object,
            _s3ServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StorageProviderFactory(
            _loggerMock.Object,
            null!,
            _fileSystemServiceMock.Object,
            _s3ServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullFileSystemService_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StorageProviderFactory(
            _loggerMock.Object,
            _configurationMock.Object,
            null!,
            _s3ServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullS3Service_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StorageProviderFactory(
            _loggerMock.Object,
            _configurationMock.Object,
            _fileSystemServiceMock.Object,
            null!));
    }
}
