using TaskManager.CLI.Repositories;
using TaskManager.CLI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;

namespace TaskManager.CLI.Services
{
    public class HtmlReportGenerator
    {
        private readonly ITaskRepository _repository;
        private readonly string _reportsPath;

        public HtmlReportGenerator(ITaskRepository repository)
        {
            _repository = repository;
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _reportsPath = Path.Combine(documentsPath, "TaskManager", "Reports");
            Directory.CreateDirectory(_reportsPath);
        }

        public async Task<string> GenerateReportAsync()
        {
            var tasks = await _repository.GetAllTasksAsync();
            var sessionLogs = await _repository.GetTodaySessionLogsAsync();
            var workDays = await _repository.GetTodayWorkDayAsync();

            var reportData = await AnalyzeDataAsync(tasks, sessionLogs, workDays);
            var htmlContent = GenerateHtmlContent(reportData);
            
            var fileName = $"TaskManager_Report_{DateTime.UtcNow:yyyy-MM-dd_HHmmss}.html";
            var filePath = Path.Combine(_reportsPath, fileName);
            
            await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);
            return filePath;
        }

        private async Task<ReportData> AnalyzeDataAsync(List<TaskModel> tasks, List<SessionLog> sessionLogs, WorkDay? workDay)
        {
            var reportData = new ReportData();

            // Task Analysis
            reportData.TotalTasks = tasks.Count(t => t.Status != TaskStatus.Deleted);
            reportData.CompletedTasks = tasks.Count(t => t.Status == TaskStatus.Completed);
            reportData.PendingTasks = tasks.Count(t => t.Status == TaskStatus.Pending);
            reportData.InProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress);
            reportData.PausedTasks = tasks.Count(t => t.Status == TaskStatus.Paused);
            reportData.CompletionRate = reportData.TotalTasks > 0 ? (double)reportData.CompletedTasks / reportData.TotalTasks * 100 : 0;

            // Session Analysis
            var focusSessions = sessionLogs.Where(s => s.Type == SessionType.Focus).ToList();
            var breakSessions = sessionLogs.Where(s => s.Type == SessionType.Break).ToList();
            var commandSessions = sessionLogs.Where(s => s.Type == SessionType.Command).ToList();

            reportData.TotalFocusTime = focusSessions.Aggregate(TimeSpan.Zero, (total, s) => total + (s.EndTime.HasValue ? s.EndTime.Value - s.StartTime : TimeSpan.Zero));
            reportData.TotalBreakTime = breakSessions.Aggregate(TimeSpan.Zero, (total, s) => total + (s.EndTime.HasValue ? s.EndTime.Value - s.StartTime : TimeSpan.Zero));
            reportData.FocusSessionsCount = focusSessions.Count;
            reportData.BreakSessionsCount = breakSessions.Count;
            reportData.CommandsExecuted = commandSessions.Count;

            // Daily Statistics
            try
            {
                var todayStats = await _repository.GetDayStatisticsAsync(DateTime.UtcNow.Date);
                reportData.TodayProductivityScore = todayStats.ProductivityScore;
                reportData.TodayTasksCompleted = todayStats.TasksCompleted;
            }
            catch
            {
                reportData.TodayProductivityScore = 0;
                reportData.TodayTasksCompleted = 0;
            }

