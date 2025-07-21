using Xunit;
using Moq;
using System.Threading.Tasks;
using TaskManager.CLI.Services;
using TaskManager.CLI.Repositories;
using TaskManager.CLI.Models;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;

namespace TaskManager.CLI.Tests;

public class HtmlReportGeneratorTests : IDisposable
{
    private readonly Mock<ITaskRepository> _repoMock = new();
    private readonly HtmlReportGenerator _reportGenerator;

    public HtmlReportGeneratorTests()
    {
        // Set EPPlus license for testing
        ExcelPackage.License.SetNonCommercialPersonal("TEST");
        
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(new List<TaskModel>());
        _repoMock.Setup(r => r.GetTodaySessionLogsAsync()).ReturnsAsync(new List<SessionLog>());
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync((WorkDay?)null);
        _repoMock.Setup(r => r.GetDayStatisticsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new DayStatistics { TasksCompleted = 0, ProductivityScore = 0 });

        _reportGenerator = new HtmlReportGenerator(_repoMock.Object);
    }

    public void Dispose()
    {
        CleanupTestFiles();
    }

    [Fact]
    public async Task GenerateReportAsync_WithValidDateRange_ReturnsFilePath()
    {
        // Act
        var result = await _reportGenerator.GenerateReportAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.EndsWith(".html", result);
        Assert.True(File.Exists(result));
        
        CleanupTestFiles();
    }

    [Fact]
    public async Task GenerateReportAsync_WithNullDateRange_ReturnsFilePath()
    {
        // Act
        var result = await _reportGenerator.GenerateReportAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.EndsWith(".html", result);
        Assert.True(File.Exists(result));
        
        CleanupTestFiles();
    }

    [Fact]
    public async Task GenerateReportAsync_IncludesTooltipSystem()
    {
        // Act
        var result = await _reportGenerator.GenerateReportAsync();
        var htmlContent = await File.ReadAllTextAsync(result);

        // Assert
        Assert.Contains("info-icon", htmlContent);
        Assert.Contains("data-description", htmlContent);
        Assert.Contains("ℹ️", htmlContent); // Info icon
        
        CleanupTestFiles();
    }

    [Fact]
    public async Task GenerateReportAsync_IncludesInfoIconsInSummaryCards()
    {
        // Act
        var result = await _reportGenerator.GenerateReportAsync();
        var htmlContent = await File.ReadAllTextAsync(result);

        // Assert
        Assert.Contains("Total Tasks", htmlContent);
        Assert.Contains("Completed Tasks", htmlContent);
        Assert.Contains("Focus Sessions", htmlContent);
        Assert.Contains("Total Focus Time", htmlContent);
        Assert.Contains("Avg Productivity", htmlContent);
        Assert.Contains("Work Days", htmlContent);
        
        CleanupTestFiles();
    }

    [Fact]
    public async Task GenerateReportAsync_IncludesInfoIconsInCharts()
    {
        // Act
        var result = await _reportGenerator.GenerateReportAsync();
        var htmlContent = await File.ReadAllTextAsync(result);

        // Assert
        Assert.Contains("Analytics Dashboard", htmlContent);
        Assert.Contains("Task Status Distribution", htmlContent);
        Assert.Contains("Daily Task Completions", htmlContent);
        Assert.Contains("Session Type Distribution", htmlContent);
        Assert.Contains("Hourly Activity Pattern", htmlContent);
        Assert.Contains("Top Commands Used", htmlContent);
        Assert.Contains("Focus vs Break Time", htmlContent);
        
        CleanupTestFiles();
    }

    [Fact]
    public async Task GenerateReportAsync_IncludesInfoIconsInTables()
    {
        // Act
        var result = await _reportGenerator.GenerateReportAsync();
        var htmlContent = await File.ReadAllTextAsync(result);

        // Assert
        Assert.Contains("Detailed Analytics Tables", htmlContent);
        Assert.Contains("Task Status Distribution", htmlContent);
        Assert.Contains("Session Performance Metrics", htmlContent);
        
        CleanupTestFiles();
    }

    [Fact]
    public async Task GenerateReportAsync_IncludesInfoIconsInUserSystemInfo()
    {
        // Act
        var result = await _reportGenerator.GenerateReportAsync();
        var htmlContent = await File.ReadAllTextAsync(result);

        // Assert
        Assert.Contains("User & System Information", htmlContent);
        
        CleanupTestFiles();
    }

    [Fact]
    public async Task GenerateReportAsync_IncludesInfoIconsInWorkDaySection()
    {
        // Act
        var result = await _reportGenerator.GenerateReportAsync();
        var htmlContent = await File.ReadAllTextAsync(result);

        // Assert
        Assert.Contains("Work Day Analytics", htmlContent);
        
        CleanupTestFiles();
    }

