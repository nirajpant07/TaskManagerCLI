using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using System;

namespace TaskManager.CLI.Tests;

public class UptimeCommandTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();

    [Fact]
    public async Task ExecuteAsync_ShowsDailyTimeSummary()
    {
        var session = new FocusSession
        {
            TotalFocusTime = TimeSpan.FromHours(4),
            TotalBreakTime = TimeSpan.FromMinutes(30),
            CompletedFocusSessions = 8,
            CompletedBreakSessions = 8
        };
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync((WorkDay?)null);
        var cmd = new UptimeCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Daily Time Summary", result);
        Assert.Contains("Focus Time: 04:00:00", result);
        Assert.Contains("Break Time: 00:30:00", result);
        Assert.Contains("Total Active: 04:30:00", result);
    }

    [Fact]
    public async Task ExecuteAsync_CalculatesFocusEfficiency()
    {
        var session = new FocusSession
        {
            TotalFocusTime = TimeSpan.FromHours(6),
            TotalBreakTime = TimeSpan.FromHours(1),
            CompletedFocusSessions = 12,
            CompletedBreakSessions = 12
        };
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync((WorkDay?)null);
        var cmd = new UptimeCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Focus Efficiency: 85.7%", result); // 6 hours / 7 hours ≈ 85.7%
    }

    [Fact]
    public async Task ExecuteAsync_ActiveWorkDay_ShowsProgress()
    {
        var session = new FocusSession
        {
            TotalFocusTime = TimeSpan.FromHours(3),
            TotalBreakTime = TimeSpan.FromMinutes(30),
            CompletedFocusSessions = 6,
            CompletedBreakSessions = 6
        };
        var workDay = new WorkDay
        {
            StartTime = DateTime.UtcNow.AddHours(-4),
            PlannedDuration = TimeSpan.FromHours(8.5),
            IsActive = true
        };
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync(workDay);
        var cmd = new UptimeCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Work Day Progress", result);
        Assert.Contains("Elapsed:", result);
        Assert.Contains("Remaining:", result);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroTime_HandlesGracefully()
    {
        var session = new FocusSession
        {
            TotalFocusTime = TimeSpan.Zero,
            TotalBreakTime = TimeSpan.Zero,
            CompletedFocusSessions = 0,
            CompletedBreakSessions = 0
        };
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync((WorkDay?)null);
        var cmd = new UptimeCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Focus Time: 00:00:00", result);
        Assert.Contains("Break Time: 00:00:00", result);
        Assert.Contains("Focus Efficiency: 0.0%", result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowsSessionCounts()
    {
        var session = new FocusSession
        {
            TotalFocusTime = TimeSpan.FromHours(2),
            TotalBreakTime = TimeSpan.FromMinutes(20),
            CompletedFocusSessions = 4,
            CompletedBreakSessions = 4
        };
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync((WorkDay?)null);
        var cmd = new UptimeCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Focus: 4 | Break: 4", result);
    }

    [Fact]
    public async Task ExecuteAsync_NoActiveWorkDay_NoProgressSection()
    {
        var session = new FocusSession
        {
            TotalFocusTime = TimeSpan.FromHours(1),
            TotalBreakTime = TimeSpan.FromMinutes(10),
            CompletedFocusSessions = 2,
            CompletedBreakSessions = 2
        };
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync((WorkDay?)null);
        var cmd = new UptimeCommand(_repoMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.DoesNotContain("Work Day Progress", result);
    }
} 