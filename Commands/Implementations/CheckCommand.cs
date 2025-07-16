using TaskManagerCLI.Models;
using TaskManagerCLI.Repositories;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskStatus = TaskManagerCLI.Models.TaskStatus;
using TaskManagerCLI.Utilities;

namespace TaskManagerCLI.Commands.Implementations
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

            var sb = new StringBuilder();
            sb.AppendLine($"📋 Your Tasks ({activeTasks.Count} active):\n");

            var pendingTasks = activeTasks.Where(t => t.Status == TaskStatus.Pending).ToList();
            var inProgressTasks = activeTasks.Where(t => t.Status == TaskStatus.InProgress).ToList();
            var pausedTasks = activeTasks.Where(t => t.Status == TaskStatus.Paused).ToList();
            var onBreakTasks = activeTasks.Where(t => t.Status == TaskStatus.OnBreak).ToList();

            if (inProgressTasks.Any())
            {
                sb.AppendLine("🎯 In Progress:");
                foreach (var task in inProgressTasks.OrderBy(t => t.Id))
                {
                    var focusIcon = task.IsFocused ? "👁️ " : "";
                    var timeInfo = task.FocusTime > TimeSpan.Zero ? $" [{task.FocusTime:hh\\:mm\\:ss}]" : "";
                    sb.AppendLine($"   {focusIcon}{task.Id}. {task.Description}{timeInfo}");
                }
                sb.AppendLine();
            }

            if (onBreakTasks.Any())
            {
                sb.AppendLine("☕ On Break:");
                foreach (var task in onBreakTasks.OrderBy(t => t.Id))
                {
                    var timeInfo = task.FocusTime > TimeSpan.Zero ? $" [{task.FocusTime:hh\\:mm\\:ss}]" : "";
                    sb.AppendLine($"   {task.Id}. {task.Description}{timeInfo}");
                }
                sb.AppendLine();
            }

            if (pausedTasks.Any())
            {
                sb.AppendLine("⏸️ Paused:");
                foreach (var task in pausedTasks.OrderBy(t => t.Id))
                {
                    var timeInfo = task.FocusTime > TimeSpan.Zero ? $" [{task.FocusTime:hh\\:mm\\:ss}]" : "";
                    var pauseInfo = !string.IsNullOrEmpty(task.PauseReason) ? $" - {task.PauseReason}" : "";
                    sb.AppendLine($"   {task.Id}. {task.Description}{timeInfo}{pauseInfo}");
                }
                sb.AppendLine();
            }

            if (pendingTasks.Any())
            {
                sb.AppendLine("📝 Pending:");
                foreach (var task in pendingTasks.OrderBy(t => t.Id))
                {
                    sb.AppendLine($"   {task.Id}. {task.Description}");
                }
                sb.AppendLine();
            }

            sb.AppendLine($"💡 Use '!focus next' to start working or '!focus next <id>' for specific task");

            return sb.ToString().TrimEnd();
        }
    }
}