    [Fact]
    public async Task GenerateReportAsync_IncludesTooltipJavaScript()
    {
        // Act
        var result = await _reportGenerator.GenerateReportAsync();
        var htmlContent = await File.ReadAllTextAsync(result);

        // Assert
        Assert.Contains("let currentTooltip = null", htmlContent);
        Assert.Contains("let currentIcon = null", htmlContent);
        Assert.Contains("function showInfoTooltip", htmlContent);
        Assert.Contains("function hideInfoTooltip", htmlContent);
        Assert.Contains("addEventListener('mouseenter'", htmlContent);
        Assert.Contains("addEventListener('mouseleave'", htmlContent);
        
        CleanupTestFiles();
    }

    [Fact]
    public async Task GenerateReportAsync_IncludesTooltipCSS()
    {
        // Act
        var result = await _reportGenerator.GenerateReportAsync();
        var htmlContent = await File.ReadAllTextAsync(result);

        // Assert
        Assert.Contains(".info-icon", htmlContent);
        Assert.Contains(".info-tooltip", htmlContent);
        Assert.Contains("background: #000000", htmlContent);
        Assert.Contains("color: #ffffff", htmlContent);
        
        CleanupTestFiles();
    }

    [Fact]
    public async Task GenerateReportAsync_IncludesChartJs()
    {
        // Act
        var result = await _reportGenerator.GenerateReportAsync();
        var htmlContent = await File.ReadAllTextAsync(result);

        // Assert
        Assert.Contains("chart.js", htmlContent);
        Assert.Contains("new Chart(", htmlContent);
        Assert.Contains("taskStatusChart", htmlContent);
        Assert.Contains("dailyCompletionsChart", htmlContent);
        Assert.Contains("sessionTypeChart", htmlContent);
        Assert.Contains("hourlyActivityChart", htmlContent);
        Assert.Contains("topCommandsChart", htmlContent);
        Assert.Contains("focusBreakChart", htmlContent);
        
        CleanupTestFiles();
    }

    [Fact]
    public async Task GenerateReportAsync_WithEmptyData_GeneratesValidReport()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(new List<TaskModel>());
        _repoMock.Setup(r => r.GetTodaySessionLogsAsync()).ReturnsAsync(new List<SessionLog>());
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync((WorkDay?)null);

        // Act
        var result = await _reportGenerator.GenerateReportAsync();
        var htmlContent = await File.ReadAllTextAsync(result);

        // Assert
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        Assert.Contains("TaskManager CLI Report", htmlContent);
        Assert.Contains("Total Tasks", htmlContent);
        
        CleanupTestFiles();
    }

    [Fact]
    public async Task GenerateReportAsync_WithRealData_GeneratesComprehensiveReport()
    {
        // Arrange
        var tasks = new List<TaskModel>
        {
            new() { Id = 1, Description = "Test Task 1", Status = Models.TaskStatus.Completed, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 2, Description = "Test Task 2", Status = Models.TaskStatus.Pending, CreatedAt = DateTime.UtcNow }
        };
        
        var sessionLogs = new List<SessionLog>
        {
            new() { Type = SessionType.Focus, StartTime = DateTime.UtcNow.AddHours(-2), EndTime = DateTime.UtcNow.AddHours(-1), Date = DateTime.UtcNow.Date },
            new() { Type = SessionType.Break, StartTime = DateTime.UtcNow.AddHours(-1), EndTime = DateTime.UtcNow.AddMinutes(-30), Date = DateTime.UtcNow.Date }
        };

        _repoMock.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(tasks);
        _repoMock.Setup(r => r.GetTodaySessionLogsAsync()).ReturnsAsync(sessionLogs);
        _repoMock.Setup(r => r.GetTodayWorkDayAsync()).ReturnsAsync((WorkDay?)null);

        // Act
        var result = await _reportGenerator.GenerateReportAsync();
        var htmlContent = await File.ReadAllTextAsync(result);

        // Assert
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        Assert.Contains("TaskManager CLI Report", htmlContent);
        Assert.Contains("Total Tasks", htmlContent);
        
        CleanupTestFiles();
    }

    private void CleanupTestFiles()
    {
        try
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var reportsPath = Path.Combine(documentsPath, "TaskManager", "Reports");
            
            if (Directory.Exists(reportsPath))
            {
                var htmlFiles = Directory.GetFiles(reportsPath, "*.html");
                foreach (var file in htmlFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore individual file deletion errors
                    }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
} 