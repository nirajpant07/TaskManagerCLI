using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using TaskManager.CLI.Services;
using System.Linq;
using System.Threading.Tasks;
using TaskStatus = TaskManager.CLI.Models.TaskStatus;
using TaskManager.CLI.Utilities;

namespace TaskManager.CLI.Commands.Implementations
{
    public class FocusCommand : ICommand
    {
        private readonly ITaskRepository _repository;
        private readonly IFocusSessionManagerService _sessionManager;

        public FocusCommand(ITaskRepository repository, IFocusSessionManagerService sessionManager)
        {
            _repository = repository;
            _sessionManager = sessionManager;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            var tasks = await _repository.GetAllTasksAsync();
            var currentTask = tasks.FirstOrDefault(t => t.IsFocused);

            if (parameters.Length > 0 && parameters[0].ToLower() == "next")
            {
                return await HandleFocusNextAsync(parameters, tasks, currentTask);
            }
            else
            {
                return await HandleShowCurrentFocusAsync(currentTask);
            }
        }

        private async Task<string> HandleFocusNextAsync(string[] parameters, List<TaskModel> tasks, TaskModel? currentTask)
        {
            TaskModel? targetTask = null;

            if (parameters.Length > 1)
            {
                // Try alias first
                if (int.TryParse(parameters[1], out int alias))
                {
                    var guid = TaskAliasManager.GetGuidByAlias(alias);
                    if (guid.HasValue)
                    {
                        targetTask = await _repository.GetTaskByIdAsync(guid.Value);
                        if (targetTask == null)
                        {
                            return $"❌ Task with alias {alias} not found.";
                        }
                    }
                    else
                    {
                        return $"❌ Alias {alias} does not correspond to any task. Use !check to see available aliases.";
                    }
                }
                // Try GUID
                else if (Guid.TryParse(parameters[1], out Guid taskId))
                {
                    targetTask = await _repository.GetTaskByIdAsync(taskId);
                    if (targetTask == null)
                    {
                        return $"❌ Task {taskId} not found.";
                    }
                }
                else
                {
                    return $"❌ '{parameters[1]}' is not a valid alias or GUID. Use !check to see available aliases.";
                }

                if (targetTask.Status == TaskStatus.Completed)
                {
                    return $"⚠️ Task {parameters[1]} is already completed. Use !done to mark tasks complete.";
                }

                if (targetTask.Status == TaskStatus.Deleted)
                {
                    return $"❌ Task {parameters[1]} has been deleted.";
                }
            }
            else
            {
                // Focus on next available task
                targetTask = tasks.Where(t => t.Status == TaskStatus.Pending || t.Status == TaskStatus.Paused)
                                 .OrderBy(t => t.CreatedAt)
                                 .FirstOrDefault();

                if (targetTask == null)
                {
                    return "🎉 No pending tasks available to focus on! Add new tasks with !task or check completed ones with !check.";
                }
            }

            // Clear current focus if different task
            if (currentTask != null && currentTask.Id != targetTask.Id)
            {
                await _sessionManager.EndCurrentSessionAsync();
                currentTask.IsFocused = false;
                if (currentTask.Status == TaskStatus.InProgress)
                {
                    currentTask.Status = TaskStatus.Pending;
                }
                await _repository.UpdateTaskAsync(currentTask);
            }

            // Set new focus
            targetTask.IsFocused = true;
            targetTask.Status = TaskStatus.InProgress;
            if (targetTask.PausedAt.HasValue)
            {
                targetTask.PausedAt = null;
                targetTask.PauseReason = string.Empty;
            }
            await _repository.UpdateTaskAsync(targetTask);

            // Start focus session
            await _sessionManager.StartFocusSessionAsync(targetTask);

            var session = await _repository.GetTodaySessionAsync();
            return $"🎯 Now focusing on task {targetTask.Id}: {targetTask.Description}\n" +
                   $"⏱️ Focus session: {session.FocusMinutes} minutes | Timer will notify when complete";
        }

        private async Task<string> HandleShowCurrentFocusAsync(TaskModel? currentTask)
        {
            if (currentTask != null)
            {
                var statusIcon = currentTask.Status switch
                {
                    TaskStatus.InProgress => "🎯",
                    TaskStatus.Paused => "⏸️",
                    TaskStatus.OnBreak => "☕",
                    _ => "📝"
                };

                var timeInfo = currentTask.FocusTime > TimeSpan.Zero
                    ? $" | ⏱️ Total focus time: {currentTask.FocusTime:hh\\:mm\\:ss}"
                    : "";

                return $"{statusIcon} Currently focused on task {currentTask.Id}: {currentTask.Description}{timeInfo}";
            }
            else
            {
                var tasks = await _repository.GetAllTasksAsync();
                var pendingCount = tasks.Count(t => t.Status == TaskStatus.Pending);

                if (pendingCount > 0)
                {
                    return $"💡 No task is currently focused. You have {pendingCount} pending tasks.\n" +
                           "🚀 Use '!focus next' to start focusing on the next task, or '!focus next <id>' for a specific task.";
                }
                else
                {
                    return "🎉 No tasks to focus on! Add new tasks with '!task <description>' to get started.";
                }
            }
        }
    }
}