using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using TaskManager.CLI.Utilities;
using System;
using System.Collections.Generic;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;

namespace TaskManager.CLI.Tests;

public class StatsCommandTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();
    private readonly Mock<ConsoleHelper> _consoleMock = new();

    [Fact]
    public async Task ExecuteAsync_ShowsDetailedStatistics()
    {
        var stats = new DayStatistics
        {
            TotalFocusTime = TimeSpan.FromHours(6),
            TotalBreakTime = TimeSpan.FromHours(1),
            TasksCompleted = 8,
            FocusSessionsCompleted = 12,
            BreakSessionsCompleted = 12,
            ProductivityScore = 0.857
        };
        var session = new FocusSession
        {
            FocusMinutes = 25,
            BreakMinutes = 5
        };
        var tasks = new List<TaskModel>
        {
            new() { Status = TaskStatus.Pending },
            new() { Status = TaskStatus.Completed },
            new() { Status = TaskStatus.InProgress }
        };
        _repoMock.Setup(r => r.GetDayStatisticsAsync(It.IsAny<DateTime>())).ReturnsAsync(stats);
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new StatsCommand(_repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Detailed Statistics", result);
        Assert.Contains("Focus Time: 06:00:00", result);
        Assert.Contains("Break Time: 01:00:00", result);
        Assert.Contains("Total Active: 07:00:00", result);
    }

    [Fact]
    public async Task ExecuteAsync_CalculatesProductivityMetrics()
    {
        var stats = new DayStatistics
        {
            TotalFocusTime = TimeSpan.FromHours(4),
            TotalBreakTime = TimeSpan.FromMinutes(30),
            TasksCompleted = 5,
            FocusSessionsCompleted = 8,
            BreakSessionsCompleted = 8,
            ProductivityScore = 0.889
        };
        var session = new FocusSession
        {
            FocusMinutes = 30,
            BreakMinutes = 5
        };
        var tasks = new List<TaskModel>
        {
            new() { Status = TaskStatus.Pending },
            new() { Status = TaskStatus.Completed }
        };
        _repoMock.Setup(r => r.GetDayStatisticsAsync(It.IsAny<DateTime>())).ReturnsAsync(stats);
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new StatsCommand(_repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Productivity Score: 88.9%", result);
        Assert.Contains("Avg Focus/Session: 30.0 minutes", result);
        Assert.Contains("Focus/Break Ratio: 8.0:1", result);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroSessions_HandlesGracefully()
    {
        var stats = new DayStatistics
        {
            TotalFocusTime = TimeSpan.Zero,
            TotalBreakTime = TimeSpan.Zero,
            TasksCompleted = 0,
            FocusSessionsCompleted = 0,
            BreakSessionsCompleted = 0,
            ProductivityScore = 0.0
        };
        var session = new FocusSession
        {
            FocusMinutes = 25,
            BreakMinutes = 5
        };
        var tasks = new List<TaskModel>();
        _repoMock.Setup(r => r.GetDayStatisticsAsync(It.IsAny<DateTime>())).ReturnsAsync(stats);
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new StatsCommand(_repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Productivity Score: 0.0%", result);
        Assert.Contains("Avg Focus/Session: 0.0 minutes", result);
        Assert.Contains("Focus/Break Ratio: 0.0:1", result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowsTaskCounts()
    {
        var stats = new DayStatistics
        {
            TotalFocusTime = TimeSpan.FromHours(2),
            TotalBreakTime = TimeSpan.FromMinutes(20),
            TasksCompleted = 3,
            FocusSessionsCompleted = 4,
            BreakSessionsCompleted = 4,
            ProductivityScore = 0.857
        };
        var session = new FocusSession
        {
            FocusMinutes = 30,
            BreakMinutes = 5
        };
        var tasks = new List<TaskModel>
        {
            new() { Status = TaskStatus.Pending },
            new() { Status = TaskStatus.Pending },
            new() { Status = TaskStatus.Completed },
            new() { Status = TaskStatus.InProgress }
        };
        _repoMock.Setup(r => r.GetDayStatisticsAsync(It.IsAny<DateTime>())).ReturnsAsync(stats);
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new StatsCommand(_repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Tasks Completed: 3", result);
        Assert.Contains("Total Tasks: 4 | Pending: 2", result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowsTimerSettings()
    {
        var stats = new DayStatistics
        {
            TotalFocusTime = TimeSpan.FromHours(1),
            TotalBreakTime = TimeSpan.FromMinutes(10),
            TasksCompleted = 2,
            FocusSessionsCompleted = 2,
            BreakSessionsCompleted = 2,
            ProductivityScore = 0.857
        };
        var session = new FocusSession
        {
            FocusMinutes = 45,
            BreakMinutes = 15
        };
        var tasks = new List<TaskModel>();
        _repoMock.Setup(r => r.GetDayStatisticsAsync(It.IsAny<DateTime>())).ReturnsAsync(stats);
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        var cmd = new StatsCommand(_repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Focus Duration: 45 min", result);
        Assert.Contains("Break Duration: 15 min", result);
    }
} 