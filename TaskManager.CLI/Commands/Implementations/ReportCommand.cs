using TaskManager.CLI.Repositories;
using TaskManager.CLI.Services;
using TaskManager.CLI.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;

namespace TaskManager.CLI.Commands.Implementations
{
    public class ReportCommand : ICommand
    {
        private readonly ITaskRepository _repository;
        private readonly ConsoleHelper _console;

        public ReportCommand(ITaskRepository repository, ConsoleHelper console)
        {
            _repository = repository;
            _console = console;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            try
            {
                DateTime? startDate = null;
                DateTime? endDate = null;

                // Parse date parameters if provided
                if (parameters.Length >= 2)
                {
                    if (DateTime.TryParse(parameters[0], out var start) && DateTime.TryParse(parameters[1], out var end))
                    {
                        startDate = start;
                        endDate = end;
                    }
                    else
                    {
                        return "❌ Invalid date format. Usage: !report [start_date] [end_date]\n" +
                               "💡 Example: !report 2024-01-01 2024-01-31\n" +
                               "💡 Or just: !report (for last 30 days)";
                    }
                }
                else if (parameters.Length == 1)
                {
                    // Single date parameter - use as end date, start date is 30 days before
                    if (DateTime.TryParse(parameters[0], out var end))
                    {
                        endDate = end;
                        startDate = end.AddDays(-30);
                    }
                    else
                    {
                        return "❌ Invalid date format. Usage: !report [start_date] [end_date]\n" +
                               "💡 Example: !report 2024-01-01 2024-01-31";
                    }
                }

                var reportGenerator = new HtmlReportGenerator(_repository);
                var reportPath = await reportGenerator.GenerateReportAsync(startDate, endDate);
                
                var startStr = startDate?.ToString("yyyy-MM-dd") ?? "30 days ago";
                var endStr = endDate?.ToString("yyyy-MM-dd") ?? "today";
                
                return $"📊 HTML Report Generated Successfully!\n\n" +
                       $"📁 Location: {reportPath}\n" +
                       $"📅 Date Range: {startStr} to {endStr}\n" +
                       $"🌐 Open in browser to view detailed analytics\n\n" +
                       $"📈 Report includes:\n" +
                       $"   • Task completion trends\n" +
                       $"   • Focus session analytics\n" +
                       $"   • Productivity metrics\n" +
                       $"   • Interactive charts and filters\n" +
                       $"   • Daily/weekly summaries\n" +
                       $"   • User & system information\n" +
                       $"   • Work day statistics\n" +
                       $"   • Archived data analysis";
            }
            catch (Exception ex)
            {
                return $"❌ Failed to generate report: {ex.Message}";
            }
        }
    }
} 