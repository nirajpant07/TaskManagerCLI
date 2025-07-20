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
                var reportGenerator = new HtmlReportGenerator(_repository);
                var reportPath = await reportGenerator.GenerateReportAsync();
                
                return $"📊 HTML Report Generated Successfully!\n\n" +
                       $"📁 Location: {reportPath}\n" +
                       $"🌐 Open in browser to view detailed analytics\n\n" +
                       $"📈 Report includes:\n" +
                       $"   • Task completion trends\n" +
                       $"   • Focus session analytics\n" +
                       $"   • Productivity metrics\n" +
                       $"   • Interactive charts and filters\n" +
                       $"   • Daily/weekly summaries";
            }
            catch (Exception ex)
            {
                return $"❌ Failed to generate report: {ex.Message}";
            }
        }
    }
} 