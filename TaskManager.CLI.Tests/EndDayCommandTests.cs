using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Services;
using TaskManager.CLI.Utilities;
using System;

namespace TaskManager.CLI.Tests;

public class EndDayCommandTests
{
    private readonly Mock<IWorkDayManagerService> _workDayMock = new();
    private readonly Mock<ConsoleHelper> _consoleMock = new();

    [Fact]
    public async Task ExecuteAsync_EndsWorkDay_ReturnsSuccess()
    {
        var startTime = DateTime.UtcNow.AddHours(-8);
        var endTime = DateTime.UtcNow;
        var workDay = new WorkDay
        {
            StartTime = startTime,
            EndTime = endTime,
            IsActive = false
        };
        _workDayMock.Setup(w => w.IsWorkDayActiveAsync()).ReturnsAsync(true);
        _workDayMock.Setup(w => w.EndWorkDayAsync()).ReturnsAsync(workDay);
        var cmd = new EndDayCommand(_workDayMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Work day ended", result);
        Assert.Contains("Total duration", result);
        Assert.Contains("Daily backup created", result);
    }

    [Fact]
    public async Task ExecuteAsync_NoActiveWorkDay_ReturnsWarning()
    {
        _workDayMock.Setup(w => w.IsWorkDayActiveAsync()).ReturnsAsync(false);
        var cmd = new EndDayCommand(_workDayMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("No active work day to end", result);
        Assert.Contains("Use '!startday' to begin", result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowsCorrectDuration()
    {
        var startTime = DateTime.UtcNow.AddHours(-6);
        var endTime = DateTime.UtcNow;
        var workDay = new WorkDay
        {
            StartTime = startTime,
            EndTime = endTime,
            IsActive = false
        };
        _workDayMock.Setup(w => w.IsWorkDayActiveAsync()).ReturnsAsync(true);
        _workDayMock.Setup(w => w.EndWorkDayAsync()).ReturnsAsync(workDay);
        var cmd = new EndDayCommand(_workDayMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("06:00", result); // 6 hours duration
    }

    [Fact]
    public async Task ExecuteAsync_ShowsEndTime()
    {
        var endTime = DateTime.UtcNow;
        var workDay = new WorkDay
        {
            StartTime = endTime.AddHours(-8),
            EndTime = endTime,
            IsActive = false
        };
        _workDayMock.Setup(w => w.IsWorkDayActiveAsync()).ReturnsAsync(true);
        _workDayMock.Setup(w => w.EndWorkDayAsync()).ReturnsAsync(workDay);
        var cmd = new EndDayCommand(_workDayMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains(endTime.ToString("HH:mm"), result);
    }

    [Fact]
    public async Task ExecuteAsync_IncludesProductivityMessage()
    {
        var workDay = new WorkDay
        {
            StartTime = DateTime.UtcNow.AddHours(-8),
            EndTime = DateTime.UtcNow,
            IsActive = false
        };
        _workDayMock.Setup(w => w.IsWorkDayActiveAsync()).ReturnsAsync(true);
        _workDayMock.Setup(w => w.EndWorkDayAsync()).ReturnsAsync(workDay);
        var cmd = new EndDayCommand(_workDayMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Productivity summary will be displayed in popup", result);
        Assert.Contains("Great work today", result);
        Assert.Contains("Rest well and see you tomorrow", result);
    }
} 