using TaskManager.CLI.Repositories;
using TaskManager.CLI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;
using OfficeOpenXml;

namespace TaskManager.CLI.Services
{
    public class HtmlReportGenerator
    {
        private readonly ITaskRepository _repository;
        private readonly string _reportsPath;
        private readonly string _archivePath;

        public HtmlReportGenerator(ITaskRepository repository)
        {
            _repository = repository;
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _reportsPath = Path.Combine(documentsPath, "TaskManager", "Reports");
            _archivePath = Path.Combine(documentsPath, "TaskManager", "Archive");
            Directory.CreateDirectory(_reportsPath);
        }

        public async Task<string> GenerateReportAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var userName = Environment.UserName;
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var reportData = await AnalyzeDataAsync(start, end);
                var htmlContent = GenerateHtmlContent(reportData, start, end);

                var fileName = $"{userName}_{start:yyyyMMdd}_{end:yyyyMMdd}.html";
                var filePath = Path.Combine(_reportsPath, fileName);

                await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);
                return filePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to generate HTML report: {ex.Message}", ex);
            }
        }

        private async Task<ReportData> AnalyzeDataAsync(DateTime startDate, DateTime endDate)
        {
            var reportData = new ReportData();

            // Load current data
            var currentTasks = await _repository.GetAllTasksAsync();
            var currentSessionLogs = await _repository.GetTodaySessionLogsAsync();
            var currentWorkDay = await _repository.GetTodayWorkDayAsync();

            // Load archived data
            var archivedData = await LoadArchivedDataAsync(startDate, endDate);

            // Combine current and archived data
            var allTasks = new List<TaskModel>(currentTasks);
            var allSessionLogs = new List<SessionLog>(currentSessionLogs);
            var allWorkDays = new List<WorkDay>();

            if (currentWorkDay != null)
                allWorkDays.Add(currentWorkDay);

            allTasks.AddRange(archivedData.Tasks);
            allSessionLogs.AddRange(archivedData.SessionLogs);
            allWorkDays.AddRange(archivedData.WorkDays);

            // Filter by date range
            var filteredTasks = allTasks.Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate).ToList();
            var filteredSessionLogs = allSessionLogs.Where(s => s.Date >= startDate && s.Date <= endDate).ToList();
            var filteredWorkDays = allWorkDays.Where(w => w.Date >= startDate && w.Date <= endDate).ToList();

            // Task Analysis
            reportData.TotalTasks = filteredTasks.Count(t => t.Status != TaskStatus.Deleted);
            reportData.CompletedTasks = filteredTasks.Count(t => t.Status == TaskStatus.Completed);
            reportData.PendingTasks = filteredTasks.Count(t => t.Status == TaskStatus.Pending);
            reportData.InProgressTasks = filteredTasks.Count(t => t.Status == TaskStatus.InProgress);
            reportData.PausedTasks = filteredTasks.Count(t => t.Status == TaskStatus.Paused);
            reportData.CompletionRate = reportData.TotalTasks > 0 ? (double)reportData.CompletedTasks / reportData.TotalTasks * 100 : 0;

            // Session Analysis
            var focusSessions = filteredSessionLogs.Where(s => s.Type == SessionType.Focus).ToList();
            var breakSessions = filteredSessionLogs.Where(s => s.Type == SessionType.Break).ToList();
            var commandSessions = filteredSessionLogs.Where(s => s.Type == SessionType.Command).ToList();

            reportData.TotalFocusTime = focusSessions.Aggregate(TimeSpan.Zero, (total, s) => total + (s.EndTime.HasValue ? s.EndTime.Value - s.StartTime : TimeSpan.Zero));
            reportData.TotalBreakTime = breakSessions.Aggregate(TimeSpan.Zero, (total, s) => total + (s.EndTime.HasValue ? s.EndTime.Value - s.StartTime : TimeSpan.Zero));
            reportData.FocusSessionsCount = focusSessions.Count;
            reportData.BreakSessionsCount = breakSessions.Count;
            reportData.CommandsExecuted = commandSessions.Count;

            // Daily Statistics for the date range
            var totalProductivityScore = 0.0;
            var totalTasksCompleted = 0;
            var daysWithData = 0;

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                try
                {
                    var dayStats = await _repository.GetDayStatisticsAsync(date);
                    totalProductivityScore += dayStats.ProductivityScore;
                    totalTasksCompleted += dayStats.TasksCompleted;
                    daysWithData++;
                }
                catch
                {
                    // Day has no data, continue
                }
            }

            reportData.TodayProductivityScore = daysWithData > 0 ? totalProductivityScore / daysWithData : 0;
            reportData.TodayTasksCompleted = totalTasksCompleted;

            // Task Trends for the date range
            reportData.DailyTaskCompletions = new Dictionary<string, int>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                try
                {
                    var dayStats = await _repository.GetDayStatisticsAsync(date);
                    reportData.DailyTaskCompletions[date.ToString("MM/dd")] = dayStats.TasksCompleted;
                }
                catch
                {
                    reportData.DailyTaskCompletions[date.ToString("MM/dd")] = 0;
                }
            }

            // Session Type Distribution
            reportData.SessionTypeDistribution = filteredSessionLogs
                .GroupBy(s => s.Type)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            // Most Active Hours
            reportData.HourlyActivity = filteredSessionLogs
                .GroupBy(s => s.StartTime.Hour)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key.ToString("00"), g => g.Count());

            // Top Commands - Fixed to properly extract command names
            reportData.TopCommands = commandSessions
                .Where(s => !string.IsNullOrEmpty(s.Notes))
                .GroupBy(s => ExtractCommandName(s.Notes))
                .Where(g => !string.IsNullOrEmpty(g.Key) && g.Key != "Unknown")
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());

            // Work Day Statistics
            reportData.TotalWorkDays = filteredWorkDays.Count;
            reportData.CompletedWorkDays = filteredWorkDays.Count(w => w.EndTime.HasValue);
            reportData.TotalWorkTime = filteredWorkDays
                .Where(w => w.EndTime.HasValue)
                .Aggregate(TimeSpan.Zero, (total, w) => total + (w.EndTime!.Value - w.StartTime));

            // Load user and system information
            reportData.UserInfo = await LoadUserAndSystemInfoAsync();

            return reportData;
        }

        private async Task<ArchivedData> LoadArchivedDataAsync(DateTime startDate, DateTime endDate)
        {
            var archivedData = new ArchivedData();

            if (!Directory.Exists(_archivePath))
                return archivedData;

            var archiveDirectories = Directory.GetDirectories(_archivePath);

            foreach (var dir in archiveDirectories)
            {
                var dirName = Path.GetFileName(dir);
                if (!DateTime.TryParse(dirName, out var archiveDate))
                    continue;

                if (archiveDate < startDate.Date || archiveDate > endDate.Date)
                    continue;

                var backupFiles = Directory.GetFiles(dir, "tasks_backup_*.xlsx");
                if (backupFiles.Length == 0)
                    continue;

                // Use the latest backup from that day
                var latestBackup = backupFiles.OrderByDescending(f => f).First();

                try
                {
                    using var package = new ExcelPackage(new FileInfo(latestBackup));
                    await LoadArchivedTasksAsync(package, archivedData, archiveDate);
                    await LoadArchivedSessionLogsAsync(package, archivedData, archiveDate);
                    await LoadArchivedWorkDaysAsync(package, archivedData, archiveDate);
                }
                catch (Exception ex)
                {
                    // Log error but continue with other archives
                    Console.WriteLine($"Error loading archived data from {latestBackup}: {ex.Message}");
                    // Don't rethrow - continue processing other archives
                }
            }

            return archivedData;
        }

        private async Task LoadArchivedTasksAsync(ExcelPackage package, ArchivedData archivedData, DateTime archiveDate)
        {
            var taskSheet = package.Workbook.Worksheets["Tasks"];
            if (taskSheet?.Dimension == null) return;

            // Find the actual data start row by looking for the header row
            int dataStartRow = 6; // Default start row
            for (int row = 1; row <= Math.Min(10, taskSheet.Dimension.End.Row); row++)
            {
                var firstCellValue = taskSheet.Cells[row, 1].Value?.ToString() ?? "";
                if (firstCellValue == "Task ID" || firstCellValue == "ID")
                {
                    dataStartRow = row + 1; // Data starts after header
                    break;
                }
            }

            for (int row = dataStartRow; row <= taskSheet.Dimension.End.Row; row++)
            {
                try
                {
                    var idValue = taskSheet.Cells[row, 1].Value?.ToString();
                    if (string.IsNullOrEmpty(idValue) || !int.TryParse(idValue, out int taskId))
                        continue;

                    var description = taskSheet.Cells[row, 2].Value?.ToString() ?? "";
                    var statusValue = taskSheet.Cells[row, 3].Value?.ToString() ?? "Pending";
                    var createdAtValue = taskSheet.Cells[row, 4].Value?.ToString();
                    var completedAtValue = taskSheet.Cells[row, 5].Value?.ToString();
                    var pausedAtValue = taskSheet.Cells[row, 6].Value?.ToString();
                    var pauseReason = taskSheet.Cells[row, 7].Value?.ToString() ?? "";
                    var isFocusedValue = taskSheet.Cells[row, 8].Value?.ToString() ?? "false";
                    var focusTimeValue = taskSheet.Cells[row, 9].Value?.ToString();

                    // Skip rows that don't look like valid task data
                    if (string.IsNullOrEmpty(description) || description.Length < 2)
                        continue;

                    var task = new TaskModel
                    {
                        Id = taskId,
                        Description = description,
                        Status = Enum.TryParse<TaskStatus>(statusValue, out var status) ? status : TaskStatus.Pending,
                        CreatedAt = DateTime.TryParse(createdAtValue, out var createdAt) ? createdAt : DateTime.UtcNow,
                        CompletedAt = DateTime.TryParse(completedAtValue, out var completed) ? completed : null,
                        PausedAt = DateTime.TryParse(pausedAtValue, out var paused) ? paused : null,
                        PauseReason = pauseReason,
                        IsFocused = bool.TryParse(isFocusedValue, out var isFocused) ? isFocused : false,
                        FocusTime = TimeSpan.TryParse(focusTimeValue, out var focusTime) ? focusTime : TimeSpan.Zero
                    };
                    archivedData.Tasks.Add(task);
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other rows
                    Console.WriteLine($"Error processing task row {row}: {ex.Message}");
                    continue;
                }
            }
        }

        private async Task LoadArchivedSessionLogsAsync(ExcelPackage package, ArchivedData archivedData, DateTime archiveDate)
        {
            var sessionLogSheet = package.Workbook.Worksheets["SessionLogs"];
            if (sessionLogSheet?.Dimension == null) return;

            // Find the actual data start row by looking for the header row
            int dataStartRow = 6; // Default start row
            for (int row = 1; row <= Math.Min(10, sessionLogSheet.Dimension.End.Row); row++)
            {
                var firstCellValue = sessionLogSheet.Cells[row, 1].Value?.ToString() ?? "";
                if (firstCellValue == "Date" || firstCellValue == "Session Date")
                {
                    dataStartRow = row + 1; // Data starts after header
                    break;
                }
            }

            for (int row = dataStartRow; row <= sessionLogSheet.Dimension.End.Row; row++)
            {
                try
                {
                    var dateValue = sessionLogSheet.Cells[row, 1].Value?.ToString();
                    var startTimeValue = sessionLogSheet.Cells[row, 2].Value?.ToString();

                    if (string.IsNullOrEmpty(dateValue) || string.IsNullOrEmpty(startTimeValue))
                        continue;

                    if (!DateTime.TryParse(dateValue, out var date) || !DateTime.TryParse(startTimeValue, out var startTime))
                        continue;

                    var endTimeValue = sessionLogSheet.Cells[row, 3].Value?.ToString();
                    var typeValue = sessionLogSheet.Cells[row, 4].Value?.ToString() ?? "Command";
                    var taskIdValue = sessionLogSheet.Cells[row, 5].Value?.ToString();
                    var notes = sessionLogSheet.Cells[row, 6].Value?.ToString() ?? "";

                    var sessionLog = new SessionLog
                    {
                        Date = date,
                        StartTime = startTime,
                        EndTime = DateTime.TryParse(endTimeValue, out var endTime) ? endTime : null,
                        Type = Enum.TryParse<SessionType>(typeValue, out var type) ? type : SessionType.Command,
                        TaskId = int.TryParse(taskIdValue, out var taskId) ? taskId : null,
                        Notes = notes
                    };
                    archivedData.SessionLogs.Add(sessionLog);
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other rows
                    Console.WriteLine($"Error processing session log row {row}: {ex.Message}");
                    continue;
                }
            }
        }

        private async Task LoadArchivedWorkDaysAsync(ExcelPackage package, ArchivedData archivedData, DateTime archiveDate)
        {
            var workDaySheet = package.Workbook.Worksheets["WorkDays"];
            if (workDaySheet?.Dimension == null) return;

            // Find the actual data start row by looking for the header row
            int dataStartRow = 6; // Default start row
            for (int row = 1; row <= Math.Min(10, workDaySheet.Dimension.End.Row); row++)
            {
                var firstCellValue = workDaySheet.Cells[row, 1].Value?.ToString() ?? "";
                if (firstCellValue == "Date" || firstCellValue == "Work Day Date")
                {
                    dataStartRow = row + 1; // Data starts after header
                    break;
                }
            }

            for (int row = dataStartRow; row <= workDaySheet.Dimension.End.Row; row++)
            {
                try
                {
                    var dateValue = workDaySheet.Cells[row, 1].Value?.ToString();
                    var startTimeValue = workDaySheet.Cells[row, 2].Value?.ToString();

                    if (string.IsNullOrEmpty(dateValue) || string.IsNullOrEmpty(startTimeValue))
                        continue;

                    if (!DateTime.TryParse(dateValue, out var date) || !DateTime.TryParse(startTimeValue, out var startTime))
                        continue;

                    var endTimeValue = workDaySheet.Cells[row, 3].Value?.ToString();
                    var durationValue = workDaySheet.Cells[row, 4].Value?.ToString();
                    var isActiveValue = workDaySheet.Cells[row, 5].Value?.ToString() ?? "false";

                    var workDay = new WorkDay
                    {
                        Date = date,
                        StartTime = startTime,
                        EndTime = DateTime.TryParse(endTimeValue, out var endTime) ? endTime : null,
                        PlannedDuration = TimeSpan.TryParse(durationValue, out var duration) ? duration : TimeSpan.FromHours(8.5),
                        IsActive = bool.TryParse(isActiveValue, out var isActive) ? isActive : false
                    };
                    archivedData.WorkDays.Add(workDay);
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other rows
                    Console.WriteLine($"Error processing work day row {row}: {ex.Message}");
                    continue;
                }
            }
        }

        private async Task<Dictionary<string, string>> LoadUserAndSystemInfoAsync()
        {
            var userInfo = new Dictionary<string, string>();

            try
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var taskManagerFolder = Path.Combine(documentsPath, "TaskManager");
                var currentFilePath = Path.Combine(taskManagerFolder, "tasks.xlsx");

                if (File.Exists(currentFilePath))
                {
                    using var package = new ExcelPackage(new FileInfo(currentFilePath));
                    var userInfoSheet = package.Workbook.Worksheets["UserInfo"];

                    if (userInfoSheet?.Dimension != null)
                    {
                        for (int row = 6; row <= userInfoSheet.Dimension.End.Row; row++)
                        {
                            var name = userInfoSheet.Cells[row, 1].Value?.ToString();
                            var value = userInfoSheet.Cells[row, 2].Value?.ToString();

                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                            {
                                userInfo[name] = value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                userInfo["Error"] = $"Failed to load user info: {ex.Message}";
            }

            return userInfo;
        }

        private string ExtractCommandName(string notes)
        {
            if (string.IsNullOrEmpty(notes))
                return "Unknown";

            if (notes.StartsWith("Command executed: !"))
            {
                var command = notes.Substring("Command executed: !".Length);
                var firstSpace = command.IndexOf(' ');
                return firstSpace > 0 ? command.Substring(0, firstSpace) : command;
            }
            return "Unknown";
        }

        private string GenerateHtmlContent(ReportData data, DateTime startDate, DateTime endDate)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("    <title>TaskManager CLI - Productivity Report</title>");
            sb.AppendLine("    <script src=\"https://cdn.jsdelivr.net/npm/chart.js\"></script>");
            sb.AppendLine("    <style>");
            sb.AppendLine(GenerateCss());
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            sb.AppendLine(GenerateHeader(startDate, endDate));
            sb.AppendLine(GenerateSummaryCards(data));
            sb.AppendLine(GenerateChartsSection(data));
            sb.AppendLine(GenerateFiltersSection(startDate, endDate));
            sb.AppendLine(GenerateDetailedTables(data));
            sb.AppendLine(GenerateUserSystemInfoSection(data));
            sb.AppendLine(GenerateWorkDaySection(data));
            sb.AppendLine(GenerateFooter());

            sb.AppendLine("    <script>");
            sb.AppendLine(GenerateJavaScript(data));
            sb.AppendLine("    </script>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private string GenerateCss()
        {
            return @"
                * {
                    margin: 0;
                    padding: 0;
                    box-sizing: border-box;
                }

                body {
                    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                    background-color: #E6EBE0;
                    color: #333;
                    line-height: 1.6;
                }

                .container {
                    max-width: 1200px;
                    margin: 0 auto;
                    padding: 20px;
                }

                .header {
                    background: linear-gradient(135deg, #5CA4A9, #9BC1BC);
                    color: white;
                    padding: 30px;
                    border-radius: 15px;
                    margin-bottom: 30px;
                    text-align: center;
                    box-shadow: 0 4px 15px rgba(0,0,0,0.1);
                }

                .header h1 {
                    font-size: 2.5em;
                    margin-bottom: 10px;
                }

                .header p {
                    font-size: 1.1em;
                    opacity: 0.9;
                }

                .summary-cards {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
                    gap: 20px;
                    margin-bottom: 30px;
                }

                .card {
                    background: white;
                    padding: 25px;
                    border-radius: 12px;
                    box-shadow: 0 4px 15px rgba(0,0,0,0.1);
                    text-align: center;
                    transition: transform 0.3s ease;
                }

                .card:hover {
                    transform: translateY(-5px);
                }

                .card-icon {
                    font-size: 2.5em;
                    margin-bottom: 15px;
                }

                .card-value {
                    font-size: 2em;
                    font-weight: bold;
                    color: #5CA4A9;
                    margin-bottom: 5px;
                }

                .card-label {
                    color: #666;
                    font-size: 0.9em;
                }

                .charts-section {
                    background: white;
                    padding: 30px;
                    border-radius: 15px;
                    margin-bottom: 30px;
                    box-shadow: 0 4px 15px rgba(0,0,0,0.1);
                }

                .chart-container {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
                    gap: 30px;
                    margin-top: 20px;
                }

                .chart-wrapper {
                    background: #F4F1BB;
                    padding: 20px;
                    border-radius: 10px;
                    text-align: center;
                }

                .filters-section {
                    background: white;
                    padding: 25px;
                    border-radius: 15px;
                    margin-bottom: 30px;
                    box-shadow: 0 4px 15px rgba(0,0,0,0.1);
                }

                .filter-controls {
                    display: flex;
                    gap: 15px;
                    flex-wrap: wrap;
                    align-items: center;
                }

                .filter-controls select, .filter-controls input {
                    padding: 10px;
                    border: 2px solid #9BC1BC;
                    border-radius: 8px;
                    font-size: 14px;
                }

                .filter-controls button {
                    background: #5CA4A9;
                    color: white;
                    border: none;
                    padding: 10px 20px;
                    border-radius: 8px;
                    cursor: pointer;
                    transition: background 0.3s ease;
                }

                .filter-controls button:hover {
                    background: #4A8A8F;
                }

                .table-section {
                    background: white;
                    padding: 30px;
                    border-radius: 15px;
                    margin-bottom: 30px;
                    box-shadow: 0 4px 15px rgba(0,0,0,0.1);
                }

                .table-container {
                    overflow-x: auto;
                }

                table {
                    width: 100%;
                    border-collapse: collapse;
                    margin-top: 20px;
                }

                th, td {
                    padding: 12px;
                    text-align: left;
                    border-bottom: 1px solid #E6EBE0;
                }

                th {
                    background: #9BC1BC;
                    color: white;
                    font-weight: 600;
                }

                tr:hover {
                    background: #F4F1BB;
                }

                .footer {
                    text-align: center;
                    padding: 20px;
                    color: #666;
                    font-size: 0.9em;
                }

                .progress-bar {
                    width: 100%;
                    height: 20px;
                    background: #E6EBE0;
                    border-radius: 10px;
                    overflow: hidden;
                    margin-top: 10px;
                }

                .progress-fill {
                    height: 100%;
                    background: linear-gradient(90deg, #5CA4A9, #9BC1BC);
                    transition: width 0.3s ease;
                }

                .info-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
                    gap: 20px;
                    margin-top: 20px;
                }

                .info-card {
                    background: #F8F9FA;
                    padding: 20px;
                    border-radius: 8px;
                    border-left: 4px solid #5CA4A9;
                }

                .info-card h4 {
                    color: #5CA4A9;
                    margin-bottom: 10px;
                }

                .info-item {
                    display: flex;
                    justify-content: space-between;
                    margin-bottom: 8px;
                    padding: 5px 0;
                    border-bottom: 1px solid #E6EBE0;
                }

                .info-label {
                    font-weight: 600;
                    color: #555;
                }

                .info-value {
                    color: #333;
                }

                @media (max-width: 768px) {
                    .container {
                        padding: 10px;
                    }
                    
                    .chart-container {
                        grid-template-columns: 1fr;
                    }
                    
                    .filter-controls {
                        flex-direction: column;
                        align-items: stretch;
                    }
                }
            ";
        }

        private string GenerateHeader(DateTime startDate, DateTime endDate)
        {
            var userName = Environment.UserName;
            return $@"
                <div class='container'>
                    <div class='header'>
                        <h1>📊 TaskManager CLI Report</h1>
                        <p>Comprehensive Productivity Analytics & Insights</p>
                        <p>Generated for {userName} | Period: {startDate:MMMM dd, yyyy} - {endDate:MMMM dd, yyyy}</p>
                        <p>Generated on {DateTime.UtcNow:MMMM dd, yyyy 'at' HH:mm UTC}</p>
                    </div>
                </div>
            ";
        }

        private string GenerateSummaryCards(ReportData data)
        {
            return $@"
                <div class='container'>
                    <div class='summary-cards'>
                        <div class='card'>
                            <div class='card-icon'>📋</div>
                            <div class='card-value'>{data.TotalTasks}</div>
                            <div class='card-label'>Total Tasks</div>
                            <div class='progress-bar'>
                                <div class='progress-fill' style='width: {data.CompletionRate}%'></div>
                            </div>
                            <div style='margin-top: 5px; font-size: 0.8em; color: #666;'>{data.CompletionRate:F1}% Complete</div>
                        </div>
                        
                        <div class='card'>
                            <div class='card-icon'>✅</div>
                            <div class='card-value'>{data.CompletedTasks}</div>
                            <div class='card-label'>Completed Tasks</div>
                        </div>
                        
                        <div class='card'>
                            <div class='card-icon'>🎯</div>
                            <div class='card-value'>{data.FocusSessionsCount}</div>
                            <div class='card-label'>Focus Sessions</div>
                        </div>
                        
                        <div class='card'>
                            <div class='card-icon'>⏱️</div>
                            <div class='card-value'>{data.TotalFocusTime.TotalHours:F1}h</div>
                            <div class='card-label'>Total Focus Time</div>
                        </div>
                        
                        <div class='card'>
                            <div class='card-icon'>📈</div>
                            <div class='card-value'>{data.TodayProductivityScore:P0}</div>
                            <div class='card-label'>Avg Productivity</div>
                        </div>
                        
                        <div class='card'>
                            <div class='card-icon'>⚡</div>
                            <div class='card-value'>{data.CommandsExecuted}</div>
                            <div class='card-label'>Commands Executed</div>
                        </div>
                    </div>
                </div>
            ";
        }

        private string GenerateChartsSection(ReportData data)
        {
            return $@"
                <div class='container'>
                    <div class='charts-section'>
                        <h2>📊 Analytics Dashboard</h2>
                        <div class='chart-container'>
                            <div class='chart-wrapper'>
                                <h3>Task Status Distribution</h3>
                                <canvas id='taskStatusChart' width='400' height='300'></canvas>
                            </div>
                            
                            <div class='chart-wrapper'>
                                <h3>Daily Task Completions</h3>
                                <canvas id='dailyCompletionsChart' width='400' height='300'></canvas>
                            </div>
                            
                            <div class='chart-wrapper'>
                                <h3>Session Type Distribution</h3>
                                <canvas id='sessionTypeChart' width='400' height='300'></canvas>
                            </div>
                            
                            <div class='chart-wrapper'>
                                <h3>Hourly Activity Pattern</h3>
                                <canvas id='hourlyActivityChart' width='400' height='300'></canvas>
                            </div>
                            
                            <div class='chart-wrapper'>
                                <h3>Top Commands Used</h3>
                                <canvas id='topCommandsChart' width='400' height='300'></canvas>
                            </div>
                            
                            <div class='chart-wrapper'>
                                <h3>Focus vs Break Time</h3>
                                <canvas id='focusBreakChart' width='400' height='300'></canvas>
                            </div>
                        </div>
                    </div>
                </div>
            ";
        }

        private string GenerateFiltersSection(DateTime startDate, DateTime endDate)
        {
            return $@"
                <div class='container'>
                    <div class='filters-section'>
                        <h2>🔍 Data Filters</h2>
                        <div class='filter-controls'>
                            <label for='dateFilter'>Date Range:</label>
                            <select id='dateFilter' onchange='applyFilters()'>
                                <option value='all'>All Time</option>
                                <option value='today'>Today</option>
                                <option value='week'>This Week</option>
                                <option value='month'>This Month</option>
                                <option value='custom'>Custom Range</option>
                            </select>
                            
                            <label for='startDate'>Start Date:</label>
                            <input type='date' id='startDate' value='{startDate:yyyy-MM-dd}' onchange='applyFilters()'>
                            
                            <label for='endDate'>End Date:</label>
                            <input type='date' id='endDate' value='{endDate:yyyy-MM-dd}' onchange='applyFilters()'>
                            
                            <select id='statusFilter' onchange='applyFilters()'>
                                <option value='all'>All Statuses</option>
                                <option value='completed'>Completed</option>
                                <option value='pending'>Pending</option>
                                <option value='inprogress'>In Progress</option>
                                <option value='paused'>Paused</option>
                            </select>
                            
                            <input type='text' id='searchFilter' placeholder='Search tasks...' onkeyup='applyFilters()'>
                            
                            <button onclick='resetFilters()'>Reset</button>
                        </div>
                    </div>
                </div>
            ";
        }

        private string GenerateDetailedTables(ReportData data)
        {
            // Safe calculations to avoid division by zero and formatting issues
            var totalTasks = Math.Max(data.TotalTasks, 1); // Prevent division by zero
            var pendingPercentage = data.TotalTasks > 0 ? ((double)data.PendingTasks / totalTasks * 100) : 0;
            var inProgressPercentage = data.TotalTasks > 0 ? ((double)data.InProgressTasks / totalTasks * 100) : 0;
            var pausedPercentage = data.TotalTasks > 0 ? ((double)data.PausedTasks / totalTasks * 100) : 0;

            var avgFocusMinutes = data.FocusSessionsCount > 0 ? data.TotalFocusTime.TotalMinutes / data.FocusSessionsCount : 0;
            var avgBreakMinutes = data.BreakSessionsCount > 0 ? data.TotalBreakTime.TotalMinutes / data.BreakSessionsCount : 0;

            // Safe TimeSpan formatting
            var focusTimeStr = data.TotalFocusTime.ToString(@"hh\:mm\:ss");
            var breakTimeStr = data.TotalBreakTime.ToString(@"hh\:mm\:ss");

            return $@"
                <div class='container'>
                    <div class='table-section'>
                        <h2>📋 Detailed Data Tables</h2>
                        
                        <h3>Task Summary</h3>
                        <div class='table-container'>
                            <table id='taskSummaryTable'>
                                <thead>
                                    <tr>
                                        <th>Metric</th>
                                        <th>Count</th>
                                        <th>Percentage</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        <td>Total Tasks</td>
                                        <td>{data.TotalTasks}</td>
                                        <td>100%</td>
                                    </tr>
                                    <tr>
                                        <td>Completed</td>
                                        <td>{data.CompletedTasks}</td>
                                        <td>{data.CompletionRate:F1}%</td>
                                    </tr>
                                    <tr>
                                        <td>Pending</td>
                                        <td>{data.PendingTasks}</td>
                                        <td>{pendingPercentage:F1}%</td>
                                    </tr>
                                    <tr>
                                        <td>In Progress</td>
                                        <td>{data.InProgressTasks}</td>
                                        <td>{inProgressPercentage:F1}%</td>
                                    </tr>
                                    <tr>
                                        <td>Paused</td>
                                        <td>{data.PausedTasks}</td>
                                        <td>{pausedPercentage:F1}%</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                        
                        <h3>Session Statistics</h3>
                        <div class='table-container'>
                            <table id='sessionStatsTable'>
                                <thead>
                                    <tr>
                                        <th>Session Type</th>
                                        <th>Count</th>
                                        <th>Total Duration</th>
                                        <th>Average Duration</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        <td>Focus Sessions</td>
                                        <td>{data.FocusSessionsCount}</td>
                                        <td>{focusTimeStr}</td>
                                        <td>{avgFocusMinutes:F1} min</td>
                                    </tr>
                                    <tr>
                                        <td>Break Sessions</td>
                                        <td>{data.BreakSessionsCount}</td>
                                        <td>{breakTimeStr}</td>
                                        <td>{avgBreakMinutes:F1} min</td>
                                    </tr>
                                    <tr>
                                        <td>Commands Executed</td>
                                        <td>{data.CommandsExecuted}</td>
                                        <td>-</td>
                                        <td>-</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            ";
        }

        private string GenerateUserSystemInfoSection(ReportData data)
        {
            var allowedCategories = new List<string> { ".NET", "Application", "Computer", "Time", "User" };
            var sb = new StringBuilder();
            sb.AppendLine("<div class='container'>");
            sb.AppendLine("    <div class='table-section'>");
            sb.AppendLine("        <h2>👤 User & System Information</h2>");
            sb.AppendLine("        <div class='info-grid'>");

            if (data.UserInfo.Any())
            {
                var categories = data.UserInfo.Keys
                    .Select(k => k.Split(' ')[0])
                    .Distinct()
                    .OrderBy(c => c);

                foreach (var category in categories.Where(x => allowedCategories.Contains(x)))
                {
                    sb.AppendLine("            <div class='info-card'>");
                    sb.AppendLine($"                <h4>{category}</h4>");

                    var categoryItems = data.UserInfo
                        .Where(kvp => kvp.Key.StartsWith(category))
                        .OrderBy(kvp => kvp.Key);

                    foreach (var item in categoryItems)
                    {
                        var name = item.Key.Replace($"{category} ", "");
                        sb.AppendLine("                <div class='info-item'>");
                        sb.AppendLine($"                    <span class='info-label'>{name}:</span>");
                        sb.AppendLine($"                    <span class='info-value'>{item.Value}</span>");
                        sb.AppendLine("                </div>");
                    }

                    sb.AppendLine("            </div>");
                }
            }
            else
            {
                sb.AppendLine("            <div class='info-card'>");
                sb.AppendLine("                <h4>System Information</h4>");
                sb.AppendLine("                <div class='info-item'>");
                sb.AppendLine("                    <span class='info-label'>User:</span>");
                sb.AppendLine($"                    <span class='info-value'>{Environment.UserName}</span>");
                sb.AppendLine("                </div>");
                sb.AppendLine("                <div class='info-item'>");
                sb.AppendLine("                    <span class='info-label'>Machine:</span>");
                sb.AppendLine($"                    <span class='info-value'>{Environment.MachineName}</span>");
                sb.AppendLine("                </div>");
                sb.AppendLine("                <div class='info-item'>");
                sb.AppendLine("                    <span class='info-label'>OS:</span>");
                sb.AppendLine($"                    <span class='info-value'>{Environment.OSVersion}</span>");
                sb.AppendLine("                </div>");
                sb.AppendLine("            </div>");
            }

            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }

        private string GenerateWorkDaySection(ReportData data)
        {
            // Safe calculations to avoid division by zero and formatting issues
            var avgWorkDayHours = data.CompletedWorkDays > 0 ? data.TotalWorkTime.TotalHours / data.CompletedWorkDays : 0;
            var workTimeStr = data.TotalWorkTime.ToString(@"hh\:mm");

            return $@"
                <div class='container'>
                    <div class='table-section'>
                        <h2>📅 Work Day Statistics</h2>
                        <div class='table-container'>
                            <table>
                                <thead>
                                    <tr>
                                        <th>Metric</th>
                                        <th>Value</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        <td>Total Work Days</td>
                                        <td>{data.TotalWorkDays}</td>
                                    </tr>
                                    <tr>
                                        <td>Completed Work Days</td>
                                        <td>{data.CompletedWorkDays}</td>
                                    </tr>
                                    <tr>
                                        <td>Total Work Time</td>
                                        <td>{workTimeStr}</td>
                                    </tr>
                                    <tr>
                                        <td>Average Work Day Duration</td>
                                        <td>{avgWorkDayHours:F1} hours</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            ";
        }

        private string GenerateFooter()
        {
            return @"
                <div class='footer'>
                    <p>Generated by TaskManager CLI - Your Personal Productivity Assistant</p>
                    <p>Data is stored locally in your Documents/TaskManager folder</p>
                </div>
            ";
        }

        private string GenerateJavaScript(ReportData data)
        {
            // Generate chart data with proper handling of empty data
            var dailyCompletionsLabels = data.DailyTaskCompletions.Any()
                ? string.Join(",", data.DailyTaskCompletions.Keys.Select(k => $"'{k}'"))
                : "'No Data'";
            var dailyCompletionsValues = data.DailyTaskCompletions.Any()
                ? string.Join(",", data.DailyTaskCompletions.Values)
                : "0";

            var sessionTypeLabels = data.SessionTypeDistribution.Any()
                ? string.Join(",", data.SessionTypeDistribution.Keys.Select(k => $"'{k}'"))
                : "'No Data'";
            var sessionTypeValues = data.SessionTypeDistribution.Any()
                ? string.Join(",", data.SessionTypeDistribution.Values)
                : "0";

            var hourlyLabels = data.HourlyActivity.Any()
                ? string.Join(",", data.HourlyActivity.Keys.Select(k => $"'{k}:00'"))
                : "'00:00'";
            var hourlyValues = data.HourlyActivity.Any()
                ? string.Join(",", data.HourlyActivity.Values)
                : "0";

            var topCommandsLabels = data.TopCommands.Any()
                ? string.Join(",", data.TopCommands.Keys.Select(k => $"'{k}'"))
                : "'No Commands'";
            var topCommandsValues = data.TopCommands.Any()
                ? string.Join(",", data.TopCommands.Values)
                : "0";

            return $@"
                // Task Status Chart
                new Chart(document.getElementById('taskStatusChart'), {{
                    type: 'doughnut',
                    data: {{
                        labels: ['Completed', 'Pending', 'In Progress', 'Paused'],
                        datasets: [{{
                            data: [{data.CompletedTasks}, {data.PendingTasks}, {data.InProgressTasks}, {data.PausedTasks}],
                            backgroundColor: ['#5CA4A9', '#9BC1BC', '#F4F1BB', '#E6EBE0'],
                            borderWidth: 2,
                            borderColor: '#fff'
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        plugins: {{
                            legend: {{
                                position: 'bottom'
                            }}
                        }}
                    }}
                }});

                // Daily Completions Chart
                new Chart(document.getElementById('dailyCompletionsChart'), {{
                    type: 'line',
                    data: {{
                        labels: [{dailyCompletionsLabels}],
                        datasets: [{{
                            label: 'Tasks Completed',
                            data: [{dailyCompletionsValues}],
                            borderColor: '#5CA4A9',
                            backgroundColor: 'rgba(92, 164, 169, 0.1)',
                            tension: 0.4,
                            fill: true
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        scales: {{
                            y: {{
                                beginAtZero: true,
                                ticks: {{
                                    stepSize: 1
                                }}
                            }}
                        }}
                    }}
                }});

                // Session Type Chart
                new Chart(document.getElementById('sessionTypeChart'), {{
                    type: 'pie',
                    data: {{
                        labels: [{sessionTypeLabels}],
                        datasets: [{{
                            data: [{sessionTypeValues}],
                            backgroundColor: ['#5CA4A9', '#9BC1BC', '#F4F1BB', '#E6EBE0', '#A8D5BA'],
                            borderWidth: 2,
                            borderColor: '#fff'
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        plugins: {{
                            legend: {{
                                position: 'bottom'
                            }}
                        }}
                    }}
                }});

                // Hourly Activity Chart
                new Chart(document.getElementById('hourlyActivityChart'), {{
                    type: 'bar',
                    data: {{
                        labels: [{hourlyLabels}],
                        datasets: [{{
                            label: 'Activities',
                            data: [{hourlyValues}],
                            backgroundColor: '#9BC1BC',
                            borderColor: '#5CA4A9',
                            borderWidth: 1
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        scales: {{
                            y: {{
                                beginAtZero: true,
                                ticks: {{
                                    stepSize: 1
                                }}
                            }}
                        }}
                    }}
                }});

                // Top Commands Chart - Fixed to use horizontalBar
                new Chart(document.getElementById('topCommandsChart'), {{
                    type: 'bar',
                    data: {{
                        labels: [{topCommandsLabels}],
                        datasets: [{{
                            label: 'Usage Count',
                            data: [{topCommandsValues}],
                            backgroundColor: '#F4F1BB',
                            borderColor: '#5CA4A9',
                            borderWidth: 1
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        indexAxis: 'y',
                        scales: {{
                            x: {{
                                beginAtZero: true
                            }}
                        }}
                    }}
                }});

                // Focus vs Break Chart - Fixed with proper data
                new Chart(document.getElementById('focusBreakChart'), {{
                    type: 'bar',
                    data: {{
                        labels: ['Focus Time', 'Break Time'],
                        datasets: [{{
                            label: 'Hours',
                            data: [{data.TotalFocusTime.TotalHours:F2}, {data.TotalBreakTime.TotalHours:F2}],
                            backgroundColor: ['#5CA4A9', '#9BC1BC'],
                            borderColor: ['#4A8A8F', '#7BA8A8'],
                            borderWidth: 2
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        scales: {{
                            y: {{
                                beginAtZero: true
                            }}
                        }}
                    }}
                }});

                // Enhanced filter functions with automatic application
                function applyFilters() {{
                    const dateFilter = document.getElementById('dateFilter').value;
                    const statusFilter = document.getElementById('statusFilter').value;
                    const searchFilter = document.getElementById('searchFilter').value;
                    const startDate = document.getElementById('startDate').value;
                    const endDate = document.getElementById('endDate').value;
                    
                    // Apply filters to tables
                    filterTable('taskSummaryTable', statusFilter, searchFilter);
                    filterTable('sessionStatsTable', statusFilter, searchFilter);
                    
                    // Handle date filter changes
                    if (dateFilter === 'custom') {{
                        document.getElementById('startDate').style.display = 'inline-block';
                        document.getElementById('endDate').style.display = 'inline-block';
                    }} else {{
                        document.getElementById('startDate').style.display = 'none';
                        document.getElementById('endDate').style.display = 'none';
                    }}
                    
                    console.log('Filters applied:', {{ dateFilter, statusFilter, searchFilter, startDate, endDate }});
                }}

                function resetFilters() {{
                    document.getElementById('dateFilter').value = 'all';
                    document.getElementById('statusFilter').value = 'all';
                    document.getElementById('searchFilter').value = '';
                    document.getElementById('startDate').style.display = 'none';
                    document.getElementById('endDate').style.display = 'none';
                    
                    // Reset table visibility
                    const tables = document.querySelectorAll('table');
                    tables.forEach(table => {{
                        const rows = table.querySelectorAll('tbody tr');
                        rows.forEach(row => row.style.display = '');
                    }});
                }}

                function filterTable(tableId, statusFilter, searchFilter) {{
                    const table = document.getElementById(tableId);
                    if (!table) return;
                    
                    const rows = table.querySelectorAll('tbody tr');
                    
                    rows.forEach(row => {{
                        const cells = row.querySelectorAll('td');
                        let showRow = true;
                        
                        if (searchFilter) {{
                            const text = row.textContent.toLowerCase();
                            showRow = text.includes(searchFilter.toLowerCase());
                        }}
                        
                        if (statusFilter !== 'all' && cells.length > 1) {{
                            const status = cells[0].textContent.toLowerCase();
                            showRow = showRow && status.includes(statusFilter);
                        }}
                        
                        row.style.display = showRow ? '' : 'none';
                    }});
                }}

                // Initialize filters on page load
                document.addEventListener('DOMContentLoaded', function() {{
                    // Hide date inputs initially
                    document.getElementById('startDate').style.display = 'none';
                    document.getElementById('endDate').style.display = 'none';
                    
                    // Set up date filter change handler
                    document.getElementById('dateFilter').addEventListener('change', function() {{
                        const isCustom = this.value === 'custom';
                        document.getElementById('startDate').style.display = isCustom ? 'inline-block' : 'none';
                        document.getElementById('endDate').style.display = isCustom ? 'inline-block' : 'none';
                    }});
                }});
            ";
        }
    }

    public class ReportData
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int PausedTasks { get; set; }
        public double CompletionRate { get; set; }

        public TimeSpan TotalFocusTime { get; set; }
        public TimeSpan TotalBreakTime { get; set; }
        public int FocusSessionsCount { get; set; }
        public int BreakSessionsCount { get; set; }
        public int CommandsExecuted { get; set; }

        public double TodayProductivityScore { get; set; }
        public int TodayTasksCompleted { get; set; }

        public Dictionary<string, int> DailyTaskCompletions { get; set; } = new();
        public Dictionary<string, int> SessionTypeDistribution { get; set; } = new();
        public Dictionary<string, int> HourlyActivity { get; set; } = new();
        public Dictionary<string, int> TopCommands { get; set; } = new();

        // New properties for work day statistics
        public int TotalWorkDays { get; set; }
        public int CompletedWorkDays { get; set; }
        public TimeSpan TotalWorkTime { get; set; }

        // User and system information
        public Dictionary<string, string> UserInfo { get; set; } = new();
    }

    public class ArchivedData
    {
        public List<TaskModel> Tasks { get; set; } = new();
        public List<SessionLog> SessionLogs { get; set; } = new();
        public List<WorkDay> WorkDays { get; set; } = new();
    }
}