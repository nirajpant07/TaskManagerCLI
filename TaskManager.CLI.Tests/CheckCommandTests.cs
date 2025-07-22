using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using TaskManager.CLI.Utilities;
using System.Collections.Generic;
using System;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;

namespace TaskManager.CLI.Tests;

public class CheckCommandTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();
    private readonly Mock<ConsoleHelper> _consoleMock = new();

    [Fact]
    public async Task ExecuteAsync_NoActiveTasks_ReturnsMessage()
    {
        var tasks = new List<TaskModel>();
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new CheckCommand(_repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("No active tasks found", result);
        Assert.Contains("Add tasks with '!task", result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowsTasksByStatus()
    {
        var tasks = new List<TaskModel>
        {
            new() { Id = Guid.NewGuid(), Description = "Task 1", Status = TaskStatus.Pending },
            new() { Id = Guid.NewGuid(), Description = "Task 2", Status = TaskStatus.InProgress, IsFocused = true },
            new() { Id = Guid.NewGuid(), Description = "Task 3", Status = TaskStatus.Paused, PauseReason = "Lunch" },
            new() { Id = Guid.NewGuid(), Description = "Task 4", Status = TaskStatus.OnBreak }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new CheckCommand(_repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("In Progress:", result);
        Assert.Contains("Paused:", result);
        Assert.Contains("On Break:", result);
        Assert.Contains("Pending:", result);
    }

    [Fact]
    public async Task ExecuteAsync_FocusedTask_ShowsFocusIcon()
    {
        var tasks = new List<TaskModel>
        {
            new() { Id = Guid.NewGuid(), Description = "Task 1", Status = TaskStatus.InProgress, IsFocused = true }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new CheckCommand(_repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("👁️", result);
    }

    [Fact]
    public async Task ExecuteAsync_TaskWithFocusTime_ShowsTimeInfo()
    {
        var tasks = new List<TaskModel>
        {
            new() { Id = Guid.NewGuid(), Description = "Task 1", Status = TaskStatus.InProgress, FocusTime = TimeSpan.FromMinutes(30) }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new CheckCommand(_repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("[00:30:00]", result);
    }

    [Fact]
    public async Task ExecuteAsync_PausedTask_ShowsPauseReason()
    {
        var tasks = new List<TaskModel>
        {
            new() { Id = Guid.NewGuid(), Description = "Task 1", Status = TaskStatus.Paused, PauseReason = "Lunch break" }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new CheckCommand(_repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("- Lunch break", result);
    }

    [Fact]
    public async Task ExecuteAsync_ExcludesCompletedAndDeletedTasks()
    {
        var tasks = new List<TaskModel>
        {
            new() { Id = Guid.NewGuid(), Description = "Active", Status = TaskStatus.Pending },
            new() { Id = Guid.NewGuid(), Description = "Completed", Status = TaskStatus.Completed },
            new() { Id = Guid.NewGuid(), Description = "Deleted", Status = TaskStatus.Deleted }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new CheckCommand(_repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Active", result);
        Assert.DoesNotContain("Completed", result);
        Assert.DoesNotContain("Deleted", result);
    }

    [Fact]
    public async Task ExecuteAsync_IncludesHelpfulInstructions()
    {
        var tasks = new List<TaskModel>
        {
            new() { Id = Guid.NewGuid(), Description = "Task 1", Status = TaskStatus.Pending }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new CheckCommand(_repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Use '!focus next'", result);
    }
} 