            // Task Trends (last 7 days)
            var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.UtcNow.Date.AddDays(-i)).ToList();
            reportData.DailyTaskCompletions = new Dictionary<string, int>();
            
            foreach (var date in last7Days)
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
            reportData.SessionTypeDistribution = sessionLogs
                .GroupBy(s => s.Type)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            // Most Active Hours
            reportData.HourlyActivity = sessionLogs
                .GroupBy(s => s.StartTime.Hour)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key.ToString("00"), g => g.Count());

            // Top Commands
            reportData.TopCommands = commandSessions
                .GroupBy(s => ExtractCommandName(s.Notes))
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());

            return reportData;
        }

        private string ExtractCommandName(string notes)
        {
            if (notes.StartsWith("Command executed: !"))
            {
                var command = notes.Substring("Command executed: !".Length);
                return command.Split(' ')[0];
            }
            return "Unknown";
        }

        private string GenerateHtmlContent(ReportData data)
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
            
            sb.AppendLine(GenerateHeader());
            sb.AppendLine(GenerateSummaryCards(data));
            sb.AppendLine(GenerateChartsSection(data));
            sb.AppendLine(GenerateFiltersSection());
            sb.AppendLine(GenerateDetailedTables(data));
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

        private string GenerateHeader()
        {
            return $@"
                <div class='container'>
                    <div class='header'>
                        <h1>📊 TaskManager CLI Report</h1>
                        <p>Comprehensive Productivity Analytics & Insights</p>
                        <p>Generated on {DateTime.UtcNow:MMMM dd, yyyy 'at' HH:mm UTC}</p>
                    </div>
                </div>
            ";
        }

        private string GenerateSummaryCards(ReportData data)
        {
            // Safe extraction of all values with null-coalescing
            int totalTasks = data?.TotalTasks ?? 0;
            int completedTasks = data?.CompletedTasks ?? 0;
            int focusSessions = data?.FocusSessionsCount ?? 0;
            int commandsExecuted = data?.CommandsExecuted ?? 0;
            double completionRate = data?.CompletionRate ?? 0;
            double todayProductivity = data?.TodayProductivityScore ?? 0;
            TimeSpan totalFocusTime = data?.TotalFocusTime ?? TimeSpan.Zero;

            return $@"
                <div class='container'>
                    <div class='summary-cards'>
                        <div class='card'>
                            <div class='card-icon'>📋</div>
                            <div class='card-value'>{totalTasks}</div>
                            <div class='card-label'>Total Tasks</div>
                            <div class='progress-bar'>
                                <div class='progress-fill' style='width: {completionRate}%'></div>
                            </div>
                            <div style='margin-top: 5px; font-size: 0.8em; color: #666;'>{completionRate:F1}% Complete</div>
                        </div>
                        
                        <div class='card'>
                            <div class='card-icon'>✅</div>
                            <div class='card-value'>{completedTasks}</div>
                            <div class='card-label'>Completed Tasks</div>
                        </div>
                        
                        <div class='card'>
                            <div class='card-icon'>🎯</div>
                            <div class='card-value'>{focusSessions}</div>
                            <div class='card-label'>Focus Sessions</div>
                        </div>
                        
                        <div class='card'>
                            <div class='card-icon'>⏱️</div>
                            <div class='card-value'>{totalFocusTime.TotalHours:F1}h</div>
                            <div class='card-label'>Total Focus Time</div>
                        </div>
                        
                        <div class='card'>
                            <div class='card-icon'>📈</div>
                            <div class='card-value'>{todayProductivity:P0}</div>
                            <div class='card-label'>Today's Productivity</div>
                        </div>
                        
                        <div class='card'>
                            <div class='card-icon'>⚡</div>
                            <div class='card-value'>{commandsExecuted}</div>
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
                                <h3>Daily Task Completions (Last 7 Days)</h3>
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

        private string GenerateFiltersSection()
        {
            return @"
                <div class='container'>
                    <div class='filters-section'>
                        <h2>🔍 Data Filters</h2>
                        <div class='filter-controls'>
                            <select id='dateFilter'>
                                <option value='all'>All Time</option>
                                <option value='today'>Today</option>
                                <option value='week'>This Week</option>
                                <option value='month'>This Month</option>
                            </select>
                            
                            <select id='statusFilter'>
                                <option value='all'>All Statuses</option>
                                <option value='completed'>Completed</option>
                                <option value='pending'>Pending</option>
                                <option value='inprogress'>In Progress</option>
                                <option value='paused'>Paused</option>
                            </select>
                            
                            <input type='text' id='searchFilter' placeholder='Search tasks...'>
                            
                            <button onclick='applyFilters()'>Apply Filters</button>
                            <button onclick='resetFilters()'>Reset</button>
                        </div>
                    </div>
                </div>
            ";
        }

        private string GenerateDetailedTables(ReportData data)
        {
            // Safe extraction of all values with null-coalescing
            int totalTasks = data?.TotalTasks ?? 0;
            int completedTasks = data?.CompletedTasks ?? 0;
            int pendingTasks = data?.PendingTasks ?? 0;
            int inProgressTasks = data?.InProgressTasks ?? 0;
            int pausedTasks = data?.PausedTasks ?? 0;

            // Safe calculation of percentages
            double completionRate = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0;
            double pendingRate = totalTasks > 0 ? (double)pendingTasks / totalTasks * 100 : 0;
            double inProgressRate = totalTasks > 0 ? (double)inProgressTasks / totalTasks * 100 : 0;
            double pausedRate = totalTasks > 0 ? (double)pausedTasks / totalTasks * 100 : 0;

            // Safe extraction of session data
            int focusSessions = data?.FocusSessionsCount ?? 0;
            int breakSessions = data?.BreakSessionsCount ?? 0;
            int commandsExecuted = data?.CommandsExecuted ?? 0;
            TimeSpan totalFocusTime = data?.TotalFocusTime ?? TimeSpan.Zero;
            TimeSpan totalBreakTime = data?.TotalBreakTime ?? TimeSpan.Zero;

            // Safe calculation of averages
            double avgFocus = focusSessions > 0 ? totalFocusTime.TotalMinutes / focusSessions : 0;
            double avgBreak = breakSessions > 0 ? totalBreakTime.TotalMinutes / breakSessions : 0;

            // Format TimeSpan safely
            string focusTimeStr = totalFocusTime.ToString(@"hh\:mm\:ss");
            string breakTimeStr = totalBreakTime.ToString(@"hh\:mm\:ss");

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
                                        <td>{totalTasks}</td>
                                        <td>100%</td>
                                    </tr>
                                    <tr>
                                        <td>Completed</td>
                                        <td>{completedTasks}</td>
                                        <td>{completionRate:F1}%</td>
                                    </tr>
                                    <tr>
                                        <td>Pending</td>
                                        <td>{pendingTasks}</td>
                                        <td>{pendingRate:F1}%</td>
                                    </tr>
                                    <tr>
                                        <td>In Progress</td>
                                        <td>{inProgressTasks}</td>
                                        <td>{inProgressRate:F1}%</td>
                                    </tr>
                                    <tr>
                                        <td>Paused</td>
                                        <td>{pausedTasks}</td>
                                        <td>{pausedRate:F1}%</td>
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
                                        <td>{focusSessions}</td>
                                        <td>{focusTimeStr}</td>
                                        <td>{avgFocus:F1} min</td>
                                    </tr>
                                    <tr>
                                        <td>Break Sessions</td>
                                        <td>{breakSessions}</td>
                                        <td>{breakTimeStr}</td>
                                        <td>{avgBreak:F1} min</td>
                                    </tr>
                                    <tr>
                                        <td>Commands Executed</td>
                                        <td>{commandsExecuted}</td>
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
            // Safe extraction of all values with null-coalescing
            int completedTasks = data?.CompletedTasks ?? 0;
            int pendingTasks = data?.PendingTasks ?? 0;
            int inProgressTasks = data?.InProgressTasks ?? 0;
            int pausedTasks = data?.PausedTasks ?? 0;
            TimeSpan totalFocusTime = data?.TotalFocusTime ?? TimeSpan.Zero;
            TimeSpan totalBreakTime = data?.TotalBreakTime ?? TimeSpan.Zero;

            // Safe generation of chart data
            var dailyCompletionsLabels = data?.DailyTaskCompletions?.Any() == true 
                ? string.Join(",", data.DailyTaskCompletions.Keys.Select(k => $"'{k}'")) 
                : "'No Data'";
            var dailyCompletionsValues = data?.DailyTaskCompletions?.Any() == true 
                ? string.Join(",", data.DailyTaskCompletions.Values) 
                : "0";
            
            var sessionTypeLabels = data?.SessionTypeDistribution?.Any() == true 
                ? string.Join(",", data.SessionTypeDistribution.Keys.Select(k => $"'{k}'")) 
                : "'No Data'";
            var sessionTypeValues = data?.SessionTypeDistribution?.Any() == true 
                ? string.Join(",", data.SessionTypeDistribution.Values) 
                : "0";
            
            var hourlyLabels = data?.HourlyActivity?.Any() == true 
                ? string.Join(",", data.HourlyActivity.Keys.Select(k => $"'{k}:00'")) 
                : "'00:00'";
            var hourlyValues = data?.HourlyActivity?.Any() == true 
                ? string.Join(",", data.HourlyActivity.Values) 
                : "0";
            
            var topCommandsLabels = data?.TopCommands?.Any() == true 
                ? string.Join(",", data.TopCommands.Keys.Select(k => $"'{k}'")) 
                : "'No Commands'";
            var topCommandsValues = data?.TopCommands?.Any() == true 
                ? string.Join(",", data.TopCommands.Values) 
                : "0";

            return $@"
                // Task Status Chart
                new Chart(document.getElementById('taskStatusChart'), {{
                    type: 'doughnut',
                    data: {{
                        labels: ['Completed', 'Pending', 'In Progress', 'Paused'],
                        datasets: [{{
                            data: [{completedTasks}, {pendingTasks}, {inProgressTasks}, {pausedTasks}],
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

                // Top Commands Chart
                new Chart(document.getElementById('topCommandsChart'), {{
                    type: 'horizontalBar',
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

                // Focus vs Break Chart
                new Chart(document.getElementById('focusBreakChart'), {{
                    type: 'bar',
                    data: {{
                        labels: ['Focus Time', 'Break Time'],
                        datasets: [{{
                            label: 'Hours',
                            data: [{totalFocusTime.TotalHours:F2}, {totalBreakTime.TotalHours:F2}],
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

                function applyFilters() {{
                    const dateFilter = document.getElementById('dateFilter').value;
                    const statusFilter = document.getElementById('statusFilter').value;
                    const searchFilter = document.getElementById('searchFilter').value;
                    
                    // Apply filters to tables
                    filterTable('taskSummaryTable', statusFilter, searchFilter);
                    filterTable('sessionStatsTable', statusFilter, searchFilter);
                    
                    console.log('Filters applied:', {{ dateFilter, statusFilter, searchFilter }});
                }}

                function resetFilters() {{
                    document.getElementById('dateFilter').value = 'all';
                    document.getElementById('statusFilter').value = 'all';
                    document.getElementById('searchFilter').value = '';
                    
                    // Reset table visibility
                    const tables = document.querySelectorAll('table');
                    tables.forEach(table => {{
                        const rows = table.querySelectorAll('tbody tr');
                        rows.forEach(row => row.style.display = '');
                    }});
                }}

                function filterTable(tableId, statusFilter, searchFilter) {{
                    const table = document.getElementById(tableId);
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
    }
} 