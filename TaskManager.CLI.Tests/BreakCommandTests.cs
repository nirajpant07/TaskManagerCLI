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

public class BreakCommandTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();
    private readonly Mock<IFocusSessionManagerService> _sessionMock = new();
    private readonly Mock<INotificationService> _notificationMock = new();
    private readonly Mock<ISoundService> _soundMock = new();

    [Fact]
    public async Task ExecuteAsync_NoFocusedTask_StartsBreakSession()
    {
        var tasks = new List<TaskModel>();
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _sessionMock.Setup(s => s.StartBreakSessionAsync()).Returns(Task.CompletedTask);
        var session = new FocusSession { BreakMinutes = 5 };
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        var cmd = new BreakCommand(_repoMock.Object, _sessionMock.Object, _notificationMock.Object, _soundMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Break session started", result);
        Assert.Contains("5 minutes", result);
    }

    [Fact]
    public async Task ExecuteAsync_WithFocusedTask_EndsCurrentSessionAndStartsBreak()
    {
        var tasks = new List<TaskModel> { new() { Id = 1, Description = "Test", Status = TaskStatus.InProgress, IsFocused = true } };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _sessionMock.Setup(s => s.EndCurrentSessionAsync()).Returns(Task.CompletedTask);
        _sessionMock.Setup(s => s.StartBreakSessionAsync()).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var session = new FocusSession { BreakMinutes = 5 };
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        var cmd = new BreakCommand(_repoMock.Object, _sessionMock.Object, _notificationMock.Object, _soundMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Break session started", result);
        Assert.Contains("Task 1 (Test) set to break status", result);
        _sessionMock.Verify(s => s.EndCurrentSessionAsync(), Times.Once);
        _sessionMock.Verify(s => s.StartBreakSessionAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesTaskStatusToOnBreak()
    {
        var task = new TaskModel { Id = 1, Description = "Test", Status = TaskStatus.InProgress, IsFocused = true };
        var tasks = new List<TaskModel> { task };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _sessionMock.Setup(s => s.EndCurrentSessionAsync()).Returns(Task.CompletedTask);
        _sessionMock.Setup(s => s.StartBreakSessionAsync()).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var session = new FocusSession { BreakMinutes = 5 };
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        var cmd = new BreakCommand(_repoMock.Object, _sessionMock.Object, _notificationMock.Object, _soundMock.Object);
        await cmd.ExecuteAsync(new string[0]);
        _repoMock.Verify(r => r.UpdateTaskAsync(It.Is<TaskModel>(t => t.Status == TaskStatus.OnBreak && !t.IsFocused)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_IncludesBreakSuggestions()
    {
        var tasks = new List<TaskModel>();
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _sessionMock.Setup(s => s.StartBreakSessionAsync()).Returns(Task.CompletedTask);
        var session = new FocusSession { BreakMinutes = 5 };
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        var cmd = new BreakCommand(_repoMock.Object, _sessionMock.Object, _notificationMock.Object, _soundMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Break suggestions", result);
        Assert.Contains("Stand up and stretch", result);
        Assert.Contains("Get some water or tea", result);
    }
} 