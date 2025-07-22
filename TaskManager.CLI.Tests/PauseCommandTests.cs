using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using TaskManager.CLI.Services;
using System.Collections.Generic;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;

namespace TaskManager.CLI.Tests;

public class PauseCommandTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();
    private readonly Mock<IFocusSessionManagerService> _sessionMock = new();

    [Fact]
    public async Task ExecuteAsync_PausesFocusedTask_ReturnsSuccess()
    {
        var tasks = new List<TaskModel> { new() { Id = Guid.NewGuid(), Description = "Test", Status = TaskStatus.InProgress, IsFocused = true } };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _sessionMock.Setup(s => s.PauseCurrentSessionAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var cmd = new PauseCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains($"Task {tasks[0].Id} paused", result);
        Assert.Contains("Reason: Manual pause", result);
    }

    [Fact]
    public async Task ExecuteAsync_NoFocusedTask_ReturnsError()
    {
        var tasks = new List<TaskModel>();
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new PauseCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("No task is currently focused", result);
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomReason_IncludesReason()
    {
        var tasks = new List<TaskModel> { new() { Id = Guid.NewGuid(), Description = "Test", Status = TaskStatus.InProgress, IsFocused = true } };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _sessionMock.Setup(s => s.PauseCurrentSessionAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var cmd = new PauseCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "Lunch break" });
        Assert.Contains("Reason: Lunch break", result);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutReason_UsesDefault()
    {
        var tasks = new List<TaskModel> { new() { Id = Guid.NewGuid(), Description = "Test", Status = TaskStatus.InProgress, IsFocused = true } };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _sessionMock.Setup(s => s.PauseCurrentSessionAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var cmd = new PauseCommand(_repoMock.Object, _sessionMock.Object);
        await cmd.ExecuteAsync(new string[0]);
        _sessionMock.Verify(s => s.PauseCurrentSessionAsync("Manual pause"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesTaskStatusToPaused()
    {
        var task = new TaskModel { Id = Guid.NewGuid(), Description = "Test", Status = TaskStatus.InProgress, IsFocused = true };
        var tasks = new List<TaskModel> { task };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _sessionMock.Setup(s => s.PauseCurrentSessionAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var cmd = new PauseCommand(_repoMock.Object, _sessionMock.Object);
        await cmd.ExecuteAsync(new string[0]);
        _repoMock.Verify(r => r.UpdateTaskAsync(It.Is<TaskModel>(t => t.Status == TaskStatus.Paused && !t.IsFocused)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_IncludesResumeInstructions()
    {
        var tasks = new List<TaskModel> { new() { Id = Guid.NewGuid(), Description = "Test", Status = TaskStatus.InProgress, IsFocused = true } };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _sessionMock.Setup(s => s.PauseCurrentSessionAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var cmd = new PauseCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains($"Use '!focus next", result);
    }
} 