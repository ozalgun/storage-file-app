using Microsoft.Extensions.Logging;
using Moq;
using StorageFileApp.Application.DTOs;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.ConsoleApp.Services;

namespace StorageFileApp.Console.Tests.Services;

public class FileOperationServiceTests
{
    private readonly Mock<ILogger<FileOperationService>> _loggerMock;
    private readonly Mock<IFileStorageUseCase> _fileStorageUseCaseMock;
    private readonly Mock<MenuService> _menuServiceMock;
    private readonly FileOperationService _service;

    public FileOperationServiceTests()
    {
        _loggerMock = new Mock<ILogger<FileOperationService>>();
        _fileStorageUseCaseMock = new Mock<IFileStorageUseCase>();
        _menuServiceMock = new Mock<MenuService>(Mock.Of<ILogger<MenuService>>());
        _service = new FileOperationService(_loggerMock.Object, _fileStorageUseCaseMock.Object, _menuServiceMock.Object);
    }

    [Fact]
    public async Task UploadFileAsync_WithValidFile_ShouldReturnSuccess()
    {
        // Arrange
        var filePath = "test-file.txt";
        var fileId = Guid.NewGuid();
        var uploadResult = new FileUploadResult(true, FileId: fileId);

        _fileStorageUseCaseMock.Setup(x => x.UploadFileAsync(It.IsAny<UploadFileRequest>()))
            .ReturnsAsync(uploadResult);
        _menuServiceMock.Setup(x => x.GetUserInputAsync(It.IsAny<string>()))
            .ReturnsAsync(filePath);
        _menuServiceMock.Setup(x => x.ConfirmOperationAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _service.UploadFileAsync();

        // Assert
        _fileStorageUseCaseMock.Verify(x => x.UploadFileAsync(It.Is<UploadFileRequest>(r => r.FilePath == filePath)), Times.Once);
        _menuServiceMock.Verify(x => x.DisplayMessageAsync(It.Is<string>(m => m.Contains("successfully")), false), Times.Once);
    }

    [Fact]
    public async Task UploadFileAsync_WithInvalidFilePath_ShouldDisplayErrorMessage()
    {
        // Arrange
        var filePath = "non-existent-file.txt";
        var uploadResult = new FileUploadResult(false, ErrorMessage: "File not found");

        _fileStorageUseCaseMock.Setup(x => x.UploadFileAsync(It.IsAny<UploadFileRequest>()))
            .ReturnsAsync(uploadResult);
        _menuServiceMock.Setup(x => x.GetUserInputAsync(It.IsAny<string>()))
            .ReturnsAsync(filePath);
        _menuServiceMock.Setup(x => x.ConfirmOperationAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _service.UploadFileAsync();

        // Assert
        _menuServiceMock.Verify(x => x.DisplayMessageAsync(It.Is<string>(m => m.Contains("Failed")), true), Times.Once);
    }

    [Fact]
    public async Task UploadFileAsync_WithUserCancellation_ShouldNotUpload()
    {
        // Arrange
        var filePath = "test-file.txt";

        _menuServiceMock.Setup(x => x.GetUserInputAsync(It.IsAny<string>()))
            .ReturnsAsync(filePath);
        _menuServiceMock.Setup(x => x.ConfirmOperationAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        await _service.UploadFileAsync();

        // Assert
        _fileStorageUseCaseMock.Verify(x => x.UploadFileAsync(It.IsAny<UploadFileRequest>()), Times.Never);
    }

    [Fact]
    public async Task RetrieveFileAsync_WithValidFileId_ShouldReturnSuccess()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var outputPath = "/tmp/retrieved-file.txt";
        var retrieveResult = new FileRetrievalResult(true, FilePath: outputPath);

        _fileStorageUseCaseMock.Setup(x => x.RetrieveFileAsync(It.IsAny<RetrieveFileRequest>()))
            .ReturnsAsync(retrieveResult);
        _menuServiceMock.SetupSequence(x => x.GetUserInputAsync(It.IsAny<string>()))
            .ReturnsAsync(fileId.ToString())
            .ReturnsAsync(outputPath);
        _menuServiceMock.Setup(x => x.ConfirmOperationAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _service.RetrieveFileAsync();

        // Assert
        _fileStorageUseCaseMock.Verify(x => x.RetrieveFileAsync(It.Is<RetrieveFileRequest>(r => r.FileId == fileId && r.OutputPath == outputPath)), Times.Once);
        _menuServiceMock.Verify(x => x.DisplayMessageAsync(It.Is<string>(m => m.Contains("successfully")), false), Times.Once);
    }

    [Fact]
    public async Task RetrieveFileAsync_WithInvalidFileId_ShouldDisplayErrorMessage()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var outputPath = "/tmp/retrieved-file.txt";
        var retrieveResult = new FileRetrievalResult(false, ErrorMessage: "File not found");

        _fileStorageUseCaseMock.Setup(x => x.RetrieveFileAsync(It.IsAny<RetrieveFileRequest>()))
            .ReturnsAsync(retrieveResult);
        _menuServiceMock.SetupSequence(x => x.GetUserInputAsync(It.IsAny<string>()))
            .ReturnsAsync(fileId.ToString())
            .ReturnsAsync(outputPath);
        _menuServiceMock.Setup(x => x.ConfirmOperationAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _service.RetrieveFileAsync();

        // Assert
        _menuServiceMock.Verify(x => x.DisplayMessageAsync(It.Is<string>(m => m.Contains("Failed")), true), Times.Once);
    }

    [Fact]
    public async Task RetrieveFileAsync_WithInvalidGuid_ShouldDisplayErrorMessage()
    {
        // Arrange
        var invalidFileId = "invalid-guid";
        var outputPath = "/tmp/retrieved-file.txt";

        _menuServiceMock.SetupSequence(x => x.GetUserInputAsync(It.IsAny<string>()))
            .ReturnsAsync(invalidFileId)
            .ReturnsAsync(outputPath);
        _menuServiceMock.Setup(x => x.ConfirmOperationAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _service.RetrieveFileAsync();

        // Assert
        _fileStorageUseCaseMock.Verify(x => x.RetrieveFileAsync(It.IsAny<RetrieveFileRequest>()), Times.Never);
        _menuServiceMock.Verify(x => x.DisplayMessageAsync(It.Is<string>(m => m.Contains("Invalid file ID")), true), Times.Once);
    }

    [Fact]
    public async Task DeleteFileAsync_WithValidFileId_ShouldReturnSuccess()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var deleteResult = new FileDeletionResult(true);

        _fileStorageUseCaseMock.Setup(x => x.DeleteFileAsync(It.IsAny<DeleteFileRequest>()))
            .ReturnsAsync(deleteResult);
        _menuServiceMock.Setup(x => x.GetUserInputAsync(It.IsAny<string>()))
            .ReturnsAsync(fileId.ToString());
        _menuServiceMock.Setup(x => x.ConfirmOperationAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _service.DeleteFileAsync();

        // Assert
        _fileStorageUseCaseMock.Verify(x => x.DeleteFileAsync(It.Is<DeleteFileRequest>(r => r.FileId == fileId)), Times.Once);
        _menuServiceMock.Verify(x => x.DisplayMessageAsync(It.Is<string>(m => m.Contains("successfully")), false), Times.Once);
    }

    [Fact]
    public async Task DeleteFileAsync_WithInvalidFileId_ShouldDisplayErrorMessage()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var deleteResult = new FileDeletionResult(false, ErrorMessage: "File not found");

        _fileStorageUseCaseMock.Setup(x => x.DeleteFileAsync(It.IsAny<DeleteFileRequest>()))
            .ReturnsAsync(deleteResult);
        _menuServiceMock.Setup(x => x.GetUserInputAsync(It.IsAny<string>()))
            .ReturnsAsync(fileId.ToString());
        _menuServiceMock.Setup(x => x.ConfirmOperationAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _service.DeleteFileAsync();

        // Assert
        _menuServiceMock.Verify(x => x.DisplayMessageAsync(It.Is<string>(m => m.Contains("Failed")), true), Times.Once);
    }

    [Fact]
    public async Task DeleteFileAsync_WithUserCancellation_ShouldNotDelete()
    {
        // Arrange
        var fileId = Guid.NewGuid();

        _menuServiceMock.Setup(x => x.GetUserInputAsync(It.IsAny<string>()))
            .ReturnsAsync(fileId.ToString());
        _menuServiceMock.Setup(x => x.ConfirmOperationAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        await _service.DeleteFileAsync();

        // Assert
        _fileStorageUseCaseMock.Verify(x => x.DeleteFileAsync(It.IsAny<DeleteFileRequest>()), Times.Never);
    }

    [Fact]
    public async Task ListFilesAsync_WithFiles_ShouldDisplayFileList()
    {
        // Arrange
        var files = new List<FileListResult>
        {
            new FileListResult(Guid.NewGuid(), "file1.txt", 1024L, "Available", DateTime.UtcNow),
            new FileListResult(Guid.NewGuid(), "file2.txt", 2048L, "Processing", DateTime.UtcNow)
        };
        var listResult = new FileListResult(files);

        _fileStorageUseCaseMock.Setup(x => x.ListFilesAsync(It.IsAny<ListFilesRequest>()))
            .ReturnsAsync(listResult);

        // Act
        await _service.ListFilesAsync();

        // Assert
        _fileStorageUseCaseMock.Verify(x => x.ListFilesAsync(It.IsAny<ListFilesRequest>()), Times.Once);
        _menuServiceMock.Verify(x => x.DisplayFileListAsync(It.IsAny<IEnumerable<object>>()), Times.Once);
    }

    [Fact]
    public async Task ListFilesAsync_WithNoFiles_ShouldDisplayNoFilesMessage()
    {
        // Arrange
        var emptyFiles = new List<FileListResult>();
        var listResult = new FileListResult(emptyFiles);

        _fileStorageUseCaseMock.Setup(x => x.ListFilesAsync(It.IsAny<ListFilesRequest>()))
            .ReturnsAsync(listResult);

        // Act
        await _service.ListFilesAsync();

        // Assert
        _fileStorageUseCaseMock.Verify(x => x.ListFilesAsync(It.IsAny<ListFilesRequest>()), Times.Once);
        _menuServiceMock.Verify(x => x.DisplayFileListAsync(It.IsAny<IEnumerable<object>>()), Times.Once);
    }

    [Fact]
    public async Task ListFilesAsync_WithError_ShouldDisplayErrorMessage()
    {
        // Arrange
        var listResult = new FileListResult(false, ErrorMessage: "Database connection failed");

        _fileStorageUseCaseMock.Setup(x => x.ListFilesAsync(It.IsAny<ListFilesRequest>()))
            .ReturnsAsync(listResult);

        // Act
        await _service.ListFilesAsync();

        // Assert
        _menuServiceMock.Verify(x => x.DisplayMessageAsync(It.Is<string>(m => m.Contains("Failed")), true), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WithHealthySystem_ShouldDisplayHealthyMessage()
    {
        // Arrange
        var healthResult = new HealthCheckResult(true, "All systems operational");

        _fileStorageUseCaseMock.Setup(x => x.CheckHealthAsync(It.IsAny<HealthCheckRequest>()))
            .ReturnsAsync(healthResult);

        // Act
        await _service.CheckHealthAsync();

        // Assert
        _fileStorageUseCaseMock.Verify(x => x.CheckHealthAsync(It.IsAny<HealthCheckRequest>()), Times.Once);
        _menuServiceMock.Verify(x => x.DisplayHealthStatusAsync(true, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WithUnhealthySystem_ShouldDisplayUnhealthyMessage()
    {
        // Arrange
        var healthResult = new HealthCheckResult(false, "Database connection failed");

        _fileStorageUseCaseMock.Setup(x => x.CheckHealthAsync(It.IsAny<HealthCheckRequest>()))
            .ReturnsAsync(healthResult);

        // Act
        await _service.CheckHealthAsync();

        // Assert
        _fileStorageUseCaseMock.Verify(x => x.CheckHealthAsync(It.IsAny<HealthCheckRequest>()), Times.Once);
        _menuServiceMock.Verify(x => x.DisplayHealthStatusAsync(false, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileOperationService(
            null!,
            _fileStorageUseCaseMock.Object,
            _menuServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullFileStorageUseCase_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileOperationService(
            _loggerMock.Object,
            null!,
            _menuServiceMock.Object));
    }

    [Fact]
    public void Constructor_WithNullMenuService_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileOperationService(
            _loggerMock.Object,
            _fileStorageUseCaseMock.Object,
            null!));
    }
}
