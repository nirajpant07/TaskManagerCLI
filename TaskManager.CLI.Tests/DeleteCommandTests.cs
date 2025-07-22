using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using System.Collections.Generic;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;

namespace TaskManager.CLI.Tests;

public class DeleteCommandTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();

    [Fact]
    public async Task ExecuteAsync_DeletesSingleTask_ReturnsSuccess()
    {
        var task = new TaskModel { Id = Guid.NewGuid(), Description = "Test", Status = TaskStatus.Pending };
        _repoMock.Setup(r => r.GetTaskByIdAsync(task.Id)).ReturnsAsync(task);
        _repoMock.Setup(r => r.DeleteTaskAsync(task.Id)).Returns(Task.CompletedTask);
        var cmd = new DeleteCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new[] { task.Id.ToString() });
        Assert.Contains($"Task {task.Id} deleted", result);
    }

    [Fact]
    public async Task ExecuteAsync_DeletesMultipleTasks_ReturnsSummary()
    {
        var task1 = new TaskModel { Id = Guid.NewGuid(), Description = "A", Status = TaskStatus.Pending };
        var task2 = new TaskModel { Id = Guid.NewGuid(), Description = "B", Status = TaskStatus.Pending };
        _repoMock.Setup(r => r.GetTaskByIdAsync(task1.Id)).ReturnsAsync(task1);
        _repoMock.Setup(r => r.GetTaskByIdAsync(task2.Id)).ReturnsAsync(task2);
        _repoMock.Setup(r => r.DeleteTaskAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        var cmd = new DeleteCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new[] { task1.Id.ToString(), task2.Id.ToString() });
        Assert.Contains("Deleted 2 task(s)", result);
        Assert.Contains($"Task {task1.Id} deleted", result);
        Assert.Contains($"Task {task2.Id} deleted", result);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTaskId_ReturnsError()
    {
        var cmd = new DeleteCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "abc" });
        Assert.Contains("Invalid task ID", result);
    }

    [Fact]
    public async Task ExecuteAsync_TaskNotFound_ReturnsError()
    {
        var taskId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetTaskByIdAsync(taskId)).ReturnsAsync((TaskModel?)null);
        var cmd = new DeleteCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new[] { taskId.ToString() });
        Assert.Contains($"Task {taskId} not found", result);
    }

    [Fact]
    public async Task ExecuteAsync_NoParameters_ReturnsError()
    {
        var cmd = new DeleteCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Please provide task ID", result);
    }
} 