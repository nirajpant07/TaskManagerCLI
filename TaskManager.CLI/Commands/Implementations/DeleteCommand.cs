using TaskManager.CLI.Commands;
using TaskManager.CLI.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaskManager.CLI.Commands.Implementations
{
    public class DeleteCommand : ICommand
    {
        private readonly ITaskRepository _repository;

        public DeleteCommand(ITaskRepository repository)
        {
            _repository = repository;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            if (parameters.Length == 0)
            {
                return "❌ Please provide task ID(s).\n💡 Usage: !delete <task_id> or !delete <id1>, <id2>, <id3>";
            }

            var taskIdsStr = string.Join(" ", parameters).Replace(",", " ");
            var taskIdParts = taskIdsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var results = new List<string>();
            var deletedCount = 0;

            foreach (var idStr in taskIdParts)
            {
                if (!Guid.TryParse(idStr, out Guid taskId))
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

                await _repository.DeleteTaskAsync(taskId);
                results.Add($"🗑️ Task {taskId} deleted: {task.Description}");
                deletedCount++;
            }

            var summary = deletedCount > 0
                ? $"🗑️ Deleted {deletedCount} task(s):\n{string.Join("\n", results)}"
                : string.Join("\n", results);

            return summary;
        }
    }
}