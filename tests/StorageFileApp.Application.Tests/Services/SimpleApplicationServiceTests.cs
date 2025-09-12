using Microsoft.Extensions.Logging;
using Moq;
using StorageFileApp.Application.Services;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Application.UseCases;
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
        var chunkingUseCaseMock = new Mock<IFileChunkingUseCase>();
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
            chunkingUseCaseMock.Object,
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
        var chunkingServiceMock = new Mock<IFileChunkingDomainService>();
        var mergingServiceMock = new Mock<IFileMergingDomainService>();
        var integrityServiceMock = new Mock<IFileIntegrityDomainService>();
        var storageProviderFactoryMock = new Mock<IStorageProviderFactory>();
        var storageStrategyServiceMock = new Mock<IStorageStrategyService>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var loggerMock = new Mock<ILogger<FileChunkingApplicationService>>();
        var streamingServiceMock = new Mock<IFileStreamingDomainService>();

        // Act & Assert
        var exception = Record.Exception(() => new FileChunkingApplicationService(
            fileRepositoryMock.Object,
            chunkRepositoryMock.Object,
            storageProviderRepositoryMock.Object,
            storageServiceMock.Object,
            chunkingServiceMock.Object,
            streamingServiceMock.Object,
            mergingServiceMock.Object,
            integrityServiceMock.Object,
            storageProviderFactoryMock.Object,
            storageStrategyServiceMock.Object,
            unitOfWorkMock.Object,
            loggerMock.Object));

        Assert.Null(exception);
    }
}
