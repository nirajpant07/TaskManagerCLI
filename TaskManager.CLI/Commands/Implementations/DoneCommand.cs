using TaskManager.CLI.Repositories;
using TaskManager.CLI.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaskManager.CLI.Commands.Implementations
{
    public class DoneCommand : ICommand
    {
        private readonly ITaskRepository _repository;
        private readonly IFocusSessionManagerService _sessionManager;

        public DoneCommand(ITaskRepository repository, IFocusSessionManagerService sessionManager)
        {
            _repository = repository;
            _sessionManager = sessionManager;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            if (parameters.Length == 0)
            {
                return "❌ Please provide task ID(s).\n💡 Usage: !done <task_id|alias> or !done <id1|alias1>, <id2|alias2>, ...";
            }

            var taskIdsStr = string.Join(" ", parameters).Replace(",", " ");
            var taskIdParts = taskIdsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var results = new List<string>();
            var completedCount = 0;

            foreach (var idStr in taskIdParts)
            {
                Guid? taskId = null;
                if (int.TryParse(idStr, out int alias))
                {
                    var guid = TaskManager.CLI.Utilities.TaskAliasManager.GetGuidByAlias(alias);
                    if (guid.HasValue)
                        taskId = guid.Value;
                }
                else if (Guid.TryParse(idStr, out Guid parsedGuid))
                {
                    taskId = parsedGuid;
                }

                if (!taskId.HasValue)
                {
                    results.Add($"❌ Invalid task ID or alias: {idStr}");
                    continue;
                }

                var task = await _repository.GetTaskByIdAsync(taskId.Value);
                if (task == null)
                {
                    results.Add($"❌ Task {idStr} not found");
                    continue;
                }

                if (task.Status == Models.TaskStatus.Completed)
                {
                    results.Add($"⚠️ Task {idStr} already completed");
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

                results.Add($"✅ Task {idStr} completed: {task.Description}");
                completedCount++;
            }

            var summary = completedCount > 0
                ? $"🎉 Completed {completedCount} task(s)!\n{string.Join("\n", results)}"
                : string.Join("\n", results);

            return summary;
        }
    }
}