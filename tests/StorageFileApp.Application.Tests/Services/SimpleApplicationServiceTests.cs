using Microsoft.Extensions.Logging;
using Moq;
using StorageFileApp.Application.Services;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Services;

namespace StorageFileApp.Application.Tests.Services;

public class SimpleApplicationServiceTests
{
    [Fact]
    public void FileStorageApplicationService_Constructor_ShouldNotThrowException()
    {
        // Arrange
        var fileRepositoryMock = new Mock<IFileRepository>();
        var chunkRepositoryMock = new Mock<IChunkRepository>();
        var storageProviderRepositoryMock = new Mock<IStorageProviderRepository>();
        var storageServiceMock = new Mock<IStorageService>();
        var storageProviderFactoryMock = new Mock<IStorageProviderFactory>();
        var chunkingServiceMock = new Mock<IFileChunkingDomainService>();
        var integrityServiceMock = new Mock<IFileIntegrityDomainService>();
        var validationServiceMock = new Mock<IFileValidationDomainService>();
        var strategyServiceMock = new Mock<IStorageStrategyDomainService>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<FileStorageApplicationService>>();

        // Act & Assert
        var exception = Record.Exception(() => new FileStorageApplicationService(
            fileRepositoryMock.Object,
            chunkRepositoryMock.Object,
            storageProviderRepositoryMock.Object,
            storageServiceMock.Object,
            storageProviderFactoryMock.Object,
            chunkingServiceMock.Object,
            integrityServiceMock.Object,
            validationServiceMock.Object,
            strategyServiceMock.Object,
            unitOfWorkMock.Object,
            loggerMock.Object));

        Assert.Null(exception);
    }

    [Fact]
    public void FileChunkingApplicationService_Constructor_ShouldNotThrowException()
    {
        // Arrange
        var fileRepositoryMock = new Mock<IFileRepository>();
        var chunkRepositoryMock = new Mock<IChunkRepository>();
        var storageProviderRepositoryMock = new Mock<IStorageProviderRepository>();
        var storageServiceMock = new Mock<IStorageService>();
        var storageProviderFactoryMock = new Mock<IStorageProviderFactory>();
        var storageStrategyServiceMock = new Mock<IStorageStrategyService>();
        var chunkingServiceMock = new Mock<IFileChunkingDomainService>();
        var integrityServiceMock = new Mock<IFileIntegrityDomainService>();
        var validationServiceMock = new Mock<IFileValidationDomainService>();
        var strategyServiceMock = new Mock<IStorageStrategyDomainService>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<FileChunkingApplicationService>>();

        // Act & Assert
        var exception = Record.Exception(() => new FileChunkingApplicationService(
            fileRepositoryMock.Object,
            chunkRepositoryMock.Object,
            storageProviderRepositoryMock.Object,
            storageServiceMock.Object,
            chunkingServiceMock.Object,
            storageStrategyServiceMock.Object,
            chunkingServiceMock.Object,
            integrityServiceMock.Object,
            validationServiceMock.Object,
            strategyServiceMock.Object,
            unitOfWorkMock.Object,
            loggerMock.Object));

        Assert.Null(exception);
    }
}
