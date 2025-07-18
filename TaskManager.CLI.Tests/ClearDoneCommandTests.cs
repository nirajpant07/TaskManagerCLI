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
            new() { Id = 1, Description = "Task 1", Status = TaskStatus.Completed },
            new() { Id = 2, Description = "Task 2", Status = TaskStatus.Completed },
            new() { Id = 3, Description = "Task 3", Status = TaskStatus.Pending }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.DeleteTaskAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
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
            new() { Id = 1, Description = "Task 1", Status = TaskStatus.Pending },
            new() { Id = 2, Description = "Task 2", Status = TaskStatus.InProgress }
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
            new() { Id = 1, Description = "Completed 1", Status = TaskStatus.Completed },
            new() { Id = 2, Description = "Completed 2", Status = TaskStatus.Completed },
            new() { Id = 3, Description = "Pending", Status = TaskStatus.Pending },
            new() { Id = 4, Description = "In Progress", Status = TaskStatus.InProgress }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.DeleteTaskAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        var cmd = new ClearDoneCommand(_repoMock.Object);
        await cmd.ExecuteAsync(new string[0]);
        _repoMock.Verify(r => r.DeleteTaskAsync(1), Times.Once);
        _repoMock.Verify(r => r.DeleteTaskAsync(2), Times.Once);
        _repoMock.Verify(r => r.DeleteTaskAsync(3), Times.Never);
        _repoMock.Verify(r => r.DeleteTaskAsync(4), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PreservesActiveTasks()
    {
        var tasks = new List<TaskModel>
        {
            new() { Id = 1, Description = "Completed", Status = TaskStatus.Completed },
            new() { Id = 2, Description = "Pending", Status = TaskStatus.Pending },
            new() { Id = 3, Description = "In Progress", Status = TaskStatus.InProgress },
            new() { Id = 4, Description = "Paused", Status = TaskStatus.Paused },
            new() { Id = 5, Description = "On Break", Status = TaskStatus.OnBreak }
        };
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.DeleteTaskAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        var cmd = new ClearDoneCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Active tasks remain", result);
        _repoMock.Verify(r => r.DeleteTaskAsync(1), Times.Once);
        _repoMock.Verify(r => r.DeleteTaskAsync(2), Times.Never);
        _repoMock.Verify(r => r.DeleteTaskAsync(3), Times.Never);
        _repoMock.Verify(r => r.DeleteTaskAsync(4), Times.Never);
        _repoMock.Verify(r => r.DeleteTaskAsync(5), Times.Never);
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