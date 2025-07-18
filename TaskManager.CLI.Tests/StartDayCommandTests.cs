using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Services;
using TaskManager.CLI.Utilities;
using System;

namespace TaskManager.CLI.Tests;

public class StartDayCommandTests
{
    private readonly Mock<IWorkDayManagerService> _workDayMock = new();
    private readonly Mock<ConsoleHelper> _consoleMock = new();

    [Fact]
    public async Task ExecuteAsync_StartsWorkDay_ReturnsSuccess()
    {
        var workDay = new WorkDay
        {
            StartTime = DateTime.UtcNow,
            PlannedDuration = TimeSpan.FromHours(8.5),
            IsActive = true
        };
        _workDayMock.Setup(w => w.StartWorkDayAsync()).ReturnsAsync(workDay);
        var cmd = new StartDayCommand(_workDayMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Work day started", result);
        Assert.Contains("8.5 hours", result);
        Assert.Contains("Ready to be productive", result);
    }

    [Fact]
    public async Task ExecuteAsync_WorkDayAlreadyActive_ReturnsWarning()
    {
        _workDayMock.Setup(w => w.IsWorkDayActiveAsync()).ReturnsAsync(true);
        var cmd = new StartDayCommand(_workDayMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Work day is already active", result);
        Assert.Contains("Use '!workday' to check status", result);
    }

    [Fact]
    public async Task ExecuteAsync_ShowsPlannedEndTime()
    {
        var startTime = DateTime.UtcNow;
        var workDay = new WorkDay
        {
            StartTime = startTime,
            PlannedDuration = TimeSpan.FromHours(8.5),
            IsActive = true
        };
        _workDayMock.Setup(w => w.StartWorkDayAsync()).ReturnsAsync(workDay);
        var cmd = new StartDayCommand(_workDayMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        var expectedEndTime = startTime.Add(TimeSpan.FromHours(8.5));
        Assert.Contains(expectedEndTime.ToString("HH:mm"), result);
    }

    [Fact]
    public async Task ExecuteAsync_IncludesHelpfulInstructions()
    {
        var workDay = new WorkDay
        {
            StartTime = DateTime.UtcNow,
            PlannedDuration = TimeSpan.FromHours(8.5),
            IsActive = true
        };
        _workDayMock.Setup(w => w.StartWorkDayAsync()).ReturnsAsync(workDay);
        var cmd = new StartDayCommand(_workDayMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Use '!task' to add tasks", result);
        Assert.Contains("!focus next", result);
    }

    [Fact]
    public async Task ExecuteAsync_MentionsBackupAndWarning()
    {
        var workDay = new WorkDay
        {
            StartTime = DateTime.UtcNow,
            PlannedDuration = TimeSpan.FromHours(8.5),
            IsActive = true
        };
        _workDayMock.Setup(w => w.StartWorkDayAsync()).ReturnsAsync(workDay);
        var cmd = new StartDayCommand(_workDayMock.Object, _consoleMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("warning 15 minutes before end", result);
        Assert.Contains("Daily backup will be created", result);
    }
} 