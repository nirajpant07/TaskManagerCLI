using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using System.Collections.Generic;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;

namespace TaskManager.CLI.Tests;

public class ClearDoneCommandTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();

    [Fact]
    public async Task ExecuteAsync_ClearsCompletedTasks_ReturnsSuccess()
    {
        var tasks = new List<TaskModel>
        {
            new() { Id = Guid.NewGuid(), Description = "Task 1", Status = TaskStatus.Completed },
            new() { Id = Guid.NewGuid(), Description = "Task 2", Status = TaskStatus.Completed },
            new() { Id = Guid.NewGuid(), Description = "Task 3", Status = TaskStatus.Pending }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.DeleteTaskAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        var cmd = new ClearDoneCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Completed tasks cleared", result);
        Assert.Contains("Removed 2 completed task(s)", result);
    }

    [Fact]
    public async Task ExecuteAsync_NoCompletedTasks_ReturnsMessage()
    {
        var tasks = new List<TaskModel>
        {
            new() { Id = Guid.NewGuid(), Description = "Task 1", Status = TaskStatus.Pending },
            new() { Id = Guid.NewGuid(), Description = "Task 2", Status = TaskStatus.InProgress }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new ClearDoneCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("No completed tasks to clear", result);
    }

    [Fact]
    public async Task ExecuteAsync_DeletesOnlyCompletedTasks()
    {
        var tasks = new List<TaskModel>
        {
            new() { Id = Guid.NewGuid(), Description = "Completed 1", Status = TaskStatus.Completed },
            new() { Id = Guid.NewGuid(), Description = "Completed 2", Status = TaskStatus.Completed },
            new() { Id = Guid.NewGuid(), Description = "Pending", Status = TaskStatus.Pending },
            new() { Id = Guid.NewGuid(), Description = "In Progress", Status = TaskStatus.InProgress }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.DeleteTaskAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        var cmd = new ClearDoneCommand(_repoMock.Object);
        await cmd.ExecuteAsync(new string[0]);
        _repoMock.Verify(r => r.DeleteTaskAsync(tasks[0].Id), Times.Once);
        _repoMock.Verify(r => r.DeleteTaskAsync(tasks[1].Id), Times.Once);
        _repoMock.Verify(r => r.DeleteTaskAsync(tasks[2].Id), Times.Never);
        _repoMock.Verify(r => r.DeleteTaskAsync(tasks[3].Id), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PreservesActiveTasks()
    {
        var tasks = new List<TaskModel>
        {
            new() { Id = Guid.NewGuid(), Description = "Completed", Status = TaskStatus.Completed },
            new() { Id = Guid.NewGuid(), Description = "Pending", Status = TaskStatus.Pending },
            new() { Id = Guid.NewGuid(), Description = "In Progress", Status = TaskStatus.InProgress },
            new() { Id = Guid.NewGuid(), Description = "Paused", Status = TaskStatus.Paused },
            new() { Id = Guid.NewGuid(), Description = "On Break", Status = TaskStatus.OnBreak }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.DeleteTaskAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        var cmd = new ClearDoneCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Active tasks remain", result);
        _repoMock.Verify(r => r.DeleteTaskAsync(tasks[0].Id), Times.Once);
        _repoMock.Verify(r => r.DeleteTaskAsync(tasks[1].Id), Times.Never);
        _repoMock.Verify(r => r.DeleteTaskAsync(tasks[2].Id), Times.Never);
        _repoMock.Verify(r => r.DeleteTaskAsync(tasks[3].Id), Times.Never);
        _repoMock.Verify(r => r.DeleteTaskAsync(tasks[4].Id), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyTaskList_ReturnsMessage()
    {
        var tasks = new List<TaskModel>();
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new ClearDoneCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("No completed tasks to clear", result);
    }
} 