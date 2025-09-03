using Microsoft.Extensions.Logging;
using Moq;
using StorageFileApp.ConsoleApp.Services;

namespace StorageFileApp.Console.Tests.Services;

public class SimpleConsoleServiceTests
{
    [Fact]
    public void MenuService_Constructor_ShouldNotThrowException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MenuService>>();

        // Act & Assert
        var exception = Record.Exception(() => new MenuService(loggerMock.Object));

        Assert.Null(exception);
    }

    [Fact]
    public async Task MenuService_DisplayMainMenuAsync_ShouldNotThrowException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MenuService>>();
        var menuService = new MenuService(loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => menuService.DisplayMainMenuAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task MenuService_DisplayMessageAsync_ShouldNotThrowException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MenuService>>();
        var menuService = new MenuService(loggerMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => menuService.DisplayMessageAsync("Test message", false));

        Assert.Null(exception);
    }
}