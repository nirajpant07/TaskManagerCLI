using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Commands;

namespace TaskManager.CLI.Tests;

public class HelpCommandTests
{
    private readonly Mock<ICommandFactory> _factoryMock = new();

    [Fact]
    public async Task ExecuteAsync_ReturnsHelpText()
    {
        var expectedHelpText = "📋 Available Commands:\n\n🔨 Task Management:\n  !task <description>";
        _factoryMock.Setup(f => f.GetHelpText()).Returns(expectedHelpText);
        var cmd = new HelpCommand(_factoryMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Equal(expectedHelpText, result);
    }

    [Fact]
    public async Task ExecuteAsync_CallsFactoryMethod()
    {
        _factoryMock.Setup(f => f.GetHelpText()).Returns("Help text");
        var cmd = new HelpCommand(_factoryMock.Object);
        await cmd.ExecuteAsync(new string[0]);
        _factoryMock.Verify(f => f.GetHelpText(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithParameters_StillReturnsHelpText()
    {
        var expectedHelpText = "📋 Available Commands:\n\n🔨 Task Management:\n  !task <description>";
        _factoryMock.Setup(f => f.GetHelpText()).Returns(expectedHelpText);
        var cmd = new HelpCommand(_factoryMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "task", "focus" });
        Assert.Equal(expectedHelpText, result);
    }
} 