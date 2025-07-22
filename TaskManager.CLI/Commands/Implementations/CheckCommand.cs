using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;
using TaskManager.CLI.Utilities;

namespace TaskManager.CLI.Commands.Implementations
{
    public class CheckCommand : ICommand
    {
        private readonly ITaskRepository _repository;
        private readonly ConsoleHelper _console;

        public CheckCommand(ITaskRepository repository, ConsoleHelper console)
        {
            _repository = repository;
            _console = console;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            var tasks = await _repository.GetAllTasksAsync();
            var activeTasks = tasks.Where(t => t.Status != TaskStatus.Completed && t.Status != TaskStatus.Deleted).ToList();

            if (!activeTasks.Any())
            {
                return "📋 No active tasks found.\n💡 Add tasks with '!task <description>' to get started!";
            }

            // Assign aliases and update alias manager
            var orderedTasks = activeTasks.OrderBy(t => t.CreatedAt).ToList();
            TaskAliasManager.SetAliases(orderedTasks.Select(t => t.Id).ToList());

            // Prepare table data
            var headers = new[] { "Alias", "Description", "Status", "Focused", "Focus Time", "GUID" };
            var rows = new string[orderedTasks.Count][];
            for (int i = 0; i < orderedTasks.Count; i++)
            {
                var task = orderedTasks[i];
                string statusIcon = task.Status switch
                {
                    TaskStatus.InProgress => "🎯",
                    TaskStatus.Pending => "📝",
                    TaskStatus.Paused => "⏸️",
                    TaskStatus.OnBreak => "☕",
                    TaskStatus.Completed => "✅",
                    _ => ""
                };
                string focusedIcon = task.IsFocused ? "👁️" : "";
                rows[i] = new[]
                {
                    (i + 1).ToString(),
                    task.Description,
                    $"{statusIcon} {task.Status}",
                    focusedIcon,
                    task.FocusTime > TimeSpan.Zero ? task.FocusTime.ToString(@"hh\:mm\:ss") : "-",
                    task.Id.ToString()
                };
            }

            // Output table
            _console.WriteHeader($"📋 Your Tasks ({orderedTasks.Count} active)");
            _console.WriteTable(headers, rows);
            _console.WriteLine();
            _console.WriteInfo("💡 Use '!focus next' to start working or '!focus next <alias>' for a specific task");

            // Add summary by status
            var statusGroups = orderedTasks.GroupBy(t => t.Status)
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToList();
            _console.WriteLine();
            _console.WriteHighlight("Summary by Status:");
            _console.WriteLine(string.Join(" | ", statusGroups));

            return string.Empty;
        }
    }
}