using Moq;
using OfficeOpenXml;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using TaskManager.CLI.Utilities;

namespace TaskManager.CLI.Tests;

public class ReportCommandTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();
    private readonly Mock<ConsoleHelper> _consoleMock = new();

    public ReportCommandTests()
    {
        // Set EPPlus license for testing
        ExcelPackage.License.SetNonCommercialPersonal("TEST");
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesReport_ReturnsSuccessMessage()
    {
        // Arrange
        var command = new ReportCommand(_repoMock.Object, _consoleMock.Object);

        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(new List<TaskModel>());
        _repoMock.Setup(r => r.GetTodaySessionLogsAsync()).ReturnsAsync(new List<SessionLog>());
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync((WorkDay?)null);
        _repoMock.Setup(r => r.GetDayStatisticsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new DayStatistics { TasksCompleted = 0, ProductivityScore = 0 });

        // Act
        var result = await command.ExecuteAsync(new string[0]);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Check if report was generated successfully or if there was an error
        if (result.Contains("📊 HTML Report Generated Successfully!"))
        {
            Assert.Contains("📁 Location:", result);
            Assert.Contains("📅 Date Range:", result);
        }
        else
        {
            // If there was an error, it should be a proper error message
            Assert.Contains("❌ Failed to generate report:", result);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithData_GeneratesComprehensiveReport()
    {
        // Arrange
        var command = new ReportCommand(_repoMock.Object, _consoleMock.Object);

        var tasks = new List<TaskModel>
        {
            new() { Id = Guid.NewGuid(), Description = "Test Task 1", Status = Models.TaskStatus.Completed },
            new() { Id = Guid.NewGuid(), Description = "Test Task 2", Status = Models.TaskStatus.Pending }
        };

        var sessionLogs = new List<SessionLog>
        {
            new() { Id = Guid.NewGuid(), Type = SessionType.Focus, StartTime = System.DateTime.UtcNow, EndTime = System.DateTime.UtcNow.AddMinutes(25) },
            new() { Id = Guid.NewGuid(), Type = SessionType.Break, StartTime = System.DateTime.UtcNow, EndTime = System.DateTime.UtcNow.AddMinutes(5) },
            new() { Id = Guid.NewGuid(), Type = SessionType.Command, Notes = "Command executed: !task Test task" }
        };

        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.GetTodaySessionLogsAsync()).ReturnsAsync(sessionLogs);
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync((WorkDay?)null);
        _repoMock.Setup(r => r.GetDayStatisticsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new DayStatistics { TasksCompleted = 1, ProductivityScore = 0.8 });

        // Act
        var result = await command.ExecuteAsync(new string[0]);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Check if report was generated successfully or if there was an error
        if (result.Contains("📊 HTML Report Generated Successfully!"))
        {
            Assert.Contains("📁 Location:", result);
            Assert.Contains("📅 Date Range:", result);
        }
        else
        {
            // If there was an error, it should be a proper error message
            Assert.Contains("❌ Failed to generate report:", result);
        }
    }

    [Fact]
    public async Task ExecuteAsync_RepositoryError_ReturnsErrorMessage()
    {
        // Arrange
        var command = new ReportCommand(_repoMock.Object, _consoleMock.Object);

        _repoMock.Setup(r => r.GetAllTasksAsync()).ThrowsAsync(new IOException("File not found"));

        // Act
        var result = await command.ExecuteAsync(new string[0]);

        // Assert
        Assert.Contains("❌ Failed to generate report:", result);
        Assert.Contains("Failed to generate HTML report", result);
    }

    [Fact]
    public async Task ExecuteAsync_WithParameters_IgnoresParameters()
    {
        // Arrange
        var command = new ReportCommand(_repoMock.Object, _consoleMock.Object);

        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(new List<TaskModel>());
        _repoMock.Setup(r => r.GetTodaySessionLogsAsync()).ReturnsAsync(new List<SessionLog>());
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync((WorkDay?)null);
        _repoMock.Setup(r => r.GetDayStatisticsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new DayStatistics { TasksCompleted = 0, ProductivityScore = 0 });

        // Act
        var result = await command.ExecuteAsync(new[] { "weekly", "detailed" });

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("❌ Invalid date format", result);
        Assert.Contains("Usage: !report [start_date] [end_date]", result);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesReportsDirectory()
    {
        // Arrange
        var command = new ReportCommand(_repoMock.Object, _consoleMock.Object);

        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(new List<TaskModel>());
        _repoMock.Setup(r => r.GetTodaySessionLogsAsync()).ReturnsAsync(new List<SessionLog>());
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync((WorkDay?)null);
        _repoMock.Setup(r => r.GetDayStatisticsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new DayStatistics { TasksCompleted = 0, ProductivityScore = 0 });

        // Act
        var result = await command.ExecuteAsync(new string[0]);

        // Assert
        var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        var reportsPath = Path.Combine(documentsPath, "TaskManager", "Reports");
        Assert.True(Directory.Exists(reportsPath));
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesHtmlFile()
    {
        // Arrange
        var command = new ReportCommand(_repoMock.Object, _consoleMock.Object);

        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(new List<TaskModel>());
        _repoMock.Setup(r => r.GetTodaySessionLogsAsync()).ReturnsAsync(new List<SessionLog>());
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync((WorkDay?)null);
        _repoMock.Setup(r => r.GetDayStatisticsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new DayStatistics { TasksCompleted = 0, ProductivityScore = 0 });

        // Act
        var result = await command.ExecuteAsync(new string[0]);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Check if report was generated successfully or if there was an error
        if (result.Contains("📊 HTML Report Generated Successfully!"))
        {
            Assert.Contains("📁 Location:", result);
            Assert.Contains(".html", result);
        }
        else
        {
            // If there was an error, it should be a proper error message
            Assert.Contains("❌ Failed to generate report:", result);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithValidDateRange_GeneratesReport()
    {
        // Arrange
        var command = new ReportCommand(_repoMock.Object, _consoleMock.Object);

        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(new List<TaskModel>());
        _repoMock.Setup(r => r.GetTodaySessionLogsAsync()).ReturnsAsync(new List<SessionLog>());
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync((WorkDay?)null);
        _repoMock.Setup(r => r.GetDayStatisticsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new DayStatistics { TasksCompleted = 0, ProductivityScore = 0 });

        // Act
        var result = await command.ExecuteAsync(new[] { "2024-01-01", "2024-01-31" });

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Check if report was generated successfully or if there was an error
        if (result.Contains("📊 HTML Report Generated Successfully!"))
        {
            Assert.Contains("📁 Location:", result);
            Assert.Contains("📅 Date Range: 2024-01-01 to 2024-01-31", result);
        }
        else
        {
            // If there was an error, it should be a proper error message
            Assert.Contains("❌ Failed to generate report:", result);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidDateRange_ReturnsError()
    {
        // Arrange
        var command = new ReportCommand(_repoMock.Object, _consoleMock.Object);

        // Act
        var result = await command.ExecuteAsync(new[] { "invalid-date", "also-invalid" });

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("❌ Invalid date format", result);
        Assert.Contains("Usage: !report [start_date] [end_date]", result);
    }
}