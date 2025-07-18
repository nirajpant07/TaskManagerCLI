using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Services;
using TaskManager.CLI.Repositories;
using TaskManager.CLI.Models;
using TaskManager.CLI.Utilities;
using TaskManager.CLI.Commands;
using System.Collections.Generic;
using SessionType = TaskManager.CLI.Models.SessionType;

namespace TaskManager.CLI.Tests;

public class CommandLoggingTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();
    private readonly Mock<ICommandFactory> _factoryMock = new();
    private readonly Mock<ISoundService> _soundMock = new();
    private readonly Mock<ConsoleHelper> _consoleMock = new();

    [Fact]
    public async Task ProcessCommandAsync_LogsCommandExecution()
    {
        // Arrange
        var taskManager = new TaskManagerService(_repoMock.Object, _factoryMock.Object, _consoleMock.Object, _soundMock.Object);
        var commandLine = "!task Test task";
        
        _factoryMock.Setup(f => f.CreateCommand("task")).Returns(new Mock<ICommand>().Object);
        _repoMock.Setup(r => r.AddSessionLogAsync(It.IsAny<SessionLog>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await taskManager.ProcessCommandAsync(commandLine);

        // Assert
        _repoMock.Verify(r => r.AddSessionLogAsync(It.Is<SessionLog>(log => 
            log.Type == SessionType.Command && 
            log.Notes.Contains("Command executed: !task Test task") &&
            log.TaskId == null)), Times.Once);
    }

    [Fact]
    public async Task ProcessCommandAsync_LogsUnknownCommand()
    {
        // Arrange
        var taskManager = new TaskManagerService(_repoMock.Object, _factoryMock.Object, _consoleMock.Object, _soundMock.Object);
        var commandLine = "!unknowncommand";
        
        _factoryMock.Setup(f => f.CreateCommand("unknowncommand")).Returns((ICommand?)null);
        _repoMock.Setup(r => r.AddSessionLogAsync(It.IsAny<SessionLog>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await taskManager.ProcessCommandAsync(commandLine);

        // Assert
        _repoMock.Verify(r => r.AddSessionLogAsync(It.Is<SessionLog>(log => 
            log.Type == SessionType.Command && 
            log.Notes.Contains("Command executed: !unknowncommand") &&
            log.TaskId == null)), Times.Once);
    }

    [Fact]
    public async Task ProcessCommandAsync_LogsCommandWithParameters()
    {
        // Arrange
        var taskManager = new TaskManagerService(_repoMock.Object, _factoryMock.Object, _consoleMock.Object, _soundMock.Object);
        var commandLine = "!edit 1 Updated description";
        
        _factoryMock.Setup(f => f.CreateCommand("edit")).Returns(new Mock<ICommand>().Object);
        _repoMock.Setup(r => r.AddSessionLogAsync(It.IsAny<SessionLog>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await taskManager.ProcessCommandAsync(commandLine);

        // Assert
        _repoMock.Verify(r => r.AddSessionLogAsync(It.Is<SessionLog>(log => 
            log.Type == SessionType.Command && 
            log.Notes.Contains("Command executed: !edit 1 Updated description") &&
            log.TaskId == null)), Times.Once);
    }

    [Fact]
    public async Task ProcessCommandAsync_LoggingFailure_DoesNotBreakCommandExecution()
    {
        // Arrange
        var taskManager = new TaskManagerService(_repoMock.Object, _factoryMock.Object, _consoleMock.Object, _soundMock.Object);
        var commandLine = "!task Test task";
        
        _factoryMock.Setup(f => f.CreateCommand("task")).Returns(new Mock<ICommand>().Object);
        _repoMock.Setup(r => r.AddSessionLogAsync(It.IsAny<SessionLog>())).ThrowsAsync(new Exception("Logging failed"));
        _repoMock.Setup(r => r.SaveAsync()).Returns(Task.CompletedTask);

        // Act & Assert - Should not throw exception
        var result = await taskManager.ProcessCommandAsync(commandLine);
        
        // Verify that the command still executed despite logging failure
        _factoryMock.Verify(f => f.CreateCommand("task"), Times.Once);
    }

    [Fact]
    public async Task ProcessCommandAsync_InvalidCommandFormat_DoesNotLog()
    {
        // Arrange
        var taskManager = new TaskManagerService(_repoMock.Object, _factoryMock.Object, _consoleMock.Object, _soundMock.Object);
        var commandLine = "invalid command without bang";

        // Act
        await taskManager.ProcessCommandAsync(commandLine);

        // Assert
        _repoMock.Verify(r => r.AddSessionLogAsync(It.IsAny<SessionLog>()), Times.Never);
    }

    [Fact]
    public async Task ProcessCommandAsync_EmptyCommand_DoesNotLog()
    {
        // Arrange
        var taskManager = new TaskManagerService(_repoMock.Object, _factoryMock.Object, _consoleMock.Object, _soundMock.Object);
        var commandLine = "";

        // Act
        await taskManager.ProcessCommandAsync(commandLine);

        // Assert
        _repoMock.Verify(r => r.AddSessionLogAsync(It.IsAny<SessionLog>()), Times.Never);
    }
} 