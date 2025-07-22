using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using TaskManager.CLI.Services;
using System.Collections.Generic;
using System;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;

namespace TaskManager.CLI.Tests;

public class FocusCommandTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();
    private readonly Mock<IFocusSessionManagerService> _sessionMock = new();

    [Fact]
    public async Task ExecuteAsync_ShowCurrentFocus_NoTaskFocused_ReturnsMessage()
    {
        var tasks = new List<TaskModel> { new() { Id = Guid.NewGuid(), Description = "Test", Status = TaskStatus.Pending } };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("No task is currently focused", result);
        Assert.Contains("pending tasks", result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowCurrentFocus_TaskFocused_ReturnsTaskInfo()
    {
        var tasks = new List<TaskModel> { new() { Id = Guid.NewGuid(), Description = "Test", Status = TaskStatus.InProgress, IsFocused = true, FocusTime = TimeSpan.FromMinutes(30) } };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains($"Currently focused on task {tasks[0].Id}", result);
        Assert.Contains("Test", result);
        Assert.Contains("Total focus time", result);
    }

    [Fact]
    public async Task ExecuteAsync_FocusNext_NoSpecificId_ReturnsSuccess()
    {
        var tasks = new List<TaskModel> { new() { Id = Guid.NewGuid(), Description = "Test", Status = TaskStatus.Pending } };
        var session = new FocusSession { FocusMinutes = 25 };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.GetTaskByIdAsync(tasks[0].Id)).ReturnsAsync(tasks[0]);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _sessionMock.Setup(s => s.StartFocusSessionAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "next" });
        Assert.Contains($"Now focusing on task {tasks[0].Id}", result);
    }

    [Fact]
    public async Task ExecuteAsync_FocusSpecificTask_ReturnsSuccess()
    {
        var tasks = new List<TaskModel> { new() { Id = Guid.NewGuid(), Description = "Test", Status = TaskStatus.Pending } };
        var session = new FocusSession { FocusMinutes = 25 };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.GetTaskByIdAsync(tasks[0].Id)).ReturnsAsync(tasks[0]);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _sessionMock.Setup(s => s.StartFocusSessionAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "next", tasks[0].Id.ToString() });
        Assert.Contains($"Now focusing on task {tasks[0].Id}", result);
    }

    [Fact]
    public async Task ExecuteAsync_TaskNotFound_ReturnsError()
    {
        var tasks = new List<TaskModel>();
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var missingId = Guid.NewGuid();
        var result = await cmd.ExecuteAsync(new[] { "next", missingId.ToString() });
        Assert.Contains($"Task {missingId} not found", result);
    }

    [Fact]
    public async Task ExecuteAsync_TaskCompleted_ReturnsError()
    {
        var task = new TaskModel { Id = Guid.NewGuid(), Description = "Test", Status = TaskStatus.Completed };
        var tasks = new List<TaskModel>();
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.GetTaskByIdAsync(task.Id)).ReturnsAsync(task);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "next", task.Id.ToString() });
        Assert.Contains($"Task {task.Id} is already completed", result);
    }

    [Fact]
    public async Task ExecuteAsync_TaskDeleted_ReturnsError()
    {
        var task = new TaskModel { Id = Guid.NewGuid(), Description = "Test", Status = TaskStatus.Deleted };
        var tasks = new List<TaskModel>();
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.GetTaskByIdAsync(task.Id)).ReturnsAsync(task);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "next", task.Id.ToString() });
        Assert.Contains($"Task {task.Id} has been deleted", result);
    }

    [Fact]
    public async Task ExecuteAsync_NoPendingTasks_ReturnsMessage()
    {
        var tasks = new List<TaskModel>();
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "next" });
        Assert.Contains("No pending tasks available", result);
    }

    [Fact]
    public async Task ExecuteAsync_SwitchFocus_EndsCurrentSession()
    {
        var currentTask = new TaskModel { Id = Guid.NewGuid(), Description = "Current", Status = TaskStatus.InProgress, IsFocused = true };
        var newTask = new TaskModel { Id = Guid.NewGuid(), Description = "New", Status = TaskStatus.Pending };
        var tasks = new List<TaskModel> { currentTask, newTask };
        var session = new FocusSession { FocusMinutes = 25 };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.GetTaskByIdAsync(newTask.Id)).ReturnsAsync(newTask);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _sessionMock.Setup(s => s.EndCurrentSessionAsync()).Returns(Task.CompletedTask);
        _sessionMock.Setup(s => s.StartFocusSessionAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "next", newTask.Id.ToString() });
        Assert.Contains($"Now focusing on task {newTask.Id}", result);
        _sessionMock.Verify(s => s.EndCurrentSessionAsync(), Times.Once);
    }
} 