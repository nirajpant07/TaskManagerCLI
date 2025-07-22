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

                // Ensure dates are normalized and consistent
                start = start.Date;
                end = end.Date;

                Console.WriteLine($"Generating report for date range: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}");

                var reportData = await AnalyzeDataAsync(start, end);
                var htmlContent = GenerateHtmlContent(reportData, start, end);

                var fileName = $"{userName}_{start:yyyyMMdd}_{end:yyyyMMdd}.html";
                var filePath = Path.Combine(_reportsPath, fileName);

                await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);
                
                Console.WriteLine($"Report generated successfully: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating report: {ex.Message}");
                throw new Exception($"Failed to generate HTML report: {ex.Message}", ex);
            }
        }

        private async Task<ReportData> AnalyzeDataAsync(DateTime startDate, DateTime endDate)
        {
            var reportData = new ReportData();

            // Validate date range
            if (startDate > endDate)
            {
                Console.WriteLine($"Warning: Invalid date range - start date ({startDate:yyyy-MM-dd}) is after end date ({endDate:yyyy-MM-dd})");
                var temp = startDate;
                startDate = endDate;
                endDate = temp;
            }

            // Ensure dates are normalized to start of day
            startDate = startDate.Date;
            endDate = endDate.Date;

            try
            {
                // Load data for the specified date range with proper error handling
                var allTasks = new List<TaskModel>();
                var allSessionLogs = new List<SessionLog>();
                var allWorkDays = new List<WorkDay>();

                // Load current data and filter by date range
                try
                {
                    var currentTasks = await _repository.GetAllTasksAsync();
                    if (currentTasks != null)
                    {
                        var filteredCurrentTasks = currentTasks
                            .Where(t => t.CreatedAt.Date >= startDate.Date && t.CreatedAt.Date <= endDate.Date)
                            .Where(t => t.Status != TaskStatus.Deleted)
                            .ToList();
                        allTasks.AddRange(filteredCurrentTasks);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading current tasks: {ex.Message}");
                }

                // Load current session logs (only for today if it's in the date range)
                try
                {
                    if (DateTime.UtcNow.Date >= startDate.Date && DateTime.UtcNow.Date <= endDate.Date)
                    {
                        var todaySessions = await _repository.GetTodaySessionLogsAsync();
                        if (todaySessions != null)
                        {
                            allSessionLogs.AddRange(todaySessions);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading current session logs: {ex.Message}");
                }

                // Load current work day (only for today if it's in the date range)
                try
                {
                    if (DateTime.UtcNow.Date >= startDate.Date && DateTime.UtcNow.Date <= endDate.Date)
                    {
                        var currentWorkDay = await _repository.GetTodayWorkDayAsync();
                        if (currentWorkDay != null)
                        {
                            allWorkDays.Add(currentWorkDay);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading current work day: {ex.Message}");
                }

                // Load archived data for the specified date range
                var archivedData = await LoadArchivedDataAsync(startDate, endDate);
                allTasks.AddRange(archivedData.Tasks);
                allSessionLogs.AddRange(archivedData.SessionLogs);
                allWorkDays.AddRange(archivedData.WorkDays);

                // Deduplicate by GUID (keep latest by CreatedAt/Date/StartTime)
                var taskDict = new Dictionary<Guid, TaskModel>();
                foreach (var t in allTasks)
                {
                    if (t.Id == Guid.Empty) continue;
                    if (!taskDict.ContainsKey(t.Id) || t.CreatedAt > taskDict[t.Id].CreatedAt)
                        taskDict[t.Id] = t;
                }
                var dedupedTasks = taskDict.Values.ToList();

                var sessionLogDict = new Dictionary<Guid, SessionLog>();
                foreach (var s in allSessionLogs)
                {
                    if (s.Id == Guid.Empty) continue;
                    if (!sessionLogDict.ContainsKey(s.Id) || s.StartTime > sessionLogDict[s.Id].StartTime)
                        sessionLogDict[s.Id] = s;
                }
                var dedupedSessionLogs = sessionLogDict.Values.ToList();

                var workDayDict = new Dictionary<Guid, WorkDay>();
                foreach (var w in allWorkDays)
                {
                    if (w.Id == Guid.Empty) continue;
                    if (!workDayDict.ContainsKey(w.Id) || w.Date > workDayDict[w.Id].Date)
                        workDayDict[w.Id] = w;
                }
                var dedupedWorkDays = workDayDict.Values.ToList();

                // Final filtering to ensure data consistency
                var filteredTasks = dedupedTasks
                    .Where(t => t.CreatedAt.Date >= startDate.Date && t.CreatedAt.Date <= endDate.Date)
                    .Where(t => t.Status != TaskStatus.Deleted)
                    .ToList();

                var filteredSessionLogs = dedupedSessionLogs
                    .Where(s => s.Date.Date >= startDate.Date && s.Date.Date <= endDate.Date)
                    .ToList();

                var filteredWorkDays = dedupedWorkDays
                    .Where(w => w.Date.Date >= startDate.Date && w.Date.Date <= endDate.Date)
                    .ToList();

                // Log data summary for debugging
                Console.WriteLine($"Data loaded for {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}:");
                Console.WriteLine($"  - Tasks: {filteredTasks.Count} (from {allTasks.Count} total)");
                Console.WriteLine($"  - Session Logs: {filteredSessionLogs.Count} (from {allSessionLogs.Count} total)");
                Console.WriteLine($"  - Work Days: {filteredWorkDays.Count} (from {allWorkDays.Count} total)");

                // Enhanced Task Analysis with better categorization
                reportData.TotalTasks = filteredTasks.Count;
                reportData.CompletedTasks = filteredTasks.Count(t => t.Status == TaskStatus.Completed);
                reportData.PendingTasks = filteredTasks.Count(t => t.Status == TaskStatus.Pending);
                reportData.InProgressTasks = filteredTasks.Count(t => t.Status == TaskStatus.InProgress);
                reportData.PausedTasks = filteredTasks.Count(t => t.Status == TaskStatus.Paused);
                reportData.OnBreakTasks = filteredTasks.Count(t => t.Status == TaskStatus.OnBreak);
                
                // Safe completion rate calculation
                reportData.CompletionRate = reportData.TotalTasks > 0 
                    ? Math.Round((double)reportData.CompletedTasks / reportData.TotalTasks * 100, 1) 
                    : 0;

                // Enhanced Session Analysis with duration validation
                var focusSessions = filteredSessionLogs
                    .Where(s => s.Type == SessionType.Focus && s.StartTime != default)
                    .ToList();
                var breakSessions = filteredSessionLogs
                    .Where(s => s.Type == SessionType.Break && s.StartTime != default)
                    .ToList();
                var commandSessions = filteredSessionLogs
                    .Where(s => s.Type == SessionType.Command)
                    .ToList();

                // Calculate session durations with validation
                reportData.TotalFocusTime = CalculateTotalSessionTime(focusSessions);
                reportData.TotalBreakTime = CalculateTotalSessionTime(breakSessions);
                reportData.FocusSessionsCount = focusSessions.Count;
                reportData.BreakSessionsCount = breakSessions.Count;
                reportData.CommandsExecuted = commandSessions.Count;

                // Enhanced Daily Statistics with better error handling
                var totalProductivityScore = 0.0;
                var totalTasksCompleted = 0;
                var daysWithData = 0;

                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    try
                    {
                        var dayStats = await _repository.GetDayStatisticsAsync(date);
                        if (dayStats != null)
                        {
                            totalProductivityScore += dayStats.ProductivityScore;
                            totalTasksCompleted += dayStats.TasksCompleted;
                            daysWithData++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing
                        Console.WriteLine($"Error loading statistics for {date:yyyy-MM-dd}: {ex.Message}");
                    }
                }

                reportData.AverageProductivityScore = daysWithData > 0 
                    ? Math.Round(totalProductivityScore / daysWithData, 1) 
                    : 0;
                reportData.TotalTasksCompleted = totalTasksCompleted;

                // Enhanced Task Trends with proper date formatting
                reportData.DailyTaskCompletions = new Dictionary<string, int>();
                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    try
                    {
                        var dayStats = await _repository.GetDayStatisticsAsync(date);
                        reportData.DailyTaskCompletions[date.ToString("MM/dd")] = dayStats?.TasksCompleted ?? 0;
                    }
                    catch
                    {
                        reportData.DailyTaskCompletions[date.ToString("MM/dd")] = 0;
                    }
                }

                // Enhanced Session Type Distribution
                reportData.SessionTypeDistribution = filteredSessionLogs
                    .Where(s => s.Type != SessionType.Command) // Exclude commands from session type chart
                    .GroupBy(s => s.Type)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count());

                // Enhanced Hourly Activity with 24-hour coverage
                reportData.HourlyActivity = new Dictionary<string, int>();
                for (int hour = 0; hour < 24; hour++)
                {
                    var hourKey = hour.ToString("00");
                    var count = filteredSessionLogs.Count(s => s.StartTime.Hour == hour);
                    reportData.HourlyActivity[hourKey] = count;
                }

                // Enhanced Top Commands with better extraction
                reportData.TopCommands = commandSessions
                    .Where(s => !string.IsNullOrEmpty(s.Notes))
                    .GroupBy(s => ExtractCommandName(s.Notes))
                    .Where(g => !string.IsNullOrEmpty(g.Key) && g.Key != "Unknown" && g.Key != "Command")
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Enhanced Work Day Statistics
                reportData.TotalWorkDays = filteredWorkDays.Count;
                reportData.CompletedWorkDays = filteredWorkDays.Count(w => w.EndTime.HasValue);
                reportData.TotalWorkTime = CalculateTotalWorkTime(filteredWorkDays);
                reportData.AverageWorkDayDuration = reportData.CompletedWorkDays > 0 
                    ? reportData.TotalWorkTime.TotalHours / reportData.CompletedWorkDays 
                    : 0;

                // Load user and system information
                reportData.UserInfo = await LoadUserAndSystemInfoAsync();

                // Calculate additional metrics
                reportData.FocusEfficiency = CalculateFocusEfficiency(reportData.TotalFocusTime, reportData.TotalWorkTime);
                reportData.TasksPerDay = CalculateTasksPerDay(reportData.TotalTasks, startDate, endDate);
                reportData.SessionsPerDay = CalculateSessionsPerDay(reportData.FocusSessionsCount + reportData.BreakSessionsCount, startDate, endDate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in data analysis: {ex.Message}");
                // Return basic report data even if analysis fails
            }

            return reportData;
        }

        private async Task<ArchivedData> LoadArchivedDataAsync(DateTime startDate, DateTime endDate)
        {
            var archivedData = new ArchivedData();

            Console.WriteLine($"Loading archived data for range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
            Console.WriteLine($"Archive path: {_archivePath}");

            if (!Directory.Exists(_archivePath))
            {
                Console.WriteLine("Archive directory does not exist");
                return archivedData;
            }

            var archiveDirectories = Directory.GetDirectories(_archivePath);
            Console.WriteLine($"Found {archiveDirectories.Length} archive directories");

            foreach (var dir in archiveDirectories)
            {
                var dirName = Path.GetFileName(dir);
                Console.WriteLine($"Processing archive directory: {dirName}");
                Console.WriteLine($"  - Full path: {dir}");
                Console.WriteLine($"  - Directory exists: {Directory.Exists(dir)}");

                if (!DateTime.TryParse(dirName, out var archiveDate))
                {
                    Console.WriteLine($"  - Could not parse date from directory name: {dirName}");
                    continue;
                }

                Console.WriteLine($"  - Parsed archive date: {archiveDate:yyyy-MM-dd}");

                if (archiveDate < startDate.Date || archiveDate > endDate.Date)
                {
                    Console.WriteLine($"  - Archive date {archiveDate:yyyy-MM-dd} is outside range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                    continue;
                }

                Console.WriteLine($"  - Archive date {archiveDate:yyyy-MM-dd} is within range");

                var backupFiles = Directory.GetFiles(dir, "tasks_backup_*.xlsx");
                Console.WriteLine($"  - Found {backupFiles.Length} backup files");

                if (backupFiles.Length == 0)
                {
                    Console.WriteLine($"  - No backup files found in {dir}");
                    continue;
                }

                // Use the latest backup from that day
                var latestBackup = backupFiles.OrderByDescending(f => f).First();
                Console.WriteLine($"  - Using backup file: {Path.GetFileName(latestBackup)}");

                try
                {
                    using var package = new ExcelPackage(new FileInfo(latestBackup));
                    
                    var tasksBefore = archivedData.Tasks.Count;
                    var sessionsBefore = archivedData.SessionLogs.Count;
                    var workDaysBefore = archivedData.WorkDays.Count;

                    await LoadArchivedTasksAsync(package, archivedData, archiveDate);
                    await LoadArchivedSessionLogsAsync(package, archivedData, archiveDate);
                    await LoadArchivedWorkDaysAsync(package, archivedData, archiveDate);

                    var tasksAdded = archivedData.Tasks.Count - tasksBefore;
                    var sessionsAdded = archivedData.SessionLogs.Count - sessionsBefore;
                    var workDaysAdded = archivedData.WorkDays.Count - workDaysBefore;

                    Console.WriteLine($"  - Loaded from {archiveDate:yyyy-MM-dd}: {tasksAdded} tasks, {sessionsAdded} sessions, {workDaysAdded} work days");
                }
                catch (Exception ex)
                {
                    // Log error but continue with other archives
                    Console.WriteLine($"  - Error loading archived data from {latestBackup}: {ex.Message}");
                    // Don't rethrow - continue processing other archives
                }
            }

            Console.WriteLine($"Total archived data loaded: {archivedData.Tasks.Count} tasks, {archivedData.SessionLogs.Count} sessions, {archivedData.WorkDays.Count} work days");
            return archivedData;
        }

        private async Task LoadArchivedTasksAsync(ExcelPackage package, ArchivedData archivedData, DateTime archiveDate)
        {
            var taskSheet = package.Workbook.Worksheets["Tasks"];
            if (taskSheet?.Dimension == null)
            {
                Console.WriteLine($"    - Tasks sheet not found or empty for {archiveDate:yyyy-MM-dd}");
                return;
            }

            Console.WriteLine($"    - Loading tasks from sheet with {taskSheet.Dimension.End.Row} rows");
            
            var tasksLoaded = 0;

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
                    if (string.IsNullOrEmpty(idValue) || !Guid.TryParse(idValue, out Guid taskId))
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
                    tasksLoaded++;
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other rows
                    Console.WriteLine($"Error processing task row {row}: {ex.Message}");
                    continue;
                }
            }

            Console.WriteLine($"    - Loaded {tasksLoaded} tasks from {archiveDate:yyyy-MM-dd}");
        }

        private async Task LoadArchivedSessionLogsAsync(ExcelPackage package, ArchivedData archivedData, DateTime archiveDate)
        {
            var sessionLogSheet = package.Workbook.Worksheets["SessionLogs"];
            if (sessionLogSheet?.Dimension == null)
            {
                Console.WriteLine($"    - SessionLogs sheet not found or empty for {archiveDate:yyyy-MM-dd}");
                return;
            }

            Console.WriteLine($"    - Loading session logs from sheet with {sessionLogSheet.Dimension.End.Row} rows");
            
            var sessionsLoaded = 0;

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
                        TaskId = Guid.TryParse(taskIdValue, out var taskId) ? taskId : null,
                        Notes = notes
                    };
                    archivedData.SessionLogs.Add(sessionLog);
                    sessionsLoaded++;
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other rows
                    Console.WriteLine($"Error processing session log row {row}: {ex.Message}");
                    continue;
                }
            }

            Console.WriteLine($"    - Loaded {sessionsLoaded} session logs from {archiveDate:yyyy-MM-dd}");
        }

        private async Task LoadArchivedWorkDaysAsync(ExcelPackage package, ArchivedData archivedData, DateTime archiveDate)
        {
            var workDaySheet = package.Workbook.Worksheets["WorkDays"];
            if (workDaySheet?.Dimension == null)
            {
                Console.WriteLine($"    - WorkDays sheet not found or empty for {archiveDate:yyyy-MM-dd}");
                return;
            }

            Console.WriteLine($"    - Loading work days from sheet with {workDaySheet.Dimension.End.Row} rows");
            
            var workDaysLoaded = 0;

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
                    workDaysLoaded++;
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other rows
                    Console.WriteLine($"Error processing work day row {row}: {ex.Message}");
                    continue;
                }
            }

            Console.WriteLine($"    - Loaded {workDaysLoaded} work days from {archiveDate:yyyy-MM-dd}");
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

        private TimeSpan CalculateTotalSessionTime(List<SessionLog> sessions)
        {
            return sessions.Aggregate(TimeSpan.Zero, (total, s) =>
            {
                if (s.EndTime.HasValue && s.StartTime != default)
                {
                    var duration = s.EndTime.Value - s.StartTime;
                    return total + (duration > TimeSpan.Zero ? duration : TimeSpan.Zero);
                }
                return total;
            });
        }

        private TimeSpan CalculateTotalWorkTime(List<WorkDay> workDays)
        {
            return workDays
                .Where(w => w.EndTime.HasValue && w.StartTime != default)
                .Aggregate(TimeSpan.Zero, (total, w) =>
                {
                    var duration = w.EndTime!.Value - w.StartTime;
                    return total + (duration > TimeSpan.Zero ? duration : TimeSpan.Zero);
                });
        }

        private double CalculateFocusEfficiency(TimeSpan focusTime, TimeSpan workTime)
        {
            if (workTime.TotalMinutes <= 0) return 0;
            return Math.Round((focusTime.TotalMinutes / workTime.TotalMinutes) * 100, 1);
        }

        private double CalculateTasksPerDay(int totalTasks, DateTime startDate, DateTime endDate)
        {
            var days = (endDate.Date - startDate.Date).Days + 1;
            return days > 0 ? Math.Round((double)totalTasks / days, 1) : 0;
        }

        private double CalculateSessionsPerDay(int totalSessions, DateTime startDate, DateTime endDate)
        {
            var days = (endDate.Date - startDate.Date).Days + 1;
            return days > 0 ? Math.Round((double)totalSessions / days, 1) : 0;
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
                    font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, 'Roboto', 'Helvetica Neue', Arial, sans-serif;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: #2d3748;
                    line-height: 1.6;
                    min-height: 100vh;
                }

                .container {
                    max-width: 1400px;
                    margin: 0 auto;
                    padding: 20px;
                }

                .header {
                    background: linear-gradient(135deg, #2c3e50, #34495e);
                    color: white;
                    padding: 40px 30px;
                    border-radius: 20px;
                    margin-bottom: 30px;
                    text-align: center;
                    box-shadow: 0 10px 30px rgba(0,0,0,0.2);
                    position: relative;
                    overflow: hidden;
                }

                .header::before {
                    content: '';
                    position: absolute;
                    top: 0;
                    left: 0;
                    right: 0;
                    bottom: 0;
                    background: linear-gradient(45deg, rgba(255,255,255,0.1) 0%, transparent 100%);
                    pointer-events: none;
                }

                .header h1 {
                    font-size: 3em;
                    margin-bottom: 15px;
                    font-weight: 300;
                    position: relative;
                    z-index: 1;
                }

                .header p {
                    font-size: 1.2em;
                    opacity: 0.9;
                    position: relative;
                    z-index: 1;
                }

                .summary-cards {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
                    gap: 25px;
                    margin-bottom: 40px;
                }

                .card {
                    background: white;
                    padding: 30px 25px;
                    border-radius: 16px;
                    box-shadow: 0 8px 25px rgba(0,0,0,0.1);
                    text-align: center;
                    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
                    position: relative;
                    overflow: hidden;
                }

                .card::before {
                    content: '';
                    position: absolute;
                    top: 0;
                    left: 0;
                    right: 0;
                    height: 4px;
                    background: linear-gradient(90deg, #667eea, #764ba2);
                }

                .card:hover {
                    transform: translateY(-8px);
                    box-shadow: 0 15px 35px rgba(0,0,0,0.15);
                }

                .primary-card::before { background: linear-gradient(90deg, #3498db, #2980b9); }
                .success-card::before { background: linear-gradient(90deg, #27ae60, #2ecc71); }
                .focus-card::before { background: linear-gradient(90deg, #e74c3c, #c0392b); }
                .time-card::before { background: linear-gradient(90deg, #f39c12, #e67e22); }
                .productivity-card::before { background: linear-gradient(90deg, #9b59b6, #8e44ad); }
                .workday-card::before { background: linear-gradient(90deg, #1abc9c, #16a085); }

                .card-icon {
                    font-size: 3em;
                    margin-bottom: 20px;
                    display: block;
                }

                .card-value {
                    font-size: 2.5em;
                    font-weight: 700;
                    color: #2c3e50;
                    margin-bottom: 8px;
                    line-height: 1;
                }

                .card-label {
                    color: #7f8c8d;
                    font-size: 1em;
                    font-weight: 500;
                    margin-bottom: 10px;
                }

                .card-subtitle {
                    color: #95a5a6;
                    font-size: 0.85em;
                    font-weight: 400;
                }

                .charts-section {
                    background: white;
                    padding: 40px;
                    border-radius: 20px;
                    margin-bottom: 40px;
                    box-shadow: 0 8px 25px rgba(0,0,0,0.1);
                }

                .charts-section h2 {
                    color: #2c3e50;
                    font-size: 2em;
                    margin-bottom: 30px;
                    text-align: center;
                    font-weight: 300;
                }

                .chart-container {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(450px, 1fr));
                    gap: 35px;
                    margin-top: 30px;
                }

                .chart-wrapper {
                    background: linear-gradient(135deg, #f8f9fa, #e9ecef);
                    padding: 25px;
                    border-radius: 16px;
                    text-align: center;
                    border: 1px solid #dee2e6;
                    transition: transform 0.3s ease;
                }

                .chart-wrapper:hover {
                    transform: translateY(-2px);
                }

                .chart-wrapper h3 {
                    color: #495057;
                    font-size: 1.3em;
                    margin-bottom: 20px;
                    font-weight: 500;
                }

                .filters-section {
                    background: white;
                    padding: 35px;
                    border-radius: 20px;
                    margin-bottom: 40px;
                    box-shadow: 0 8px 25px rgba(0,0,0,0.1);
                }

                .filters-section h2 {
                    color: #2c3e50;
                    font-size: 2em;
                    margin-bottom: 25px;
                    font-weight: 300;
                }

                .filter-controls {
                    display: flex;
                    gap: 20px;
                    flex-wrap: wrap;
                    align-items: center;
                }

                .filter-controls label {
                    font-weight: 500;
                    color: #495057;
                    min-width: 80px;
                }

                .filter-controls select, .filter-controls input {
                    padding: 12px 16px;
                    border: 2px solid #e9ecef;
                    border-radius: 10px;
                    font-size: 14px;
                    transition: border-color 0.3s ease;
                    background: white;
                }

                .filter-controls select:focus, .filter-controls input:focus {
                    outline: none;
                    border-color: #667eea;
                    box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
                }

                .filter-controls button {
                    background: linear-gradient(135deg, #667eea, #764ba2);
                    color: white;
                    border: none;
                    padding: 12px 24px;
                    border-radius: 10px;
                    cursor: pointer;
                    transition: all 0.3s ease;
                    font-weight: 500;
                }

                .filter-controls button:hover {
                    transform: translateY(-2px);
                    box-shadow: 0 5px 15px rgba(102, 126, 234, 0.3);
                }

                .table-section {
                    background: white;
                    padding: 40px;
                    border-radius: 20px;
                    margin-bottom: 40px;
                    box-shadow: 0 8px 25px rgba(0,0,0,0.1);
                }

                .table-section h2 {
                    color: #2c3e50;
                    font-size: 2em;
                    margin-bottom: 30px;
                    font-weight: 300;
                }

                .table-section h3 {
                    color: #495057;
                    font-size: 1.5em;
                    margin: 30px 0 20px 0;
                    font-weight: 500;
                }

                .table-container {
                    overflow-x: auto;
                    border-radius: 12px;
                    box-shadow: 0 4px 15px rgba(0,0,0,0.1);
                }

                table {
                    width: 100%;
                    border-collapse: collapse;
                    margin-top: 0;
                }

                th, td {
                    padding: 16px 20px;
                    text-align: left;
                    border-bottom: 1px solid #e9ecef;
                }

                th {
                    background: linear-gradient(135deg, #667eea, #764ba2);
                    color: white;
                    font-weight: 600;
                    font-size: 0.95em;
                }

                tr:hover {
                    background: #f8f9fa;
                }

                .footer {
                    text-align: center;
                    padding: 30px;
                    color: #7f8c8d;
                    font-size: 0.95em;
                    background: rgba(255,255,255,0.9);
                    border-radius: 20px;
                    margin-top: 40px;
                }

                .progress-bar {
                    width: 100%;
                    height: 8px;
                    background: #e9ecef;
                    border-radius: 4px;
                    overflow: hidden;
                    margin: 15px 0 10px 0;
                }

                .progress-fill {
                    height: 100%;
                    background: linear-gradient(90deg, #667eea, #764ba2);
                    transition: width 0.6s ease;
                    border-radius: 4px;
                }

                .info-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
                    gap: 25px;
                    margin-top: 25px;
                }

                .info-card {
                    background: linear-gradient(135deg, #f8f9fa, #e9ecef);
                    padding: 25px;
                    border-radius: 16px;
                    border-left: 5px solid #667eea;
                    transition: transform 0.3s ease;
                }

                .info-card:hover {
                    transform: translateY(-2px);
                }

                .info-card h4 {
                    color: #2c3e50;
                    margin-bottom: 15px;
                    font-size: 1.3em;
                    font-weight: 600;
                }

                .info-item {
                    display: flex;
                    justify-content: space-between;
                    margin-bottom: 12px;
                    padding: 8px 0;
                    border-bottom: 1px solid #dee2e6;
                }

                .info-label {
                    font-weight: 600;
                    color: #495057;
                }

                .info-value {
                    color: #2c3e50;
                    font-weight: 500;
                }

                /* Info icon styling */
                .info-icon {
                    display: inline-block;
                    cursor: pointer;
                    font-size: 0.8em;
                    margin-left: 8px;
                    color: #667eea;
                    transition: all 0.3s ease;
                    position: relative;
                    vertical-align: middle;
                }

                .info-icon:hover {
                    color: #764ba2;
                    transform: scale(1.1);
                }

                /* Special styling for info icons in cards */
                .card .info-icon {
                    font-size: 0.7em;
                    margin-left: 6px;
                    opacity: 0.8;
                }

                .card .info-icon:hover {
                    opacity: 1;
                }

                /* Info tooltip styling */
                .info-tooltip {
                    position: fixed;
                    background: #000000;
                    color: #ffffff;
                    padding: 12px 16px;
                    border-radius: 8px;
                    font-size: 0.85em;
                    max-width: 300px;
                    white-space: normal;
                    text-align: left;
                    box-shadow: 0 6px 20px rgba(0, 0, 0, 0.5);
                    z-index: 10000;
                    opacity: 0;
                    visibility: hidden;
                    transition: all 0.3s ease;
                    pointer-events: none;
                    border: 1px solid #333333;
                }

                .info-tooltip::before {
                    content: '';
                    position: absolute;
                    border: 6px solid transparent;
                    z-index: 10000;
                }

                .info-tooltip.show {
                    opacity: 1;
                    visibility: visible;
                }

                /* Tooltip positioning */
                .info-tooltip.top::before {
                    top: 100%;
                    left: 50%;
                    transform: translateX(-50%);
                    border-top-color: #000000;
                }

                .info-tooltip.bottom::before {
                    bottom: 100%;
                    left: 50%;
                    transform: translateX(-50%);
                    border-bottom-color: #000000;
                }

                .info-tooltip.left::before {
                    left: 100%;
                    top: 50%;
                    transform: translateY(-50%);
                    border-left-color: #000000;
                }

                .info-tooltip.right::before {
                    right: 100%;
                    top: 50%;
                    transform: translateY(-50%);
                    border-right-color: #000000;
                }

                @media (max-width: 768px) {
                    .container {
                        padding: 15px;
                    }
                    
                    .header h1 {
                        font-size: 2em;
                    }
                    
                    .chart-container {
                        grid-template-columns: 1fr;
                    }
                    
                    .filter-controls {
                        flex-direction: column;
                        align-items: stretch;
                    }
                    
                    .summary-cards {
                        grid-template-columns: 1fr;
                    }
                    
                    .card {
                        padding: 25px 20px;
                    }
                    
                    .card-value {
                        font-size: 2em;
                    }
                }

                @media (max-width: 480px) {
                    .header {
                        padding: 30px 20px;
                    }
                    
                    .header h1 {
                        font-size: 1.8em;
                    }
                    
                    .charts-section,
                    .filters-section,
                    .table-section {
                        padding: 25px 20px;
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
                        <div class='card primary-card'>
                            <div class='card-icon'>📋</div>
                            <div class='card-value'>{data.TotalTasks}</div>
                            <div class='card-label'>Total Tasks <span class='info-icon' data-description='Shows total active tasks and completion progress. The progress bar indicates what percentage of tasks have been completed.'>ℹ️</span></div>
                            <div class='progress-bar'>
                                <div class='progress-fill' style='width: {data.CompletionRate}%'></div>
                            </div>
                            <div class='card-subtitle'>{data.CompletionRate:F1}% Complete</div>
                        </div>
                        
                        <div class='card success-card'>
                            <div class='card-icon'>✅</div>
                            <div class='card-value'>{data.CompletedTasks}</div>
                            <div class='card-label'>Completed Tasks <span class='info-icon' data-description='Displays completed tasks count and average tasks created per day. Higher numbers indicate better task completion habits.'>ℹ️</span></div>
                            <div class='card-subtitle'>{data.TasksPerDay:F1} tasks/day avg</div>
                        </div>
                        
                        <div class='card focus-card'>
                            <div class='card-icon'>🎯</div>
                            <div class='card-value'>{data.FocusSessionsCount}</div>
                            <div class='card-label'>Focus Sessions <span class='info-icon' data-description='Shows total focus sessions and average sessions per day. Focus sessions are dedicated time blocks for deep work.'>ℹ️</span></div>
                            <div class='card-subtitle'>{data.SessionsPerDay:F1} sessions/day</div>
                        </div>
                        
                        <div class='card time-card'>
                            <div class='card-icon'>⏱️</div>
                            <div class='card-value'>{data.TotalFocusTime.TotalHours:F1}h</div>
                            <div class='card-label'>Total Focus Time <span class='info-icon' data-description='Total time spent in focus mode and focus efficiency percentage. Efficiency shows how much of your work time was focused vs distracted.'>ℹ️</span></div>
                            <div class='card-subtitle'>{data.FocusEfficiency:F1}% efficiency</div>
                        </div>
                        
                        <div class='card productivity-card'>
                            <div class='card-icon'>📈</div>
                            <div class='card-value'>{data.AverageProductivityScore:F0}%</div>
                            <div class='card-label'>Avg Productivity <span class='info-icon' data-description='Average productivity score based on focus/break ratio and task completion patterns. Higher scores indicate better productivity habits.'>ℹ️</span></div>
                            <div class='card-subtitle'>Based on focus/break ratio</div>
                        </div>
                        
                        <div class='card workday-card'>
                            <div class='card-icon'>📅</div>
                            <div class='card-value'>{data.CompletedWorkDays}</div>
                            <div class='card-label'>Work Days <span class='info-icon' data-description='Number of completed work days and average work day duration. Shows consistency in work day management and time tracking.'>ℹ️</span></div>
                            <div class='card-subtitle'>{data.AverageWorkDayDuration:F1}h avg duration</div>
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
                        <h2>📊 Analytics Dashboard <span class='info-icon' data-description='Interactive charts showing your productivity patterns, task distribution, and time management insights. Hover over each chart for detailed information.'>ℹ️</span></h2>
                        <div class='chart-container'>
                            <div class='chart-wrapper'>
                                <h3>Task Status Distribution <span class='info-icon' data-description='Shows the distribution of tasks by their current status. Helps identify if you have too many pending tasks or if you are maintaining good completion rates.'>ℹ️</span></h3>
                                <canvas id='taskStatusChart' width='400' height='300'></canvas>
                            </div>
                            
                            <div class='chart-wrapper'>
                                <h3>Daily Task Completions <span class='info-icon' data-description='Displays daily task completion trends over time. Helps identify productive days and patterns in your work habits.'>ℹ️</span></h3>
                                <canvas id='dailyCompletionsChart' width='400' height='300'></canvas>
                            </div>
                            
                            <div class='chart-wrapper'>
                                <h3>Session Type Distribution <span class='info-icon' data-description='Shows the breakdown of different session types (Focus, Break, Pause). Helps understand your work rhythm and time management patterns.'>ℹ️</span></h3>
                                <canvas id='sessionTypeChart' width='400' height='300'></canvas>
                            </div>
                            
                            <div class='chart-wrapper'>
                                <h3>Hourly Activity Pattern <span class='info-icon' data-description='Displays activity levels by hour of the day. Helps identify your most productive hours and optimal work schedule.'>ℹ️</span></h3>
                                <canvas id='hourlyActivityChart' width='400' height='300'></canvas>
                            </div>
                            
                            <div class='chart-wrapper'>
                                <h3>Top Commands Used <span class='info-icon' data-description='Shows your most frequently used commands. Helps understand your workflow patterns and identify opportunities for automation.'>ℹ️</span></h3>
                                <canvas id='topCommandsChart' width='400' height='300'></canvas>
                            </div>
                            
                            <div class='chart-wrapper'>
                                <h3>Focus vs Break Time <span class='info-icon' data-description='Compares total focus time vs break time. Helps assess work-life balance and identify if you need more breaks or longer focus sessions.'>ℹ️</span></h3>
                                <canvas id='focusBreakChart' width='400' height='300'></canvas>
                            </div>
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
            var onBreakPercentage = data.TotalTasks > 0 ? ((double)data.OnBreakTasks / totalTasks * 100) : 0;

            var avgFocusMinutes = data.FocusSessionsCount > 0 ? data.TotalFocusTime.TotalMinutes / data.FocusSessionsCount : 0;
            var avgBreakMinutes = data.BreakSessionsCount > 0 ? data.TotalBreakTime.TotalMinutes / data.BreakSessionsCount : 0;

            // Safe TimeSpan formatting
            var focusTimeStr = data.TotalFocusTime.ToString(@"hh\:mm\:ss");
            var breakTimeStr = data.TotalBreakTime.ToString(@"hh\:mm\:ss");
            var workTimeStr = data.TotalWorkTime.ToString(@"hh\:mm");

            return $@"
                <div class='container'>
                    <div class='table-section'>
                        <h2>📋 Detailed Analytics Tables <span class='info-icon' data-description='Detailed breakdown of your productivity data. Each section provides comprehensive metrics to help you understand your work patterns and identify areas for improvement.'>ℹ️</span></h2>
                        
                        <h3>Task Status Distribution <span class='info-icon' data-description='Shows how your tasks are distributed across different statuses. Helps identify if you have too many pending tasks or if you need to focus on completing more tasks.'>ℹ️</span></h3>
                        <div class='table-container'>
                            <table id='taskSummaryTable'>
                                <thead>
                                    <tr>
                                        <th>Status</th>
                                        <th>Count</th>
                                        <th>Percentage</th>
                                        <th>Description</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        <td><strong>Total Tasks</strong></td>
                                        <td><strong>{data.TotalTasks}</strong></td>
                                        <td><strong>100%</strong></td>
                                        <td><em>All active tasks in the system</em></td>
                                    </tr>
                                    <tr>
                                        <td>✅ Completed</td>
                                        <td>{data.CompletedTasks}</td>
                                        <td>{data.CompletionRate:F1}%</td>
                                        <td>Successfully finished tasks</td>
                                    </tr>
                                    <tr>
                                        <td>📝 Pending</td>
                                        <td>{data.PendingTasks}</td>
                                        <td>{pendingPercentage:F1}%</td>
                                        <td>Tasks waiting to be started</td>
                                    </tr>
                                    <tr>
                                        <td>🎯 In Progress</td>
                                        <td>{data.InProgressTasks}</td>
                                        <td>{inProgressPercentage:F1}%</td>
                                        <td>Currently being worked on</td>
                                    </tr>
                                    <tr>
                                        <td>⏸️ Paused</td>
                                        <td>{data.PausedTasks}</td>
                                        <td>{pausedPercentage:F1}%</td>
                                        <td>Temporarily stopped tasks</td>
                                    </tr>
                                    <tr>
                                        <td>☕ On Break</td>
                                        <td>{data.OnBreakTasks}</td>
                                        <td>{onBreakPercentage:F1}%</td>
                                        <td>Tasks in break status</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                        
                        <h3>Session Performance Metrics <span class='info-icon' data-description='Detailed metrics about your focus and break sessions. Shows session counts, durations, and efficiency to help optimize your work patterns.'>ℹ️</span></h3>
                        <div class='table-container'>
                            <table id='sessionStatsTable'>
                                <thead>
                                    <tr>
                                        <th>Session Type</th>
                                        <th>Count</th>
                                        <th>Total Duration</th>
                                        <th>Average Duration</th>
                                        <th>Efficiency</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        <td>🎯 Focus Sessions</td>
                                        <td>{data.FocusSessionsCount}</td>
                                        <td>{focusTimeStr}</td>
                                        <td>{avgFocusMinutes:F1} min</td>
                                        <td>{data.FocusEfficiency:F1}%</td>
                                    </tr>
                                    <tr>
                                        <td>☕ Break Sessions</td>
                                        <td>{data.BreakSessionsCount}</td>
                                        <td>{breakTimeStr}</td>
                                        <td>{avgBreakMinutes:F1} min</td>
                                        <td>-</td>
                                    </tr>
                                    <tr>
                                        <td>⚡ Commands Executed</td>
                                        <td>{data.CommandsExecuted}</td>
                                        <td>-</td>
                                        <td>-</td>
                                        <td>-</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>

                        <h3>Productivity Metrics <span class='info-icon' data-description='Key productivity indicators that help assess your overall performance and work habits. Use these metrics to identify areas for improvement.'>ℹ️</span></h3>
                        <div class='table-container'>
                            <table id='productivityTable'>
                                <thead>
                                    <tr>
                                        <th>Metric</th>
                                        <th>Value</th>
                                        <th>Description</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        <td>📈 Average Productivity Score</td>
                                        <td>{data.AverageProductivityScore:F1}%</td>
                                        <td>Overall productivity rating</td>
                                    </tr>
                                    <tr>
                                        <td>📋 Tasks Per Day</td>
                                        <td>{data.TasksPerDay:F1}</td>
                                        <td>Average tasks created per day</td>
                                    </tr>
                                    <tr>
                                        <td>🔄 Sessions Per Day</td>
                                        <td>{data.SessionsPerDay:F1}</td>
                                        <td>Average focus/break sessions per day</td>
                                    </tr>
                                    <tr>
                                        <td>🎯 Focus Efficiency</td>
                                        <td>{data.FocusEfficiency:F1}%</td>
                                        <td>Focus time vs total work time ratio</td>
                                    </tr>
                                    <tr>
                                        <td>📅 Work Days Completed</td>
                                        <td>{data.CompletedWorkDays} / {data.TotalWorkDays}</td>
                                        <td>Completed vs total work days</td>
                                    </tr>
                                    <tr>
                                        <td>⏱️ Average Work Day Duration</td>
                                        <td>{data.AverageWorkDayDuration:F1} hours</td>
                                        <td>Average length of completed work days</td>
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
            sb.AppendLine("        <h2>👤 User & System Information <span class='info-icon' data-description='System and user information captured during report generation. Provides context about the environment where the data was collected.'>ℹ️</span></h2>");
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
            var workTimeStr = data.TotalWorkTime.ToString(@"hh\:mm");
            var completionRate = data.TotalWorkDays > 0 ? ((double)data.CompletedWorkDays / data.TotalWorkDays * 100) : 0;

            return $@"
                <div class='container'>
                    <div class='table-section'>
                        <h2>📅 Work Day Analytics <span class='info-icon' data-description='Analysis of your work day patterns. Shows how consistently you start and end work days, and how much time you spend in work mode.'>ℹ️</span></h2>
                        <div class='table-container'>
                            <table>
                                <thead>
                                    <tr>
                                        <th>Metric</th>
                                        <th>Value</th>
                                        <th>Description</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr>
                                        <td>📅 Total Work Days</td>
                                        <td>{data.TotalWorkDays}</td>
                                        <td>Total work days started</td>
                                    </tr>
                                    <tr>
                                        <td>✅ Completed Work Days</td>
                                        <td>{data.CompletedWorkDays}</td>
                                        <td>Work days that were properly ended</td>
                                    </tr>
                                    <tr>
                                        <td>📊 Completion Rate</td>
                                        <td>{completionRate:F1}%</td>
                                        <td>Percentage of work days completed</td>
                                    </tr>
                                    <tr>
                                        <td>⏱️ Total Work Time</td>
                                        <td>{workTimeStr}</td>
                                        <td>Total time spent in work mode</td>
                                    </tr>
                                    <tr>
                                        <td>📈 Average Work Day Duration</td>
                                        <td>{data.AverageWorkDayDuration:F1} hours</td>
                                        <td>Average length of completed work days</td>
                                    </tr>
                                    <tr>
                                        <td>🎯 Focus Efficiency</td>
                                        <td>{data.FocusEfficiency:F1}%</td>
                                        <td>Focus time vs total work time ratio</td>
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
                        labels: ['Completed', 'Pending', 'In Progress', 'Paused', 'On Break'],
                        datasets: [{{
                            data: [{data.CompletedTasks}, {data.PendingTasks}, {data.InProgressTasks}, {data.PausedTasks}, {data.OnBreakTasks}],
                            backgroundColor: ['#27ae60', '#3498db', '#e74c3c', '#f39c12', '#9b59b6'],
                            borderWidth: 3,
                            borderColor: '#fff'
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        plugins: {{
                            legend: {{
                                position: 'bottom',
                                labels: {{
                                    padding: 20,
                                    usePointStyle: true
                                }}
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
                            borderColor: '#667eea',
                            backgroundColor: 'rgba(102, 126, 234, 0.1)',
                            tension: 0.4,
                            fill: true,
                            borderWidth: 3
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        plugins: {{
                            legend: {{
                                display: false
                            }}
                        }},
                        scales: {{
                            y: {{
                                beginAtZero: true,
                                ticks: {{
                                    stepSize: 1
                                }},
                                grid: {{
                                    color: 'rgba(0,0,0,0.1)'
                                }}
                            }},
                            x: {{
                                grid: {{
                                    display: false
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
                            backgroundColor: ['#667eea', '#764ba2', '#f39c12', '#e74c3c', '#27ae60'],
                            borderWidth: 3,
                            borderColor: '#fff'
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        plugins: {{
                            legend: {{
                                position: 'bottom',
                                labels: {{
                                    padding: 20,
                                    usePointStyle: true
                                }}
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
                            backgroundColor: 'rgba(102, 126, 234, 0.8)',
                            borderColor: '#667eea',
                            borderWidth: 1,
                            borderRadius: 4
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        plugins: {{
                            legend: {{
                                display: false
                            }}
                        }},
                        scales: {{
                            y: {{
                                beginAtZero: true,
                                ticks: {{
                                    stepSize: 1
                                }},
                                grid: {{
                                    color: 'rgba(0,0,0,0.1)'
                                }}
                            }},
                            x: {{
                                grid: {{
                                    display: false
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
                            backgroundColor: 'rgba(118, 75, 162, 0.8)',
                            borderColor: '#764ba2',
                            borderWidth: 1,
                            borderRadius: 4
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        indexAxis: 'y',
                        plugins: {{
                            legend: {{
                                display: false
                            }}
                        }},
                        scales: {{
                            x: {{
                                beginAtZero: true,
                                grid: {{
                                    color: 'rgba(0,0,0,0.1)'
                                }}
                            }},
                            y: {{
                                grid: {{
                                    display: false
                                }}
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
                            backgroundColor: ['#667eea', '#764ba2'],
                            borderColor: ['#5a6fd8', '#6a4190'],
                            borderWidth: 2,
                            borderRadius: 8
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        plugins: {{
                            legend: {{
                                display: false
                            }}
                        }},
                        scales: {{
                            y: {{
                                beginAtZero: true,
                                grid: {{
                                    color: 'rgba(0,0,0,0.1)'
                                }}
                            }},
                            x: {{
                                grid: {{
                                    display: false
                                }}
                            }}
                        }}
                    }}
                }});

                // Info icon functionality
                let currentTooltip = null;
                let currentIcon = null;
                let tooltipTimeout = null;

                function showInfoTooltip(mouseX, mouseY, description) {{
                    // Remove any existing tooltip
                    hideInfoTooltip();
                    
                    // Create tooltip element
                    const tooltip = document.createElement('div');
                    tooltip.className = 'info-tooltip';
                    tooltip.textContent = description;
                    
                    // Add to page first to get dimensions
                    document.body.appendChild(tooltip);
                    
                    // Get tooltip dimensions
                    const tooltipRect = tooltip.getBoundingClientRect();
                    const viewportHeight = window.innerHeight;
                    const viewportWidth = window.innerWidth;
                    
                    // Calculate initial position (above mouse cursor)
                    let tooltipX = mouseX - (tooltipRect.width / 2);
                    let tooltipY = mouseY - tooltipRect.height - 15;
                    let position = 'top';
                    
                    // Adjust position if tooltip goes off screen
                    if (tooltipY < 10) {{
                        // Position below cursor
                        tooltipY = mouseY + 15;
                        position = 'bottom';
                    }}
                    
                    if (tooltipX < 10) {{
                        tooltipX = 10;
                    }} else if (tooltipX + tooltipRect.width > viewportWidth - 10) {{
                        tooltipX = viewportWidth - tooltipRect.width - 10;
                    }}
                    
                    if (tooltipY + tooltipRect.height > viewportHeight - 10) {{
                        tooltipY = viewportHeight - tooltipRect.height - 10;
                    }}
                    
                    // Apply position
                    tooltip.style.left = tooltipX + 'px';
                    tooltip.style.top = tooltipY + 'px';
                    tooltip.classList.add(position);
                    
                    // Show tooltip
                    setTimeout(() => {{
                        tooltip.classList.add('show');
                    }}, 10);
                    
                    currentTooltip = tooltip;
                }}

                function hideInfoTooltip() {{
                    if (currentTooltip) {{
                        currentTooltip.classList.remove('show');
                        setTimeout(() => {{
                            if (currentTooltip && currentTooltip.parentNode) {{
                                currentTooltip.parentNode.removeChild(currentTooltip);
                            }}
                        }}, 300);
                        currentTooltip = null;
                        currentIcon = null;
                    }}
                    
                    if (tooltipTimeout) {{
                        clearTimeout(tooltipTimeout);
                        tooltipTimeout = null;
                    }}
                }}

                // Initialize info icons on page load
                document.addEventListener('DOMContentLoaded', function() {{
                    // Add event listeners to all info icons
                    const infoIcons = document.querySelectorAll('.info-icon');
                    
                    infoIcons.forEach(icon => {{
                        const description = icon.getAttribute('data-description');
                        
                        if (description) {{
                            // Show tooltip on mouse enter with delay
                            icon.addEventListener('mouseenter', function(e) {{
                                currentIcon = this;
                                tooltipTimeout = setTimeout(() => {{
                                    showInfoTooltip(e.clientX, e.clientY, description);
                                }}, 300); // 300ms delay
                            }});
                            
                            // Update tooltip position on mouse move
                            icon.addEventListener('mousemove', function(e) {{
                                if (currentTooltip && currentIcon === this) {{
                                    const tooltipRect = currentTooltip.getBoundingClientRect();
                                    const viewportHeight = window.innerHeight;
                                    const viewportWidth = window.innerWidth;
                                    
                                    let tooltipX = e.clientX - (tooltipRect.width / 2);
                                    let tooltipY = e.clientY - tooltipRect.height - 15;
                                    
                                    // Adjust position if tooltip goes off screen
                                    if (tooltipY < 10) {{
                                        tooltipY = e.clientY + 15;
                                    }}
                                    
                                    if (tooltipX < 10) {{
                                        tooltipX = 10;
                                    }} else if (tooltipX + tooltipRect.width > viewportWidth - 10) {{
                                        tooltipX = viewportWidth - tooltipRect.width - 10;
                                    }}
                                    
                                    if (tooltipY + tooltipRect.height > viewportHeight - 10) {{
                                        tooltipY = viewportHeight - tooltipRect.height - 10;
                                    }}
                                    
                                    currentTooltip.style.left = tooltipX + 'px';
                                    currentTooltip.style.top = tooltipY + 'px';
                                }}
                            }});
                            
                            // Hide tooltip on mouse leave
                            icon.addEventListener('mouseleave', function(e) {{
                                if (currentIcon === this) {{
                                    hideInfoTooltip();
                                }}
                            }});
                            
                            // Show tooltip on click (for mobile)
                            icon.addEventListener('click', function(e) {{
                                e.preventDefault();
                                e.stopPropagation();
                                
                                if (currentTooltip && currentIcon === this) {{
                                    hideInfoTooltip();
                                }} else {{
                                    showInfoTooltip(e.clientX, e.clientY, description);
                                    currentIcon = this;
                                }}
                            }});
                        }}
                    }});
                    
                    // Hide tooltip when clicking outside
                    document.addEventListener('click', function(e) {{
                        if (!e.target.classList.contains('info-icon')) {{
                            hideInfoTooltip();
                        }}
                    }});
                    
                    // Hide tooltip on escape key
                    document.addEventListener('keydown', function(e) {{
                        if (e.key === 'Escape') {{
                            hideInfoTooltip();
                        }}
                    }});
                    
                    // Hide tooltip when mouse leaves the page
                    document.addEventListener('mouseleave', function(e) {{
                        hideInfoTooltip();
                    }});
                    
                    console.log('Info icon system initialized');
                }});
            ";
        }
    }

    public class ReportData
    {
        // Task Statistics
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int PausedTasks { get; set; }
        public int OnBreakTasks { get; set; }
        public double CompletionRate { get; set; }

        // Session Statistics
        public TimeSpan TotalFocusTime { get; set; }
        public TimeSpan TotalBreakTime { get; set; }
        public int FocusSessionsCount { get; set; }
        public int BreakSessionsCount { get; set; }
        public int CommandsExecuted { get; set; }

        // Productivity Metrics
        public double AverageProductivityScore { get; set; }
        public int TotalTasksCompleted { get; set; }
        public double FocusEfficiency { get; set; }
        public double TasksPerDay { get; set; }
        public double SessionsPerDay { get; set; }

        // Time-based Data
        public Dictionary<string, int> DailyTaskCompletions { get; set; } = new();
        public Dictionary<string, int> SessionTypeDistribution { get; set; } = new();
        public Dictionary<string, int> HourlyActivity { get; set; } = new();
        public Dictionary<string, int> TopCommands { get; set; } = new();

        // Work Day Statistics
        public int TotalWorkDays { get; set; }
        public int CompletedWorkDays { get; set; }
        public TimeSpan TotalWorkTime { get; set; }
        public double AverageWorkDayDuration { get; set; }

        // User and System Information
        public Dictionary<string, string> UserInfo { get; set; } = new();
    }

    public class ArchivedData
    {
        public List<TaskModel> Tasks { get; set; } = new();
        public List<SessionLog> SessionLogs { get; set; } = new();
        public List<WorkDay> WorkDays { get; set; } = new();
    }
}