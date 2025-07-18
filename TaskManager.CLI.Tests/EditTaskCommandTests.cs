using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;

namespace TaskManager.CLI.Tests;

public class EditTaskCommandTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();

    [Fact]
    public async Task ExecuteAsync_EditsValidTask_ReturnsSuccess()
    {
        var task = new TaskModel { Id = 1, Description = "Old", Status = TaskStatus.Pending };
        _repoMock.Setup(r => r.GetTaskByIdAsync(1)).ReturnsAsync(task);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var cmd = new EditTaskCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "1", "New description" });
        Assert.Contains("Task 1 updated", result);
        Assert.Contains("Old: Old", result);
        Assert.Contains("New: New description", result);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTaskId_ReturnsError()
    {
        var cmd = new EditTaskCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "abc", "New" });
        Assert.Contains("Invalid task ID", result);
    }

    [Fact]
    public async Task ExecuteAsync_TaskNotFound_ReturnsError()
    {
        _repoMock.Setup(r => r.GetTaskByIdAsync(99)).ReturnsAsync((TaskModel?)null);
        var cmd = new EditTaskCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "99", "New" });
        Assert.Contains("Task 99 not found", result);
    }

    [Fact]
    public async Task ExecuteAsync_TaskDeleted_ReturnsError()
    {
        var task = new TaskModel { Id = 1, Description = "Test", Status = TaskStatus.Deleted };
        _repoMock.Setup(r => r.GetTaskByIdAsync(1)).ReturnsAsync(task);
        var cmd = new EditTaskCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "1", "New" });
        Assert.Contains("Cannot edit deleted task", result);
    }

    [Fact]
    public async Task ExecuteAsync_TooLongDescription_ReturnsError()
    {
        var task = new TaskModel { Id = 1, Description = "Old", Status = TaskStatus.Pending };
        _repoMock.Setup(r => r.GetTaskByIdAsync(1)).ReturnsAsync(task);
        var cmd = new EditTaskCommand(_repoMock.Object);
        string longDesc = new('a', 201);
        var result = await cmd.ExecuteAsync(new[] { "1", longDesc });
        Assert.Contains("Task description too long", result);
    }

    [Fact]
    public async Task ExecuteAsync_InsufficientParameters_ReturnsError()
    {
        var cmd = new EditTaskCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "1" });
        Assert.Contains("Please provide task ID and new description", result);
    }
} 