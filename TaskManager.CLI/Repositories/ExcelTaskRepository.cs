using OfficeOpenXml;
using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;

namespace TaskManager.CLI.Repositories
{
    public class ExcelTaskRepository : ITaskRepository
    {
        private readonly string _filePath;
        private readonly string _archivePath;
        private readonly List<TaskModel> _tasks = new();
        private readonly List<SessionLog> _sessionLogs = new();
        private readonly List<WorkDay> _workDays = new();
        private FocusSession _todaySession;

        public ExcelTaskRepository()
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var taskManagerFolder = Path.Combine(documentsPath, "TaskManager");
                Directory.CreateDirectory(taskManagerFolder);

                _archivePath = Path.Combine(taskManagerFolder, "Archive");
                Directory.CreateDirectory(_archivePath);

                _filePath = Path.Combine(taskManagerFolder, "tasks.xlsx");

                ExcelPackage.License.SetNonCommercialPersonal("NIRAJ");

            LoadDataAsync().Wait();
        }

        private async Task LoadDataAsync()
        {
            if (!File.Exists(_filePath))
            {
                await CreateNewFileAsync();
                return;
            }

            using var package = new ExcelPackage(new FileInfo(_filePath));

            await LoadTasksAsync(package);
            await LoadSessionsAsync(package);
            await LoadWorkDaysAsync(package);
            await LoadSessionLogsAsync(package);
        }

        private async Task LoadTasksAsync(ExcelPackage package)
        {
            var taskSheet = package.Workbook.Worksheets["Tasks"];
            if (taskSheet?.Dimension == null) return;

            for (int row = 6; row <= taskSheet.Dimension.End.Row; row++)
            {
                Guid id;
                var idCell = taskSheet.Cells[row, 1].Value?.ToString();
                if (string.IsNullOrEmpty(idCell) || !Guid.TryParse(idCell, out id))
                {
                    id = Guid.NewGuid();
                    taskSheet.Cells[row, 1].Value = id; // Write back to Excel
                }

                var task = new TaskModel
                {
                    Id = id,
                    Description = taskSheet.Cells[row, 2].Value?.ToString() ?? "",
                    Status = Enum.Parse<TaskStatus>(taskSheet.Cells[row, 3].Value?.ToString() ?? "Pending"),
                    CreatedAt = DateTime.Parse(taskSheet.Cells[row, 4].Value?.ToString() ?? DateTime.UtcNow.ToString()),
                    CompletedAt = DateTime.TryParse(taskSheet.Cells[row, 5].Value?.ToString(), out var completed) ? completed : null,
                    PausedAt = DateTime.TryParse(taskSheet.Cells[row, 6].Value?.ToString(), out var paused) ? paused : null,
                    PauseReason = taskSheet.Cells[row, 7].Value?.ToString() ?? "",
                    IsFocused = bool.Parse(taskSheet.Cells[row, 8].Value?.ToString() ?? "false"),
                    FocusTime = TimeSpan.TryParse(taskSheet.Cells[row, 9].Value?.ToString(), out var focusTime) ? focusTime : TimeSpan.Zero
                };
                _tasks.Add(task);
            }
        }

        private async Task UpdateUserInfoTimestampAsync(ExcelPackage package)
        {
            try
            {
                var userInfoSheet = package.Workbook.Worksheets["UserInfo"];
                if (userInfoSheet?.Dimension == null) return;

                // Find the timestamp row and update it
                for (int row = 1; row <= userInfoSheet.Dimension.End.Row; row++)
                {
                    var cellValue = userInfoSheet.Cells[row, 1].Value?.ToString();
                    if (cellValue == "Information captured on:" || cellValue == "Last updated on:")
                    {
                        userInfoSheet.Cells[row, 1].Value = "Last updated on:";
                        userInfoSheet.Cells[row, 2].Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                        break;
                    }
                }
            }
            catch
            {
                // Ignore errors in updating timestamp
            }
        }

        private async Task LoadSessionsAsync(ExcelPackage package)
        {
            var sessionSheet = package.Workbook.Worksheets["Sessions"];
            var today = DateTime.UtcNow.Date;
            _todaySession = new FocusSession { SessionDate = today };

            if (sessionSheet?.Dimension == null) return;

            // Data starts from row 7 (after headers and formatting)
            for (int row = 7; row <= sessionSheet.Dimension.End.Row; row++)
            {
                if (DateTime.TryParse(sessionSheet.Cells[row, 1].Value?.ToString(), out var sessionDate) &&
                    sessionDate.Date == today)
                {
                    _todaySession.TotalFocusTime = TimeSpan.Parse(sessionSheet.Cells[row, 2].Value?.ToString() ?? "00:00:00");
                    _todaySession.TotalBreakTime = TimeSpan.Parse(sessionSheet.Cells[row, 3].Value?.ToString() ?? "00:00:00");
                    _todaySession.CompletedFocusSessions = int.Parse(sessionSheet.Cells[row, 4].Value?.ToString() ?? "0");
                    _todaySession.CompletedBreakSessions = int.Parse(sessionSheet.Cells[row, 5].Value?.ToString() ?? "0");
                    _todaySession.FocusMinutes = int.Parse(sessionSheet.Cells[row, 6].Value?.ToString() ?? "25");
                    _todaySession.BreakMinutes = int.Parse(sessionSheet.Cells[row, 7].Value?.ToString() ?? "5");
                    _todaySession.DayStartTime = DateTime.TryParse(sessionSheet.Cells[row, 8].Value?.ToString(), out var startTime) ? startTime : null;
                    _todaySession.DayEndTime = DateTime.TryParse(sessionSheet.Cells[row, 9].Value?.ToString(), out var endTime) ? endTime : null;
                    _todaySession.IsWorkDayActive = bool.Parse(sessionSheet.Cells[row, 10].Value?.ToString() ?? "false");
                    break;
                }
            }
        }

