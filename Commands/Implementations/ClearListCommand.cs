using TaskManagerCLI.Repositories;
using TaskStatus = TaskManagerCLI.Models.TaskStatus;

namespace TaskManagerCLI.Commands.Implementations
{
    public class ClearListCommand : ICommand
    {
        private readonly ITaskRepository _repository;

        public ClearListCommand(ITaskRepository repository)
        {
            _repository = repository;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            var tasks = await _repository.GetAllTasksAsync();
            var activeTasks = tasks.Where(t => t.Status != TaskStatus.Deleted).ToList();

            if (!activeTasks.Any())
            {
                return "📋 No tasks to clear. Task list is already empty.";
            }

            foreach (var task in activeTasks)
            {
                await _repository.DeleteTaskAsync(task.Id);
            }

            return $"🧹 All tasks cleared! Deleted {activeTasks.Count} task(s).\n" +
                   $"💡 Start fresh with '!task <description>' to add new tasks.";
        }
    }
}