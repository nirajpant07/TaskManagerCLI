using TaskManager.CLI.Repositories;
using TaskManager.CLI.Services;

namespace TaskManager.CLI.Commands.Implementations
{
    public class DoneCommand : ICommand
    {
        private readonly ITaskRepository _repository;
        private readonly FocusSessionManagerService _sessionManager;

        public DoneCommand(ITaskRepository repository, FocusSessionManagerService sessionManager)
        {
            _repository = repository;
            _sessionManager = sessionManager;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            if (parameters.Length == 0)
            {
                return "❌ Please provide task ID(s).\n💡 Usage: !done <task_id> or !done <id1>, <id2>, <id3>";
            }

            var taskIdsStr = string.Join(" ", parameters).Replace(",", " ");
            var taskIdParts = taskIdsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var results = new List<string>();
            var completedCount = 0;

            foreach (var idStr in taskIdParts)
            {
                if (!int.TryParse(idStr, out int taskId))
                {
                    results.Add($"❌ Invalid task ID: {idStr}");
                    continue;
                }

                var task = await _repository.GetTaskByIdAsync(taskId);
                if (task == null)
                {
                    results.Add($"❌ Task {taskId} not found");
                    continue;
                }

                if (task.Status == Models.TaskStatus.Completed)
                {
                    results.Add($"⚠️ Task {taskId} already completed");
                    continue;
                }

                // If this task is currently focused, end the session
                if (task.IsFocused)
                {
                    await _sessionManager.EndCurrentSessionAsync();
                    task.IsFocused = false;
                }

                task.Status = Models.TaskStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                await _repository.UpdateTaskAsync(task);

                results.Add($"✅ Task {taskId} completed: {task.Description}");
                completedCount++;
            }

            var summary = completedCount > 0
                ? $"🎉 Completed {completedCount} task(s)!\n{string.Join("\n", results)}"
                : string.Join("\n", results);

            return summary;
        }
    }
}