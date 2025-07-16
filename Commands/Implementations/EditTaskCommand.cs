using TaskManagerCLI.Models;
using TaskManagerCLI.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManagerCLI.Commands.Implementations
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
                return "❌ Please provide task ID and new description.\n💡 Usage: !edit <task_id> <new description>";
            }

            if (!int.TryParse(parameters[0], out int taskId))
            {
                return "❌ Invalid task ID. Please provide a valid number.";
            }

            var task = await _repository.GetTaskByIdAsync(taskId);
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