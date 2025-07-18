using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using TaskManager.CLI.Services;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;

namespace TaskManager.CLI.Tests;

public class DoneCommandTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();
    private readonly Mock<IFocusSessionManagerService> _sessionMock = new();

    [Fact]
    public async Task ExecuteAsync_CompletesSingleTask_ReturnsSuccess()
    {
        var task = new TaskModel { Id = 1, Description = "Test", Status = TaskStatus.Pending };
        _repoMock.Setup(r => r.GetTaskByIdAsync(1)).ReturnsAsync(task);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var cmd = new DoneCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "1" });
        Assert.Contains("Task 1 completed", result);
    }

    [Fact]
    public async Task ExecuteAsync_CompletesMultipleTasks_ReturnsSummary()
    {
        var task1 = new TaskModel { Id = 1, Description = "A", Status = TaskStatus.Pending };
        var task2 = new TaskModel { Id = 2, Description = "B", Status = TaskStatus.Pending };
        _repoMock.Setup(r => r.GetTaskByIdAsync(1)).ReturnsAsync(task1);
        _repoMock.Setup(r => r.GetTaskByIdAsync(2)).ReturnsAsync(task2);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        var cmd = new DoneCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "1,2" });
        Assert.Contains("Completed 2 task(s)", result);
        Assert.Contains("Task 1 completed", result);
        Assert.Contains("Task 2 completed", result);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTaskId_ReturnsError()
    {
        var cmd = new DoneCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "abc" });
        Assert.Contains("Invalid task ID", result);
    }

    [Fact]
    public async Task ExecuteAsync_TaskNotFound_ReturnsError()
    {
        _repoMock.Setup(r => r.GetTaskByIdAsync(99)).ReturnsAsync((TaskModel?)null);
        var cmd = new DoneCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "99" });
        Assert.Contains("Task 99 not found", result);
    }

    [Fact]
    public async Task ExecuteAsync_TaskAlreadyCompleted_ReturnsWarning()
    {
        var task = new TaskModel { Id = 1, Description = "Test", Status = TaskStatus.Completed };
        _repoMock.Setup(r => r.GetTaskByIdAsync(1)).ReturnsAsync(task);
        var cmd = new DoneCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "1" });
        Assert.Contains("Task 1 already completed", result);
    }

    [Fact]
    public async Task ExecuteAsync_NoParameters_ReturnsError()
    {
        var cmd = new DoneCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Please provide task ID", result);
    }

    [Fact]
    public async Task ExecuteAsync_CompletingFocusedTask_EndsSession()
    {
        var task = new TaskModel { Id = 1, Description = "Test", Status = TaskStatus.Pending, IsFocused = true };
        _repoMock.Setup(r => r.GetTaskByIdAsync(1)).ReturnsAsync(task);
        _repoMock.Setup(r => r.UpdateTaskAsync(It.IsAny<TaskModel>())).Returns(Task.CompletedTask);
        _sessionMock.Setup(s => s.EndCurrentSessionAsync()).Returns(Task.CompletedTask);
        var cmd = new DoneCommand(_repoMock.Object, _sessionMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "1" });
        Assert.Contains("Task 1 completed", result);
        _sessionMock.Verify(s => s.EndCurrentSessionAsync(), Times.Once);
    }
} 