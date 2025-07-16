using TaskManagerCLI.Repositories;
using TaskStatus = TaskManagerCLI.Models.TaskStatus;

namespace TaskManagerCLI.Commands.Implementations
{
    public class ClearDoneCommand : ICommand
    {
        private readonly ITaskRepository _repository;

        public ClearDoneCommand(ITaskRepository repository)
        {
            _repository = repository;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            var tasks = await _repository.GetAllTasksAsync();
            var completedTasks = tasks.Where(t => t.Status == TaskStatus.Completed).ToList();

            if (!completedTasks.Any())
            {
                return "📋 No completed tasks to clear.";
            }

            foreach (var task in completedTasks)
            {
                await _repository.DeleteTaskAsync(task.Id);
            }

            return $"🧹 Completed tasks cleared! Removed {completedTasks.Count} completed task(s).\n" +
                   $"📝 Active tasks remain in your list.";
        }
    }
}