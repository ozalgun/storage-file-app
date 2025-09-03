using Microsoft.Extensions.Logging;
using Moq;
using StorageFileApp.Infrastructure.Services;

namespace StorageFileApp.Infrastructure.Tests.Services;

public class SimpleInfrastructureServiceTests
{
    [Fact]
    public void FileSystemStorageService_Constructor_ShouldNotThrowException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FileSystemStorageService>>();
        var basePath = "/tmp/test";

        // Act & Assert
        var exception = Record.Exception(() => new FileSystemStorageService(loggerMock.Object, basePath));

        Assert.Null(exception);
    }

    [Fact]
    public void StorageProviderFactory_Constructor_ShouldNotThrowException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<StorageProviderFactory>>();
        var configurationMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        var fileSystemServiceMock = new Mock<FileSystemStorageService>(Mock.Of<ILogger<FileSystemStorageService>>(), "test");
        var s3ServiceMock = new Mock<MinioS3StorageService>(Mock.Of<ILogger<MinioS3StorageService>>(), Mock.Of<Amazon.S3.IAmazonS3>(), "test");

        // Act & Assert
        var exception = Record.Exception(() => new StorageProviderFactory(
            loggerMock.Object,
            configurationMock.Object,
            fileSystemServiceMock.Object,
            s3ServiceMock.Object));

        Assert.Null(exception);
    }
}
