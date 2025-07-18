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
        var task = new TaskModel { Id = 1, Description = "Test", Status = TaskStatus.Pending };
        _repoMock.Setup(r => r.GetTaskByIdAsync(1)).ReturnsAsync(task);
        _repoMock.Setup(r => r.DeleteTaskAsync(1)).Returns(Task.CompletedTask);
        var cmd = new DeleteCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "1" });
        Assert.Contains("Task 1 deleted", result);
    }

    [Fact]
    public async Task ExecuteAsync_DeletesMultipleTasks_ReturnsSummary()
    {
        var task1 = new TaskModel { Id = 1, Description = "A", Status = TaskStatus.Pending };
        var task2 = new TaskModel { Id = 2, Description = "B", Status = TaskStatus.Pending };
        _repoMock.Setup(r => r.GetTaskByIdAsync(1)).ReturnsAsync(task1);
        _repoMock.Setup(r => r.GetTaskByIdAsync(2)).ReturnsAsync(task2);
        _repoMock.Setup(r => r.DeleteTaskAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        var cmd = new DeleteCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "1,2" });
        Assert.Contains("Deleted 2 task(s)", result);
        Assert.Contains("Task 1 deleted", result);
        Assert.Contains("Task 2 deleted", result);
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
        _repoMock.Setup(r => r.GetTaskByIdAsync(99)).ReturnsAsync((TaskModel?)null);
        var cmd = new DeleteCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "99" });
        Assert.Contains("Task 99 not found", result);
    }

    [Fact]
    public async Task ExecuteAsync_NoParameters_ReturnsError()
    {
        var cmd = new DeleteCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Please provide task ID", result);
    }
} 