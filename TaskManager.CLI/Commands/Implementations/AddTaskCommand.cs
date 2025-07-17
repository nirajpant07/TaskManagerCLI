using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.CLI.Commands.Implementations
{
    public class AddTaskCommand : ICommand
    {
        private readonly ITaskRepository _repository;

        public AddTaskCommand(ITaskRepository repository)
        {
            _repository = repository;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            if (parameters.Length == 0)
            {
                return "❌ Please provide a task description.\n💡 Usage: !task <description> or !task <task1>, <task2>, <task3>";
            }

            var description = string.Join(" ", parameters);
            var tasks = description.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(t => t.Trim())
                                  .Where(t => !string.IsNullOrEmpty(t))
                                  .ToArray();

            if (tasks.Length == 0)
            {
                return "❌ Please provide valid task descriptions.";
            }

            var results = new List<string>();
            foreach (var taskDesc in tasks)
            {
                if (taskDesc.Length > 200)
                {
                    results.Add($"⚠️ Task description too long (max 200 chars): {taskDesc[..50]}...");
                    continue;
                }

                var task = new TaskModel
                {
                    Description = taskDesc,
                    Status = Models.TaskStatus.Pending
                };

                var id = await _repository.AddTaskAsync(task);
                results.Add($"✅ Task {id} added: {taskDesc}");
            }

            var summary = tasks.Length == 1 ?
                results[0] :
                $"📝 Added {tasks.Length} tasks:\n{string.Join("\n", results)}";

            return summary;
        }
    }
}