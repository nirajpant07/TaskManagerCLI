using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Services;
using TaskManager.CLI.Commands;
using TaskManager.CLI.Repositories;
using TaskManager.CLI.Utilities;

namespace TaskManager.CLI.Tests;

public class TaskManagerServiceTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();
    private readonly Mock<ICommandFactory> _factoryMock = new();
    private readonly Mock<ISoundService> _soundMock = new();
    private readonly Mock<ConsoleHelper> _consoleMock = new();

    [Fact]
    public async Task ProcessCommandAsync_UnknownCommand_ReturnsError()
    {
        _factoryMock.Setup(f => f.CreateCommand(It.IsAny<string>())).Returns((ICommand?)null);
        var svc = new TaskManagerService(_repoMock.Object, _factoryMock.Object, _consoleMock.Object, _soundMock.Object);
        var result = await svc.ProcessCommandAsync("!unknown");
        Assert.Contains("Unknown command", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_CommandWithoutBang_ReturnsWarning()
    {
        var svc = new TaskManagerService(_repoMock.Object, _factoryMock.Object, _consoleMock.Object, _soundMock.Object);
        var result = await svc.ProcessCommandAsync("task");
        Assert.Contains("must start with '!'", result);
    }

    [Fact]
    public async Task ProcessCommandAsync_ValidCommand_ExecutesAndReturnsResult()
    {
        var cmdMock = new Mock<ICommand>();
        cmdMock.Setup(c => c.ExecuteAsync(It.IsAny<string[]>())).ReturnsAsync("OK");
        _factoryMock.Setup(f => f.CreateCommand("task")).Returns(cmdMock.Object);
        var svc = new TaskManagerService(_repoMock.Object, _factoryMock.Object, _consoleMock.Object, _soundMock.Object);
        var result = await svc.ProcessCommandAsync("!task do something");
        Assert.Equal("OK", result);
    }
} 