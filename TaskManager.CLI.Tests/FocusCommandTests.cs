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
        var tasks = new List<TaskModel> { new() { Id = 1, Description = "Test", Status = TaskStatus.Pending } };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("No task is currently focused", result);
        Assert.Contains("pending tasks", result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowCurrentFocus_TaskFocused_ReturnsTaskInfo()
    {
        var tasks = new List<TaskModel> { new() { Id = 1, Description = "Test", Status = TaskStatus.InProgress, IsFocused = true, FocusTime = TimeSpan.FromMinutes(30) } };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Currently focused on task 1", result);
        Assert.Contains("Test", result);
        Assert.Contains("Total focus time", result);
    }

    [Fact]
    public async Task ExecuteAsync_FocusNext_NoSpecificId_ReturnsSuccess()
    {
        var tasks = new List<TaskModel> { new() { Id = 1, Description = "Test", Status = TaskStatus.Pending } };
        var session = new FocusSession { FocusMinutes = 25 };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.GetTaskByIdAsync(1)).ReturnsAsync(tasks[0]);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _sessionMock.Setup(s => s.StartFocusSessionAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "next" });
        Assert.Contains("Now focusing on task 1", result);
    }

    [Fact]
    public async Task ExecuteAsync_FocusSpecificTask_ReturnsSuccess()
    {
        var tasks = new List<TaskModel> { new() { Id = 1, Description = "Test", Status = TaskStatus.Pending } };
        var session = new FocusSession { FocusMinutes = 25 };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.GetTaskByIdAsync(1)).ReturnsAsync(tasks[0]);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _sessionMock.Setup(s => s.StartFocusSessionAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "next", "1" });
        Assert.Contains("Now focusing on task 1", result);
    }

    [Fact]
    public async Task ExecuteAsync_TaskNotFound_ReturnsError()
    {
        var tasks = new List<TaskModel>();
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "next", "99" });
        Assert.Contains("Task 99 not found", result);
    }

    [Fact]
    public async Task ExecuteAsync_TaskCompleted_ReturnsError()
    {
        var task = new TaskModel { Id = 1, Description = "Test", Status = TaskStatus.Completed };
        var tasks = new List<TaskModel>();
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.GetTaskByIdAsync(1)).ReturnsAsync(task);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "next", "1" });
        Assert.Contains("Task 1 is already completed", result);
    }

    [Fact]
    public async Task ExecuteAsync_TaskDeleted_ReturnsError()
    {
        var task = new TaskModel { Id = 1, Description = "Test", Status = TaskStatus.Deleted };
        var tasks = new List<TaskModel>();
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.GetTaskByIdAsync(1)).ReturnsAsync(task);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "next", "1" });
        Assert.Contains("Task 1 has been deleted", result);
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
        var currentTask = new TaskModel { Id = 1, Description = "Current", Status = TaskStatus.InProgress, IsFocused = true };
        var newTask = new TaskModel { Id = 2, Description = "New", Status = TaskStatus.Pending };
        var tasks = new List<TaskModel> { currentTask, newTask };
        var session = new FocusSession { FocusMinutes = 25 };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.GetTaskByIdAsync(2)).ReturnsAsync(newTask);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _sessionMock.Setup(s => s.EndCurrentSessionAsync()).Returns(Task.CompletedTask);
        _sessionMock.Setup(s => s.StartFocusSessionAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var cmd = new FocusCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "next", "2" });
        Assert.Contains("Now focusing on task 2", result);
        _sessionMock.Verify(s => s.EndCurrentSessionAsync(), Times.Once);
    }
} 