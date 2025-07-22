using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using System.Collections.Generic;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;

namespace TaskManager.CLI.Tests;

public class ClearListCommandTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();

    [Fact]
    public async Task ExecuteAsync_ClearsAllTasks_ReturnsSuccess()
    {
        var tasks = new List<TaskModel>
        {
            new() { Id = Guid.NewGuid(), Description = "Task 1", Status = TaskStatus.Pending },
            new() { Id = Guid.NewGuid(), Description = "Task 2", Status = TaskStatus.InProgress },
            new() { Id = Guid.NewGuid(), Description = "Task 3", Status = TaskStatus.Paused }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.DeleteTaskAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        var cmd = new ClearListCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("All tasks cleared", result);
        Assert.Contains("Deleted 3 task(s)", result);
    }

    [Fact]
    public async Task ExecuteAsync_NoTasksToClear_ReturnsMessage()
    {
        var tasks = new List<TaskModel>();
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new ClearListCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("No tasks to clear", result);
        Assert.Contains("already empty", result);
    }

    [Fact]
    public async Task ExecuteAsync_DeletesAllActiveTasks()
    {
        var tasks = new List<TaskModel>
        {
            new() { Id = Guid.NewGuid(), Description = "Task 1", Status = TaskStatus.Pending },
            new() { Id = Guid.NewGuid(), Description = "Task 2", Status = TaskStatus.InProgress }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.DeleteTaskAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        var cmd = new ClearListCommand(_repoMock.Object);
        await cmd.ExecuteAsync(new string[0]);
        _repoMock.Verify(r => r.DeleteTaskAsync(tasks[0].Id), Times.Once);
        _repoMock.Verify(r => r.DeleteTaskAsync(tasks[1].Id), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ExcludesDeletedTasks()
    {
        var tasks = new List<TaskModel>
        {
            new() { Id = Guid.NewGuid(), Description = "Active", Status = TaskStatus.Pending },
            new() { Id = Guid.NewGuid(), Description = "Deleted", Status = TaskStatus.Deleted }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.DeleteTaskAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        var cmd = new ClearListCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Deleted 1 task(s)", result);
        _repoMock.Verify(r => r.DeleteTaskAsync(tasks[0].Id), Times.Once);
        _repoMock.Verify(r => r.DeleteTaskAsync(tasks[1].Id), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_IncludesHelpfulInstructions()
    {
        var tasks = new List<TaskModel>
        {
            new() { Id = Guid.NewGuid(), Description = "Task 1", Status = TaskStatus.Pending }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.DeleteTaskAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        var cmd = new ClearListCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Start fresh with '!task", result);
    }
} 