        private async Task LoadWorkDaysAsync(ExcelPackage package)
        {
            var workDaySheet = package.Workbook.Worksheets["WorkDays"];
            if (workDaySheet?.Dimension == null) return;

            for (int row = 7; row <= workDaySheet.Dimension.End.Row; row++)
            {
                Guid id;
                var idCell = workDaySheet.Cells[row, 1].Value?.ToString();
                if (string.IsNullOrEmpty(idCell) || !Guid.TryParse(idCell, out id))
                {
                    id = Guid.NewGuid();
                    workDaySheet.Cells[row, 1].Value = id; // Write back to Excel
                }

                var workDay = new WorkDay
                {
                    Id = id,
                    Date = DateTime.Parse(workDaySheet.Cells[row, 2].Value?.ToString() ?? DateTime.UtcNow.ToString()),
                    StartTime = DateTime.Parse(workDaySheet.Cells[row, 3].Value?.ToString() ?? DateTime.UtcNow.ToString()),
                    EndTime = DateTime.TryParse(workDaySheet.Cells[row, 4].Value?.ToString(), out var endTime) ? endTime : null,
                    PlannedDuration = TimeSpan.Parse(workDaySheet.Cells[row, 5].Value?.ToString() ?? "08:30:00"),
                    IsActive = bool.Parse(workDaySheet.Cells[row, 6].Value?.ToString() ?? "false")
                };
                _workDays.Add(workDay);
            }
        }

        private async Task LoadSessionLogsAsync(ExcelPackage package)
        {
            var logSheet = package.Workbook.Worksheets["SessionLogs"];
            if (logSheet?.Dimension == null) return;

            for (int row = 8; row <= logSheet.Dimension.End.Row; row++)
            {
                Guid id;
                var idCell = logSheet.Cells[row, 1].Value?.ToString();
                if (string.IsNullOrEmpty(idCell) || !Guid.TryParse(idCell, out id))
                {
                    id = Guid.NewGuid();
                    logSheet.Cells[row, 1].Value = id; // Write back to Excel
                }

                var log = new SessionLog
                {
                    Id = id,
                    Date = DateTime.Parse(logSheet.Cells[row, 2].Value?.ToString() ?? DateTime.UtcNow.ToString()),
                    StartTime = DateTime.Parse(logSheet.Cells[row, 3].Value?.ToString() ?? DateTime.UtcNow.ToString()),
                    EndTime = DateTime.TryParse(logSheet.Cells[row, 4].Value?.ToString(), out var endTime) ? endTime : null,
                    Type = Enum.Parse<SessionType>(logSheet.Cells[row, 5].Value?.ToString() ?? "Focus"),
                    TaskId = Guid.TryParse(logSheet.Cells[row, 6].Value?.ToString(), out var taskId) ? taskId : null,
                    Notes = logSheet.Cells[row, 7].Value?.ToString() ?? ""
                };
                _sessionLogs.Add(log);
            }
        }

        private async Task CreateNewFileAsync()
        {
            using var package = new ExcelPackage();

            CreateTasksSheet(package);
            CreateSessionsSheet(package);
            CreateWorkDaysSheet(package);
            CreateSessionLogsSheet(package);
            CreateSettingsSheet(package);
            CreateUserInfoSheet(package);

            await package.SaveAsAsync(new FileInfo(_filePath));
            _todaySession = new FocusSession { SessionDate = DateTime.UtcNow.Date };
        }

        private void CreateTasksSheet(ExcelPackage package)
        {
            var taskSheet = package.Workbook.Worksheets.Add("Tasks");

            // Add title and description
            taskSheet.Cells[1, 1].Value = "TASK MANAGEMENT DATA";
            taskSheet.Cells[1, 1, 1, 9].Merge = true;
            taskSheet.Cells[1, 1].Style.Font.Bold = true;
            taskSheet.Cells[1, 1].Style.Font.Size = 14;
            taskSheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            taskSheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            taskSheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);

