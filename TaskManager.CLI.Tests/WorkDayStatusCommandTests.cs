using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Services;
using TaskManager.CLI.Utilities;
using TaskManager.CLI.Repositories;
using System;

namespace TaskManager.CLI.Tests;

public class WorkDayStatusCommandTests
{
    private readonly Mock<IWorkDayManagerService> _workDayMock = new();
    private readonly Mock<ITaskRepository> _repoMock = new();
    private readonly Mock<ConsoleHelper> _consoleMock = new();

    [Fact]
    public async Task ExecuteAsync_ActiveWorkDay_ShowsStatus()
    {
        var startTime = DateTime.UtcNow.AddHours(-4);
        var workDay = new WorkDay
        {
            StartTime = startTime,
            PlannedDuration = TimeSpan.FromHours(8.5),
            IsActive = true
        };
        var session = new FocusSession
        {
            TotalFocusTime = TimeSpan.FromHours(3),
            TotalBreakTime = TimeSpan.FromMinutes(30),
            CompletedFocusSessions = 6,
            CompletedBreakSessions = 6,
            FocusMinutes = 25,
            BreakMinutes = 5
        };
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync(workDay);
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _workDayMock.Setup(w => w.GetRemainingWorkTimeAsync()).ReturnsAsync(TimeSpan.FromHours(4.5));
        var cmd = new WorkDayStatusCommand(_workDayMock.Object, _repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Work Day Status", result);
        Assert.Contains("Started:", result);
        Assert.Contains("Elapsed:", result);
        Assert.Contains("Remaining:", result);
    }

    [Fact]
    public async Task ExecuteAsync_NoActiveWorkDay_ReturnsMessage()
    {
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync((WorkDay?)null);
        var cmd = new WorkDayStatusCommand(_workDayMock.Object, _repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("No active work day", result);
        Assert.Contains("Use '!startday' to begin", result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowsProgressPercentage()
    {
        var startTime = DateTime.UtcNow.AddHours(-4);
        var workDay = new WorkDay
        {
            StartTime = startTime,
            PlannedDuration = TimeSpan.FromHours(8.5),
            IsActive = true
        };
        var session = new FocusSession();
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync(workDay);
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _workDayMock.Setup(w => w.GetRemainingWorkTimeAsync()).ReturnsAsync(TimeSpan.FromHours(4.5));
        var cmd = new WorkDayStatusCommand(_workDayMock.Object, _repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("47.1%", result); // 4 hours / 8.5 hours ≈ 47.1%
    }

    [Fact]
    public async Task ExecuteAsync_ShowsProductivityMetrics()
    {
        var workDay = new WorkDay
        {
            StartTime = DateTime.UtcNow.AddHours(-4),
            PlannedDuration = TimeSpan.FromHours(8.5),
            IsActive = true
        };
        var session = new FocusSession
        {
            TotalFocusTime = TimeSpan.FromHours(3),
            TotalBreakTime = TimeSpan.FromMinutes(30),
            CompletedFocusSessions = 6,
            CompletedBreakSessions = 6,
            FocusMinutes = 25,
            BreakMinutes = 5
        };
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync(workDay);
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _workDayMock.Setup(w => w.GetRemainingWorkTimeAsync()).ReturnsAsync(TimeSpan.FromHours(4.5));
        var cmd = new WorkDayStatusCommand(_workDayMock.Object, _repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Focus Time: 03:00", result);
        Assert.Contains("Break Time: 00:30", result);
        Assert.Contains("Focus/Break Ratio: 6.0:1", result);
        Assert.Contains("Focus: 6 | Break: 6", result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowsTimerSettings()
    {
        var workDay = new WorkDay
        {
            StartTime = DateTime.UtcNow.AddHours(-4),
            PlannedDuration = TimeSpan.FromHours(8.5),
            IsActive = true
        };
        var session = new FocusSession
        {
            FocusMinutes = 45,
            BreakMinutes = 15
        };
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync(workDay);
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _workDayMock.Setup(w => w.GetRemainingWorkTimeAsync()).ReturnsAsync(TimeSpan.FromHours(4.5));
        var cmd = new WorkDayStatusCommand(_workDayMock.Object, _repoMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Focus: 45 min | Break: 15 min", result);
    }
} 