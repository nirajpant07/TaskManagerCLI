using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using TaskManager.CLI.Services;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;

namespace TaskManager.CLI.Tests;

public class TimerCommandTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();
    private readonly Mock<ITimerService> _timerMock = new();
    private readonly Mock<INotificationService> _notificationMock = new();
    private readonly Mock<ISoundService> _soundMock = new();

    [Fact]
    public async Task ExecuteAsync_NoParameters_ShowsCurrentSettings()
    {
        _timerMock.Setup(t => t.GetCurrentSettings()).Returns((25, 5));
        var session = new FocusSession { FocusMinutes = 25, BreakMinutes = 5 };
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        var cmd = new TimerCommand(_repoMock.Object, _timerMock.Object, _notificationMock.Object, _soundMock.Object);
        var result = await cmd.ExecuteAsync(new string[0]);
        Assert.Contains("Current timer settings", result);
        Assert.Contains("Focus: 25 minutes", result);
        Assert.Contains("Break: 5 minutes", result);
    }

    [Fact]
    public async Task ExecuteAsync_ValidSettings_UpdatesTimer()
    {
        var session = new FocusSession { FocusMinutes = 25, BreakMinutes = 5 };
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _repoMock.Setup(r => r.UpdateSessionAsync(It.IsAny<FocusSession>())).Returns(Task.CompletedTask);
        var cmd = new TimerCommand(_repoMock.Object, _timerMock.Object, _notificationMock.Object, _soundMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "30/10" });
        Assert.Contains("Timer updated successfully", result);
        Assert.Contains("Focus sessions: 30 minutes", result);
        Assert.Contains("Break sessions: 10 minutes", result);
        _timerMock.Verify(t => t.SetTimer(30, 10), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidFormat_ReturnsError()
    {
        var cmd = new TimerCommand(_repoMock.Object, _timerMock.Object, _notificationMock.Object, _soundMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "25" });
        Assert.Contains("Invalid timer format", result);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidTimeValues_ReturnsError()
    {
        var cmd = new TimerCommand(_repoMock.Object, _timerMock.Object, _notificationMock.Object, _soundMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "abc/def" });
        Assert.Contains("Invalid time values", result);
    }

    [Fact]
    public async Task ExecuteAsync_FocusTimeTooShort_ReturnsError()
    {
        var cmd = new TimerCommand(_repoMock.Object, _timerMock.Object, _notificationMock.Object, _soundMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "3/5" });
        Assert.Contains("Focus time must be between 5 and 120 minutes", result);
    }

    [Fact]
    public async Task ExecuteAsync_FocusTimeTooLong_ReturnsError()
    {
        var cmd = new TimerCommand(_repoMock.Object, _timerMock.Object, _notificationMock.Object, _soundMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "150/5" });
        Assert.Contains("Focus time must be between 5 and 120 minutes", result);
    }

    [Fact]
    public async Task ExecuteAsync_BreakTimeTooShort_ReturnsError()
    {
        var cmd = new TimerCommand(_repoMock.Object, _timerMock.Object, _notificationMock.Object, _soundMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "25/1" });
        Assert.Contains("Break time must be between 2 and 60 minutes", result);
    }

    [Fact]
    public async Task ExecuteAsync_BreakTimeTooLong_ReturnsError()
    {
        var cmd = new TimerCommand(_repoMock.Object, _timerMock.Object, _notificationMock.Object, _soundMock.Object);
        var result = await cmd.ExecuteAsync(new[] { "25/90" });
        Assert.Contains("Break time must be between 2 and 60 minutes", result);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesSessionSettings()
    {
        var session = new FocusSession { FocusMinutes = 25, BreakMinutes = 5 };
        _repoMock.Setup(r => r.GetTodaySessionAsync()).ReturnsAsync(session);
        _repoMock.Setup(r => r.UpdateSessionAsync(It.IsAny<FocusSession>())).Returns(Task.CompletedTask);
        var cmd = new TimerCommand(_repoMock.Object, _timerMock.Object, _notificationMock.Object, _soundMock.Object);
        await cmd.ExecuteAsync(new[] { "45/15" });
        _repoMock.Verify(r => r.UpdateSessionAsync(It.Is<FocusSession>(s => s.FocusMinutes == 45 && s.BreakMinutes == 15)), Times.Once);
    }
} 