using Microsoft.Extensions.Logging;
using Moq;
using StorageFileApp.ConsoleApp.Services;

namespace StorageFileApp.Console.Tests.Services;

public class MenuServiceTests
{
    private readonly Mock<ILogger<MenuService>> _loggerMock;
    private readonly MenuService _menuService;

    public MenuServiceTests()
    {
        _loggerMock = new Mock<ILogger<MenuService>>();
        _menuService = new MenuService(_loggerMock.Object);
    }

    [Fact]
    public void DisplayMainMenu_ShouldDisplayCorrectOptions()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        // Act
        _menuService.DisplayMainMenu();

        // Assert
        var output = stringWriter.ToString();
        output.Should().Contain("1. Upload File");
        output.Should().Contain("2. Retrieve File");
        output.Should().Contain("3. Delete File");
        output.Should().Contain("4. List Files");
        output.Should().Contain("5. Health Check");
        output.Should().Contain("6. Exit");
    }

    [Fact]
    public void DisplayMessage_WithSuccessMessage_ShouldDisplaySuccessIcon()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        var message = "Test success message";

        // Act
        _menuService.DisplayMessage(message, false);

        // Assert
        var output = stringWriter.ToString();
        output.Should().Contain("✅");
        output.Should().Contain(message);
    }

    [Fact]
    public void DisplayMessage_WithErrorMessage_ShouldDisplayErrorIcon()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        var message = "Test error message";

        // Act
        _menuService.DisplayMessage(message, true);

        // Assert
        var output = stringWriter.ToString();
        output.Should().Contain("❌");
        output.Should().Contain(message);
    }

    [Fact]
    public void DisplayFileList_WithEmptyList_ShouldDisplayNoFilesMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        var files = new List<object>();

        // Act
        _menuService.DisplayFileList(files);

        // Assert
        var output = stringWriter.ToString();
        output.Should().Contain("No files found");
    }

    [Fact]
    public void DisplayFileList_WithFiles_ShouldDisplayFileInformation()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        var files = new List<object>
        {
            new { Id = Guid.NewGuid(), Name = "test1.txt", Size = 1024L, Status = "Available" },
            new { Id = Guid.NewGuid(), Name = "test2.txt", Size = 2048L, Status = "Processing" }
        };

        // Act
        _menuService.DisplayFileList(files);

        // Assert
        var output = stringWriter.ToString();
        output.Should().Contain("test1.txt");
        output.Should().Contain("test2.txt");
        output.Should().Contain("1024");
        output.Should().Contain("2048");
    }

    [Fact]
    public void DisplayHealthStatus_WithHealthyStatus_ShouldDisplayHealthyMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        var isHealthy = true;
        var message = "All systems operational";

        // Act
        _menuService.DisplayHealthStatus(isHealthy, message);

        // Assert
        var output = stringWriter.ToString();
        output.Should().Contain("✅");
        output.Should().Contain("System is healthy");
        output.Should().Contain(message);
    }

    [Fact]
    public void DisplayHealthStatus_WithUnhealthyStatus_ShouldDisplayUnhealthyMessage()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        var isHealthy = false;
        var message = "Database connection failed";

        // Act
        _menuService.DisplayHealthStatus(isHealthy, message);

        // Assert
        var output = stringWriter.ToString();
        output.Should().Contain("❌");
        output.Should().Contain("System is unhealthy");
        output.Should().Contain(message);
    }

    [Fact]
    public void DisplayProgress_WithValidProgress_ShouldDisplayProgressBar()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        var current = 50;
        var total = 100;
        var operation = "Processing";

        // Act
        _menuService.DisplayProgress(current, total, operation);

        // Assert
        var output = stringWriter.ToString();
        output.Should().Contain(operation);
        output.Should().Contain("50/100");
        output.Should().Contain("50%");
    }

    [Fact]
    public void DisplayProgress_WithZeroTotal_ShouldHandleGracefully()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        var current = 0;
        var total = 0;
        var operation = "Processing";

        // Act
        var exception = Record.Exception(() => _menuService.DisplayProgress(current, total, operation));

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void DisplayProgress_WithCurrentGreaterThanTotal_ShouldHandleGracefully()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        var current = 150;
        var total = 100;
        var operation = "Processing";

        // Act
        var exception = Record.Exception(() => _menuService.DisplayProgress(current, total, operation));

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void ClearScreen_ShouldClearConsole()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        // Act
        _menuService.ClearScreen();

        // Assert
        // This test mainly ensures the method doesn't throw an exception
        // The actual clearing behavior depends on the console implementation
    }

    [Fact]
    public void DisplaySeparator_ShouldDisplaySeparatorLine()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        // Act
        _menuService.DisplaySeparator();

        // Assert
        var output = stringWriter.ToString();
        output.Should().Contain("=");
    }

    [Fact]
    public void DisplayHeader_ShouldDisplayHeaderWithTitle()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        var title = "Test Header";

        // Act
        _menuService.DisplayHeader(title);

        // Assert
        var output = stringWriter.ToString();
        output.Should().Contain(title);
    }

    [Fact]
    public void DisplayFooter_ShouldDisplayFooter()
    {
        // Arrange
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        // Act
        _menuService.DisplayFooter();

        // Assert
        var output = stringWriter.ToString();
        output.Should().Contain("Storage File App");
    }

    [Fact]
    public void FormatFileSize_WithBytes_ShouldReturnCorrectFormat()
    {
        // Act
        var result = _menuService.FormatFileSize(1024);

        // Assert
        result.Should().Be("1.00 KB");
    }

    [Fact]
    public void FormatFileSize_WithKilobytes_ShouldReturnCorrectFormat()
    {
        // Act
        var result = _menuService.FormatFileSize(1024 * 1024);

        // Assert
        result.Should().Be("1.00 MB");
    }

    [Fact]
    public void FormatFileSize_WithMegabytes_ShouldReturnCorrectFormat()
    {
        // Act
        var result = _menuService.FormatFileSize(1024L * 1024 * 1024);

        // Assert
        result.Should().Be("1.00 GB");
    }

    [Fact]
    public void FormatFileSize_WithZero_ShouldReturnZeroBytes()
    {
        // Act
        var result = _menuService.FormatFileSize(0);

        // Assert
        result.Should().Be("0 B");
    }

    [Fact]
    public void FormatFileSize_WithNegativeValue_ShouldReturnZeroBytes()
    {
        // Act
        var result = _menuService.FormatFileSize(-1);

        // Assert
        result.Should().Be("0 B");
    }

    [Fact]
    public void FormatDateTime_WithValidDateTime_ShouldReturnFormattedString()
    {
        // Arrange
        var dateTime = new DateTime(2023, 12, 25, 14, 30, 45);

        // Act
        var result = _menuService.FormatDateTime(dateTime);

        // Assert
        result.Should().Contain("2023");
        result.Should().Contain("12");
        result.Should().Contain("25");
        result.Should().Contain("14:30");
    }

    [Fact]
    public void FormatDateTime_WithUtcDateTime_ShouldReturnFormattedString()
    {
        // Arrange
        var dateTime = DateTime.UtcNow;

        // Act
        var result = _menuService.FormatDateTime(dateTime);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }
}
