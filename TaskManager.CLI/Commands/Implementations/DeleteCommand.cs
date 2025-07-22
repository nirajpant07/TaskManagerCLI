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
                return "❌ Please provide task ID(s).\n💡 Usage: !delete <task_id|alias> or !delete <id1|alias1>, <id2|alias2>, ...";
            }

            var taskIdsStr = string.Join(" ", parameters).Replace(",", " ");
            var taskIdParts = taskIdsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var results = new List<string>();
            var deletedCount = 0;

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

                await _repository.DeleteTaskAsync(taskId.Value);
                results.Add($"🗑️ Task {idStr} deleted: {task.Description}");
                deletedCount++;
            }

            var summary = deletedCount > 0
                ? $"🗑️ Deleted {deletedCount} task(s):\n{string.Join("\n", results)}"
                : string.Join("\n", results);

            return summary;
        }
    }
}