            // Add description row
            taskSheet.Cells[2, 1].Value = "This worksheet contains all task data including status, timing, and focus information";
            taskSheet.Cells[2, 1, 2, 9].Merge = true;
            taskSheet.Cells[2, 1].Style.Font.Italic = true;
            taskSheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            // Create headers
            var headers = new string[]
            {
                "Task ID", "Task Description", "Current Status", "Created Date/Time",
                "Completed Date/Time", "Paused Date/Time", "Pause Reason",
                "Currently Focused", "Total Focus Time"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                taskSheet.Cells[4, i + 1].Value = headers[i];
                taskSheet.Cells[4, i + 1].Style.Font.Bold = true;
                taskSheet.Cells[4, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                taskSheet.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                taskSheet.Cells[4, i + 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // Add data type descriptions
            var dataTypes = new string[]
            {
                "Guid", "Text", "Enum", "DateTime", "DateTime", "DateTime",
                "Text", "Boolean", "TimeSpan"
            };

            for (int i = 0; i < dataTypes.Length; i++)
            {
                taskSheet.Cells[5, i + 1].Value = $"({dataTypes[i]})";
                taskSheet.Cells[5, i + 1].Style.Font.Size = 9;
                taskSheet.Cells[5, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
                taskSheet.Cells[5, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // Auto-fit columns
            taskSheet.Cells[taskSheet.Dimension.Address].AutoFitColumns();

            // Set minimum column widths
            for (int i = 1; i <= headers.Length; i++)
            {
                if (taskSheet.Column(i).Width < 12)
                    taskSheet.Column(i).Width = 12;
            }
        }

        private void CreateSessionsSheet(ExcelPackage package)
        {
            var sessionSheet = package.Workbook.Worksheets.Add("Sessions");

            // Add title and description
            sessionSheet.Cells[1, 1].Value = "DAILY SESSION TRACKING";
            sessionSheet.Cells[1, 1, 1, 10].Merge = true;
            sessionSheet.Cells[1, 1].Style.Font.Bold = true;
            sessionSheet.Cells[1, 1].Style.Font.Size = 14;
            sessionSheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            sessionSheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            sessionSheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);

            // Add description row
            sessionSheet.Cells[2, 1].Value = "Daily aggregated data for focus sessions, break sessions, and work day management";
            sessionSheet.Cells[2, 1, 2, 10].Merge = true;
            sessionSheet.Cells[2, 1].Style.Font.Italic = true;
            sessionSheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            // Create headers
            var headers = new string[]
            {
                "Session Date", "Total Focus Time", "Total Break Time", "Focus Sessions Count",
                "Break Sessions Count", "Focus Duration (min)", "Break Duration (min)",
                "Work Day Start", "Work Day End", "Work Day Active"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sessionSheet.Cells[4, i + 1].Value = headers[i];
                sessionSheet.Cells[4, i + 1].Style.Font.Bold = true;
                sessionSheet.Cells[4, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sessionSheet.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                sessionSheet.Cells[4, i + 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // Add data type descriptions
            var dataTypes = new string[]
            {
                "Date", "TimeSpan", "TimeSpan", "Integer", "Integer",
                "Integer", "Integer", "DateTime", "DateTime", "Boolean"
            };

            for (int i = 0; i < dataTypes.Length; i++)
            {
                sessionSheet.Cells[5, i + 1].Value = $"({dataTypes[i]})";
                sessionSheet.Cells[5, i + 1].Style.Font.Size = 9;
                sessionSheet.Cells[5, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
                sessionSheet.Cells[5, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // Auto-fit columns
            sessionSheet.Cells.AutoFitColumns();

            // Set minimum column widths
            for (int i = 1; i <= headers.Length; i++)
            {
                if (sessionSheet.Column(i).Width < 15)
                    sessionSheet.Column(i).Width = 15;
            }
        }

        private void CreateWorkDaysSheet(ExcelPackage package)
        {
            var workDaySheet = package.Workbook.Worksheets.Add("WorkDays");

            // Add title and description
            workDaySheet.Cells[1, 1].Value = "WORK DAY SCHEDULE";
            workDaySheet.Cells[1, 1, 1, 5].Merge = true;
            workDaySheet.Cells[1, 1].Style.Font.Bold = true;
            workDaySheet.Cells[1, 1].Style.Font.Size = 14;
            workDaySheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            workDaySheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            workDaySheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);

            // Add description row
            workDaySheet.Cells[2, 1].Value = "Work day start/end times and planned duration tracking for productivity analysis";
            workDaySheet.Cells[2, 1, 2, 5].Merge = true;
            workDaySheet.Cells[2, 1].Style.Font.Italic = true;
            workDaySheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            // Create headers
            var headers = new string[]
            {
                "Work Date", "Start Time", "End Time", "Planned Duration", "Status"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                workDaySheet.Cells[4, i + 1].Value = headers[i];
                workDaySheet.Cells[4, i + 1].Style.Font.Bold = true;
                workDaySheet.Cells[4, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                workDaySheet.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                workDaySheet.Cells[4, i + 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // Add data type descriptions
            var dataTypes = new string[]
            {
                "Guid", "DateTime", "DateTime", "TimeSpan", "Boolean"
            };

            for (int i = 0; i < dataTypes.Length; i++)
            {
                workDaySheet.Cells[5, i + 1].Value = $"({dataTypes[i]})";
                workDaySheet.Cells[5, i + 1].Style.Font.Size = 9;
                workDaySheet.Cells[5, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
                workDaySheet.Cells[5, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // Auto-fit columns
            workDaySheet.Cells.AutoFitColumns();

            // Set minimum column widths
            for (int i = 1; i <= headers.Length; i++)
            {
                if (workDaySheet.Column(i).Width < 18)
                    workDaySheet.Column(i).Width = 18;
            }
        }

        private void CreateSessionLogsSheet(ExcelPackage package)
        {
            var logSheet = package.Workbook.Worksheets.Add("SessionLogs");

            // Add title and description
            logSheet.Cells[1, 1].Value = "DETAILED SESSION ACTIVITY LOG";
            logSheet.Cells[1, 1, 1, 6].Merge = true;
            logSheet.Cells[1, 1].Style.Font.Bold = true;
            logSheet.Cells[1, 1].Style.Font.Size = 14;
            logSheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            logSheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            logSheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCyan);

            // Add description row
            logSheet.Cells[2, 1].Value = "Chronological log of all focus sessions, breaks, and pauses with detailed timing information";
            logSheet.Cells[2, 1, 2, 6].Merge = true;
            logSheet.Cells[2, 1].Style.Font.Italic = true;
            logSheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            // Create headers
            var headers = new string[]
            {
                "Log ID", "Session Start Time", "Session End Time", "Session Type", "Related Task ID", "Activity Notes"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                logSheet.Cells[4, i + 1].Value = headers[i];
                logSheet.Cells[4, i + 1].Style.Font.Bold = true;
                logSheet.Cells[4, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                logSheet.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                logSheet.Cells[4, i + 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // Add data type descriptions
            var dataTypes = new string[]
            {
                "Guid", "DateTime", "DateTime", "Enum", "Guid", "Text"
            };

            for (int i = 0; i < dataTypes.Length; i++)
            {
                logSheet.Cells[5, i + 1].Value = $"({dataTypes[i]})";
                logSheet.Cells[5, i + 1].Style.Font.Size = 9;
                logSheet.Cells[5, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
                logSheet.Cells[5, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // Add example/legend row
            logSheet.Cells[6, 1].Value = "Examples:";
            logSheet.Cells[6, 1].Style.Font.Bold = true;
            logSheet.Cells[6, 4].Value = "Focus/Break/Pause/Command";
            logSheet.Cells[6, 4].Style.Font.Size = 9;
            logSheet.Cells[6, 4].Style.Font.Color.SetColor(System.Drawing.Color.Blue);

            // Auto-fit columns
            logSheet.Cells.AutoFitColumns();

            // Set minimum column widths
            for (int i = 1; i <= headers.Length; i++)
            {
                if (logSheet.Column(i).Width < 16)
                    logSheet.Column(i).Width = 16;
            }

            // Make the notes column wider
            logSheet.Column(6).Width = 40;
        }

        private void CreateSettingsSheet(ExcelPackage package)
        {
            var settingsSheet = package.Workbook.Worksheets.Add("Settings");

            // Add title and description
            settingsSheet.Cells[1, 1].Value = "APPLICATION SETTINGS & CONFIGURATION";
            settingsSheet.Cells[1, 1, 1, 2].Merge = true;
            settingsSheet.Cells[1, 1].Style.Font.Bold = true;
            settingsSheet.Cells[1, 1].Style.Font.Size = 14;
            settingsSheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            settingsSheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            settingsSheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightPink);

            // Add description row
            settingsSheet.Cells[2, 1].Value = "User preferences and default timer settings for the Task Manager application";
            settingsSheet.Cells[2, 1, 2, 2].Merge = true;
            settingsSheet.Cells[2, 1].Style.Font.Italic = true;
            settingsSheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            // Create headers
            var headers = new string[] { "Setting Name", "Setting Value" };

            for (int i = 0; i < headers.Length; i++)
            {
                settingsSheet.Cells[4, i + 1].Value = headers[i];
                settingsSheet.Cells[4, i + 1].Style.Font.Bold = true;
                settingsSheet.Cells[4, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                settingsSheet.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                settingsSheet.Cells[4, i + 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // Add data type descriptions
            var dataTypes = new string[] { "Text", "Various" };

            for (int i = 0; i < dataTypes.Length; i++)
            {
                settingsSheet.Cells[5, i + 1].Value = $"({dataTypes[i]})";
                settingsSheet.Cells[5, i + 1].Style.Font.Size = 9;
                settingsSheet.Cells[5, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
                settingsSheet.Cells[5, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // Add default settings
            var defaultSettings = new[]
            {
                new { Setting = "DefaultFocusMinutes", Value = "25" },
                new { Setting = "DefaultBreakMinutes", Value = "5" },
                new { Setting = "WorkDayDurationHours", Value = "8.5" },
                new { Setting = "EndOfDayWarningMinutes", Value = "15" },
                new { Setting = "AutoBackup", Value = "true" },
                new { Setting = "BackupRetentionDays", Value = "30" },
                new { Setting = "EnableSounds", Value = "true" },
                new { Setting = "EnablePopupNotifications", Value = "true" }
            };

            for (int i = 0; i < defaultSettings.Length; i++)
            {
                var row = i + 7;
                settingsSheet.Cells[row, 1].Value = defaultSettings[i].Setting;
                settingsSheet.Cells[row, 2].Value = defaultSettings[i].Value;

                // Alternate row colors
                if (i % 2 == 0)
                {
                    settingsSheet.Cells[row, 1, row, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    settingsSheet.Cells[row, 1, row, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.WhiteSmoke);
                }
            }

            // Add instruction note
            var instructionRow = 7 + defaultSettings.Length + 2;
            settingsSheet.Cells[instructionRow, 1].Value = "Note: Modify values in this sheet to customize application behavior";
            settingsSheet.Cells[instructionRow, 1, instructionRow, 2].Merge = true;
            settingsSheet.Cells[instructionRow, 1].Style.Font.Size = 10;
            settingsSheet.Cells[instructionRow, 1].Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);
            settingsSheet.Cells[instructionRow, 1].Style.Font.Italic = true;

            // Auto-fit columns
            settingsSheet.Cells.AutoFitColumns();

            // Set minimum column widths
            settingsSheet.Column(1).Width = Math.Max(settingsSheet.Column(1).Width, 25);
            settingsSheet.Column(2).Width = Math.Max(settingsSheet.Column(2).Width, 15);
        }

        private void CreateUserInfoSheet(ExcelPackage package)
        {
            var userInfoSheet = package.Workbook.Worksheets.Add("UserInfo");

            // Add title and description
            userInfoSheet.Cells[1, 1].Value = "SYSTEM & USER INFORMATION";
            userInfoSheet.Cells[1, 1, 1, 2].Merge = true;
            userInfoSheet.Cells[1, 1].Style.Font.Bold = true;
            userInfoSheet.Cells[1, 1].Style.Font.Size = 14;
            userInfoSheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            userInfoSheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            userInfoSheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSteelBlue);

            // Add description row
            userInfoSheet.Cells[2, 1].Value = "Comprehensive system and user environment information captured at application startup";
            userInfoSheet.Cells[2, 1, 2, 2].Merge = true;
            userInfoSheet.Cells[2, 1].Style.Font.Italic = true;
            userInfoSheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

            // Create headers
            var headers = new string[] { "Information Category", "Value" };

            for (int i = 0; i < headers.Length; i++)
            {
                userInfoSheet.Cells[4, i + 1].Value = headers[i];
                userInfoSheet.Cells[4, i + 1].Style.Font.Bold = true;
                userInfoSheet.Cells[4, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                userInfoSheet.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                userInfoSheet.Cells[4, i + 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // Collect system information
            var systemInfo = CollectSystemInformation();

            int currentRow = 6;
            string currentCategory = "";

            foreach (var info in systemInfo)
            {
                // Add category headers
                if (info.Category != currentCategory)
                {
                    currentCategory = info.Category;

                    // Category header
                    userInfoSheet.Cells[currentRow, 1].Value = $"=== {currentCategory} ===";
                    userInfoSheet.Cells[currentRow, 1, currentRow, 2].Merge = true;
                    userInfoSheet.Cells[currentRow, 1].Style.Font.Bold = true;
                    userInfoSheet.Cells[currentRow, 1].Style.Font.Size = 12;
                    userInfoSheet.Cells[currentRow, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    userInfoSheet.Cells[currentRow, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    userInfoSheet.Cells[currentRow, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    currentRow++;

                    // Empty row for spacing
                    currentRow++;
                }

                // Add information row
                userInfoSheet.Cells[currentRow, 1].Value = info.Name;
                userInfoSheet.Cells[currentRow, 2].Value = info.Value;

                // Formatting based on importance
                if (info.IsImportant)
                {
                    userInfoSheet.Cells[currentRow, 1].Style.Font.Bold = true;
                    userInfoSheet.Cells[currentRow, 1, currentRow, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    userInfoSheet.Cells[currentRow, 1, currentRow, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                }
                else if (currentRow % 2 == 0)
                {
                    userInfoSheet.Cells[currentRow, 1, currentRow, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    userInfoSheet.Cells[currentRow, 1, currentRow, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.WhiteSmoke);
                }

                currentRow++;
            }

            // Add footer with timestamp
            currentRow += 2;
            userInfoSheet.Cells[currentRow, 1].Value = "Information captured on:";
            userInfoSheet.Cells[currentRow, 2].Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            userInfoSheet.Cells[currentRow, 1].Style.Font.Italic = true;
            userInfoSheet.Cells[currentRow, 2].Style.Font.Italic = true;
            userInfoSheet.Cells[currentRow, 1, currentRow, 2].Style.Font.Color.SetColor(System.Drawing.Color.DarkGray);

            // Auto-fit columns
            userInfoSheet.Cells.AutoFitColumns();

            // Set minimum and maximum column widths
            userInfoSheet.Column(1).Width = Math.Max(userInfoSheet.Column(1).Width, 30);
            userInfoSheet.Column(2).Width = Math.Max(userInfoSheet.Column(2).Width, 40);
            if (userInfoSheet.Column(2).Width > 80) userInfoSheet.Column(2).Width = 80;
        }

        private List<SystemInformation> CollectSystemInformation()
        {
            var systemInfo = new List<SystemInformation>();

            try
            {
                // User Information
                systemInfo.Add(new SystemInformation("User Information", "Current User", Environment.UserName, true));
                systemInfo.Add(new SystemInformation("User Information", "User Domain", Environment.UserDomainName));
                systemInfo.Add(new SystemInformation("User Information", "Is Administrator", IsAdministrator().ToString()));
                systemInfo.Add(new SystemInformation("User Information", "Interactive Mode", Environment.UserInteractive.ToString()));

                // System Information
                systemInfo.Add(new SystemInformation("System Information", "Computer Name", Environment.MachineName, true));
                systemInfo.Add(new SystemInformation("System Information", "Operating System", Environment.OSVersion.ToString(), true));
                systemInfo.Add(new SystemInformation("System Information", "OS Platform", Environment.OSVersion.Platform.ToString()));
                systemInfo.Add(new SystemInformation("System Information", "OS Version", Environment.OSVersion.Version.ToString()));
                systemInfo.Add(new SystemInformation("System Information", "Is 64-bit OS", Environment.Is64BitOperatingSystem.ToString()));
                systemInfo.Add(new SystemInformation("System Information", "System Directory", Environment.SystemDirectory));

                // Hardware Information
                systemInfo.Add(new SystemInformation("Hardware Information", "Processor Count", Environment.ProcessorCount.ToString(), true));
                systemInfo.Add(new SystemInformation("Hardware Information", "Is 64-bit Process", Environment.Is64BitProcess.ToString()));
                systemInfo.Add(new SystemInformation("Hardware Information", "Working Set", FormatBytes(Environment.WorkingSet)));
                systemInfo.Add(new SystemInformation("Hardware Information", "System Page Size", Environment.SystemPageSize.ToString()));

                // .NET Runtime Information
                systemInfo.Add(new SystemInformation("Runtime Information", ".NET Version", Environment.Version.ToString(), true));
                systemInfo.Add(new SystemInformation("Runtime Information", "CLR Version", Environment.Version.ToString()));
                systemInfo.Add(new SystemInformation("Runtime Information", "Has Shutdown Started", Environment.HasShutdownStarted.ToString()));
                systemInfo.Add(new SystemInformation("Runtime Information", "Tick Count", Environment.TickCount.ToString()));
                systemInfo.Add(new SystemInformation("Runtime Information", "Tick Count (64-bit)", Environment.TickCount64.ToString()));

                // Application Information
                systemInfo.Add(new SystemInformation("Application Information", "Command Line", Environment.CommandLine));
                systemInfo.Add(new SystemInformation("Application Information", "Current Directory", Environment.CurrentDirectory, true));
                systemInfo.Add(new SystemInformation("Application Information", "Process ID", Environment.ProcessId.ToString()));
                systemInfo.Add(new SystemInformation("Application Information", "Process Path", Environment.ProcessPath ?? "Not Available"));

                // Environment Paths
                systemInfo.Add(new SystemInformation("Environment Paths", "Desktop", Environment.GetFolderPath(Environment.SpecialFolder.Desktop)));
                systemInfo.Add(new SystemInformation("Environment Paths", "Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));
                systemInfo.Add(new SystemInformation("Environment Paths", "Program Files", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)));
                systemInfo.Add(new SystemInformation("Environment Paths", "System Root", Environment.GetFolderPath(Environment.SpecialFolder.System)));
                systemInfo.Add(new SystemInformation("Environment Paths", "Temp Directory", Path.GetTempPath()));
                systemInfo.Add(new SystemInformation("Environment Paths", "User Profile", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));

                // Localization Information
                systemInfo.Add(new SystemInformation("Localization", "Current Culture", System.Globalization.CultureInfo.CurrentCulture.DisplayName));
                systemInfo.Add(new SystemInformation("Localization", "Current UI Culture", System.Globalization.CultureInfo.CurrentUICulture.DisplayName));
                systemInfo.Add(new SystemInformation("Localization", "Time Zone", TimeZoneInfo.Local.DisplayName));
                systemInfo.Add(new SystemInformation("Localization", "UTC Offset", TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).ToString()));

                // Network Information
                systemInfo.Add(new SystemInformation("Network Information", "Computer Name (NetBIOS)", Environment.MachineName));

                // Important Environment Variables
                var importantEnvVars = new[] { "PATH", "USERPROFILE", "APPDATA", "LOCALAPPDATA", "PROGRAMFILES", "WINDIR" };
                foreach (var envVar in importantEnvVars)
                {
                    var value = Environment.GetEnvironmentVariable(envVar);
                    if (!string.IsNullOrEmpty(value))
                    {
                        systemInfo.Add(new SystemInformation("Environment Variables", envVar, value.Length > 100 ? value.Substring(0, 100) + "..." : value));
                    }
                }

                // Task Manager Specific Information
                systemInfo.Add(new SystemInformation("Task Manager Info", "Installation Date", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), true));
                systemInfo.Add(new SystemInformation("Task Manager Info", "Data Directory", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TaskManager")));
                systemInfo.Add(new SystemInformation("Task Manager Info", "Application Version", "1.0.0"));
                systemInfo.Add(new SystemInformation("Task Manager Info", "Excel File Path", _filePath));
            }
            catch (Exception ex)
            {
                systemInfo.Add(new SystemInformation("Error", "Information Collection Error", ex.Message));
            }

            return systemInfo.OrderBy(x => x.Category).ThenBy(x => x.Name).ToList();
        }

        private bool IsAdministrator()
        {
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private class SystemInformation
        {
            public string Category { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
            public bool IsImportant { get; set; }

            public SystemInformation(string category, string name, string value, bool isImportant = false)
            {
                Category = category;
                Name = name;
                Value = value ?? "Not Available";
                IsImportant = isImportant;
            }
        }

        public Task<List<TaskModel>> GetAllTasksAsync()
        {
            return Task.FromResult(_tasks.Where(t => t.Status != TaskStatus.Deleted).ToList());
        }

        public Task<TaskModel?> GetTaskByIdAsync(Guid id)
        {
            return Task.FromResult(_tasks.FirstOrDefault(t => t.Id == id && t.Status != TaskStatus.Deleted));
        }

        public Task<Guid> AddTaskAsync(TaskModel task)
        {
            task.Id = Guid.NewGuid();
            task.CreatedAt = DateTime.UtcNow;
            _tasks.Add(task);
            return Task.FromResult(task.Id);
        }

        public Task UpdateTaskAsync(TaskModel task)
        {
            var existingTask = _tasks.FirstOrDefault(t => t.Id == task.Id);
            if (existingTask != null)
            {
                var index = _tasks.IndexOf(existingTask);
                _tasks[index] = task;
            }
            return Task.CompletedTask;
        }

        public Task DeleteTaskAsync(Guid id)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                task.Status = TaskStatus.Deleted;
            }
            return Task.CompletedTask;
        }

        public Task<FocusSession> GetTodaySessionAsync()
        {
            return Task.FromResult(_todaySession);
        }

        public Task UpdateSessionAsync(FocusSession session)
        {
            _todaySession = session;
            return Task.CompletedTask;
        }

        public Task<WorkDay?> GetTodayWorkDayAsync()
        {
            var today = DateTime.UtcNow.Date;
            return Task.FromResult(_workDays.FirstOrDefault(w => w.Date == today));
        }

        public async Task<WorkDay> StartWorkDayAsync()
        {
            var today = DateTime.UtcNow.Date;
            var existingWorkDay = _workDays.FirstOrDefault(w => w.Date == today);

            if (existingWorkDay != null && existingWorkDay.IsActive)
            {
                return existingWorkDay;
            }

            var workDay = new WorkDay
            {
                Id = Guid.NewGuid(),
                Date = today,
                StartTime = DateTime.UtcNow,
                IsActive = true,
                PlannedDuration = TimeSpan.FromHours(8.5)
            };

            if (existingWorkDay != null)
            {
                var index = _workDays.IndexOf(existingWorkDay);
                _workDays[index] = workDay;
            }
            else
            {
                _workDays.Add(workDay);
            }

            return workDay;
        }

        public async Task<WorkDay> EndWorkDayAsync()
        {
            var today = DateTime.UtcNow.Date;
            var workDay = _workDays.FirstOrDefault(w => w.Date == today);

            if (workDay != null)
            {
                workDay.EndTime = DateTime.UtcNow;
                workDay.IsActive = false;
            }

            return workDay ?? new WorkDay { Date = today, EndTime = DateTime.UtcNow };
        }

        public Task UpdateWorkDayAsync(WorkDay workDay)
        {
            var existing = _workDays.FirstOrDefault(w => w.Date == workDay.Date);
            if (existing != null)
            {
                var index = _workDays.IndexOf(existing);
                _workDays[index] = workDay;
            }
            else
            {
                _workDays.Add(workDay);
            }
            return Task.CompletedTask;
        }

        public Task AddSessionLogAsync(SessionLog sessionLog)
        {
            _sessionLogs.Add(sessionLog);
            return Task.CompletedTask;
        }

        public Task<List<SessionLog>> GetTodaySessionLogsAsync()
        {
            var today = DateTime.UtcNow.Date;
            return Task.FromResult(_sessionLogs.Where(s => s.Date == today).ToList());
        }

        public async Task<DayStatistics> GetDayStatisticsAsync(DateTime date)
        {
            var session = date.Date == DateTime.UtcNow.Date ? _todaySession :
                new FocusSession { SessionDate = date };

            var tasksCompleted = _tasks.Count(t => t.CompletedAt?.Date == date.Date);
            var totalActiveTime = session.TotalFocusTime + session.TotalBreakTime;
            var productivityScore = totalActiveTime.TotalMinutes > 0 ?
                session.TotalFocusTime.TotalMinutes / totalActiveTime.TotalMinutes : 0;

            return new DayStatistics
            {
                Date = date,
                TotalFocusTime = session.TotalFocusTime,
                TotalBreakTime = session.TotalBreakTime,
                TasksCompleted = tasksCompleted,
                FocusSessionsCompleted = session.CompletedFocusSessions,
                BreakSessionsCompleted = session.CompletedBreakSessions,
                ProductivityScore = productivityScore
            };
        }

        public async Task CreateBackupAsync()
        {
            var today = DateTime.UtcNow.Date;
            var backupFolder = Path.Combine(_archivePath, today.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(backupFolder);

            var timestamp = DateTime.UtcNow.ToString("HHmmss");
            var backupFileName = $"tasks_backup_{today:yyyy-MM-dd}_{timestamp}.xlsx";
            var backupPath = Path.Combine(backupFolder, backupFileName);

            File.Copy(_filePath, backupPath, true);

            // Clean up old backups (keep 30 days)
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            foreach (var directory in Directory.GetDirectories(_archivePath))
            {
                var dirInfo = new DirectoryInfo(directory);
                if (dirInfo.CreationTime < cutoffDate)
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        public async Task SaveAsync()
        {
            using var package = new ExcelPackage(new FileInfo(_filePath));

            await SaveTasksAsync(package);
            await SaveSessionsAsync(package);
            await SaveWorkDaysAsync(package);
            await SaveSessionLogsAsync(package);

            // Update UserInfo sheet with current timestamp if it exists
            await UpdateUserInfoTimestampAsync(package);

            await package.SaveAsync();
        }

        private async Task SaveTasksAsync(ExcelPackage package)
        {
            var taskSheet = package.Workbook.Worksheets["Tasks"];
            
            // Ensure the metadata rows exist
            EnsureTasksSheetStructure(taskSheet);
            
            if (taskSheet.Dimension != null)
            {
                taskSheet.Cells[7, 1, taskSheet.Dimension.End.Row, 9].Clear();
            }

            for (int i = 0; i < _tasks.Count; i++)
            {
                var task = _tasks[i];
                var row = i + 7;
                taskSheet.Cells[row, 1].Value = task.Id;
                taskSheet.Cells[row, 2].Value = task.Description;
                taskSheet.Cells[row, 3].Value = task.Status.ToString();
                taskSheet.Cells[row, 4].Value = task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                taskSheet.Cells[row, 5].Value = task.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss");
                taskSheet.Cells[row, 6].Value = task.PausedAt?.ToString("yyyy-MM-dd HH:mm:ss");
                taskSheet.Cells[row, 7].Value = task.PauseReason;
                taskSheet.Cells[row, 8].Value = task.IsFocused;
                taskSheet.Cells[row, 9].Value = task.FocusTime.ToString();
            }
        }

        private void EnsureTasksSheetStructure(ExcelWorksheet taskSheet)
        {
            // Check if title exists, if not create the structure
            if (taskSheet.Cells[1, 1].Value?.ToString() != "TASK MANAGEMENT DATA")
            {
                // Add title and description
                taskSheet.Cells[1, 1].Value = "TASK MANAGEMENT DATA";
                taskSheet.Cells[1, 1, 1, 9].Merge = true;
                taskSheet.Cells[1, 1].Style.Font.Bold = true;
                taskSheet.Cells[1, 1].Style.Font.Size = 14;
                taskSheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                taskSheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                taskSheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);

                // Add description row
                taskSheet.Cells[2, 1].Value = "This worksheet contains all task data including status, timing, and focus information";
                taskSheet.Cells[2, 1, 2, 9].Merge = true;
                taskSheet.Cells[2, 1].Style.Font.Italic = true;
                taskSheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                // Create headers
                var headers = new string[]
                {
                    "Task ID", "Task Description", "Current Status", "Created Date/Time",
                    "Completed Date/Time", "Paused Date/Time", "Pause Reason",
                    "Currently Focused", "Total Focus Time"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    taskSheet.Cells[4, i + 1].Value = headers[i];
                    taskSheet.Cells[4, i + 1].Style.Font.Bold = true;
                    taskSheet.Cells[4, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    taskSheet.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    taskSheet.Cells[4, i + 1].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }

                // Add data type descriptions
                var dataTypes = new string[]
                {
                    "Guid", "Text", "Enum", "DateTime", "DateTime", "DateTime",
                    "Text", "Boolean", "TimeSpan"
                };

                for (int i = 0; i < dataTypes.Length; i++)
                {
                    taskSheet.Cells[5, i + 1].Value = $"({dataTypes[i]})";
                    taskSheet.Cells[5, i + 1].Style.Font.Size = 9;
                    taskSheet.Cells[5, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
                    taskSheet.Cells[5, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }

                // Auto-fit columns
                taskSheet.Cells[taskSheet.Dimension.Address].AutoFitColumns();

                // Set minimum column widths
                for (int i = 1; i <= headers.Length; i++)
                {
                    if (taskSheet.Column(i).Width < 12)
                        taskSheet.Column(i).Width = 12;
                }
            }
        }

        private async Task SaveSessionsAsync(ExcelPackage package)
        {
            var sessionSheet = package.Workbook.Worksheets["Sessions"];
            bool sessionExists = false;

            if (sessionSheet.Dimension != null)
            {
                // Check from row 7 onwards for existing session data
                for (int row = 7; row <= sessionSheet.Dimension.End.Row; row++)
                {
                    if (DateTime.TryParse(sessionSheet.Cells[row, 1].Value?.ToString(), out var sessionDate) &&
                        sessionDate.Date == _todaySession.SessionDate.Date)
                    {
                        UpdateSessionRow(sessionSheet, row);
                        sessionExists = true;
                        break;
                    }
                }
            }

            if (!sessionExists)
            {
                // Find the next available row (after row 6, considering headers)
                var nextRow = Math.Max(7, (sessionSheet.Dimension?.End.Row ?? 6) + 1);
                UpdateSessionRow(sessionSheet, nextRow);
            }
        }

        private void UpdateSessionRow(ExcelWorksheet sessionSheet, int row)
        {
            sessionSheet.Cells[row, 1].Value = _todaySession.SessionDate.ToString("yyyy-MM-dd");
            sessionSheet.Cells[row, 2].Value = _todaySession.TotalFocusTime.ToString();
            sessionSheet.Cells[row, 3].Value = _todaySession.TotalBreakTime.ToString();
            sessionSheet.Cells[row, 4].Value = _todaySession.CompletedFocusSessions;
            sessionSheet.Cells[row, 5].Value = _todaySession.CompletedBreakSessions;
            sessionSheet.Cells[row, 6].Value = _todaySession.FocusMinutes;
            sessionSheet.Cells[row, 7].Value = _todaySession.BreakMinutes;
            sessionSheet.Cells[row, 8].Value = _todaySession.DayStartTime?.ToString("yyyy-MM-dd HH:mm:ss");
            sessionSheet.Cells[row, 9].Value = _todaySession.DayEndTime?.ToString("yyyy-MM-dd HH:mm:ss");
            sessionSheet.Cells[row, 10].Value = _todaySession.IsWorkDayActive;
        }

        private async Task SaveWorkDaysAsync(ExcelPackage package)
        {
            var workDaySheet = package.Workbook.Worksheets["WorkDays"];

            // Clear existing data (from row 7 onwards)
            if (workDaySheet.Dimension != null && workDaySheet.Dimension.End.Row >= 7)
            {
                workDaySheet.Cells[7, 1, workDaySheet.Dimension.End.Row, 5].Clear();
            }

            // Write work day data starting from row 7
            for (int i = 0; i < _workDays.Count; i++)
            {
                var workDay = _workDays[i];
                var row = i + 7;
                workDaySheet.Cells[row, 1].Value = workDay.Id;
                workDaySheet.Cells[row, 2].Value = workDay.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
                workDaySheet.Cells[row, 3].Value = workDay.EndTime?.ToString("yyyy-MM-dd HH:mm:ss");
                workDaySheet.Cells[row, 4].Value = workDay.PlannedDuration.ToString();
                workDaySheet.Cells[row, 5].Value = workDay.IsActive;

                // Add subtle alternating row colors for better readability
                if (i % 2 == 0)
                {
                    workDaySheet.Cells[row, 1, row, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    workDaySheet.Cells[row, 1, row, 5].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(248, 248, 248));
                }
            }
        }

        private async Task SaveSessionLogsAsync(ExcelPackage package)
        {
            var logSheet = package.Workbook.Worksheets["SessionLogs"];

            // Clear existing data (from row 8 onwards, after the example row)
            if (logSheet.Dimension != null && logSheet.Dimension.End.Row >= 8)
            {
                logSheet.Cells[8, 1, logSheet.Dimension.End.Row, 6].Clear();
            }

            // Write session log data starting from row 8
            for (int i = 0; i < _sessionLogs.Count; i++)
            {
                var log = _sessionLogs[i];
                var row = i + 8;
                logSheet.Cells[row, 1].Value = log.Id;
                logSheet.Cells[row, 2].Value = log.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
                logSheet.Cells[row, 3].Value = log.EndTime?.ToString("yyyy-MM-dd HH:mm:ss");
                logSheet.Cells[row, 4].Value = log.Type.ToString();
                logSheet.Cells[row, 5].Value = log.TaskId;
                logSheet.Cells[row, 6].Value = log.Notes;

                // Color-code by session type
                switch (log.Type)
                {
                    case SessionType.Focus:
                        logSheet.Cells[row, 1, row, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        logSheet.Cells[row, 1, row, 6].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(230, 255, 230)); // Light green
                        break;

                    case SessionType.Break:
                        logSheet.Cells[row, 1, row, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        logSheet.Cells[row, 1, row, 6].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(230, 230, 255)); // Light blue
                        break;

                    case SessionType.Pause:
                        logSheet.Cells[row, 1, row, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        logSheet.Cells[row, 1, row, 6].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 255, 230)); // Light yellow
                        break;

                    case SessionType.Application:
                        logSheet.Cells[row, 1, row, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        logSheet.Cells[row, 1, row, 6].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 230, 230)); // Light red
                        break;

                    case SessionType.Command:
                        logSheet.Cells[row, 1, row, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        logSheet.Cells[row, 1, row, 6].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 240, 240)); // Light gray
                        break;
                }
            }
        }
    }
}