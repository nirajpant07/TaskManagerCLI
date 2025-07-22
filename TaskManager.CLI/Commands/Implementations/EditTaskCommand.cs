using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.CLI.Commands.Implementations
{
    public class EditTaskCommand : ICommand
    {
        private readonly ITaskRepository _repository;

        public EditTaskCommand(ITaskRepository repository)
        {
            _repository = repository;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            if (parameters.Length < 2)
            {
                return "❌ Please provide task ID and new description.\n💡 Usage: !edit <task_id|alias> <new description>";
            }

            Guid? taskId = null;
            if (int.TryParse(parameters[0], out int alias))
            {
                var guid = TaskManager.CLI.Utilities.TaskAliasManager.GetGuidByAlias(alias);
                if (guid.HasValue)
                    taskId = guid.Value;
            }
            else if (Guid.TryParse(parameters[0], out Guid parsedGuid))
            {
                taskId = parsedGuid;
            }

            if (!taskId.HasValue)
            {
                return "❌ Invalid task ID or alias. Please provide a valid alias or GUID.";
            }

            var task = await _repository.GetTaskByIdAsync(taskId.Value);
            if (task == null)
            {
                return $"❌ Task {taskId} not found.";
            }

            if (task.Status == Models.TaskStatus.Deleted)
            {
                return $"❌ Cannot edit deleted task {taskId}.";
            }

            var newDescription = string.Join(" ", parameters[1..]);
            if (newDescription.Length > 200)
            {
                return "❌ Task description too long (max 200 characters).";
            }

            var oldDescription = task.Description;
            task.Description = newDescription;
            await _repository.UpdateTaskAsync(task);

            return $"✏️ Task {taskId} updated:\n" +
                   $"   Old: {oldDescription}\n" +
                   $"   New: {newDescription}";
        }
    }
}