using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using System.Collections.Generic;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;

namespace TaskManager.CLI.Tests;

public class AddTaskCommandTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();

    [Fact]
    public async Task ExecuteAsync_AddsSingleTask_ReturnsSuccess()
    {
        _repoMock.Setup(r => r.AddTaskAsync(It.IsAny<TaskModel>())).ReturnsAsync(1);
        var cmd = new AddTaskCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "Test task" });
        Assert.Contains("✅ Task 1 added", result);
    }

    [Fact]
    public async Task ExecuteAsync_AddsMultipleTasks_ReturnsSummary()
    {
        _repoMock.SetupSequence(r => r.AddTaskAsync(It.IsAny<TaskModel>()))
            .ReturnsAsync(1).ReturnsAsync(2);
        var cmd = new AddTaskCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "Task one, Task two" });
        Assert.Contains("📝 Added 2 tasks", result);
        Assert.Contains("Task 1 added", result);
        Assert.Contains("Task 2 added", result);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyInput_ReturnsError()
    {
        var cmd = new AddTaskCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Please provide a task description", result);
    }

    [Fact]
    public async Task ExecuteAsync_TooLongDescription_ReturnsWarning()
    {
        var cmd = new AddTaskCommand(_repoMock.Object);
        string longDesc = new('a', 201);
        var result = await cmd.ExecuteAsync(new[] { longDesc });
        Assert.Contains("Task description too long", result);
    }
} 