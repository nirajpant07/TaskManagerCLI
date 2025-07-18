using TaskManager.CLI.Repositories;
using TaskManager.CLI.Services;

namespace TaskManager.CLI.Commands.Implementations
{
    public class PauseCommand : ICommand
    {
        private readonly ITaskRepository _repository;
        private readonly IFocusSessionManagerService _sessionManager;

        public PauseCommand(ITaskRepository repository, IFocusSessionManagerService sessionManager)
        {
            _repository = repository;
            _sessionManager = sessionManager;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            var tasks = await _repository.GetAllTasksAsync();
            var currentTask = tasks.FirstOrDefault(t => t.IsFocused);

            if (currentTask == null)
            {
                return "⚠️ No task is currently focused. Use '!focus next' to start working on a task.";
            }

            var reason = parameters.Length > 0 ? string.Join(" ", parameters) : "Manual pause";

            // Pause the current session
            await _sessionManager.PauseCurrentSessionAsync(reason);

            currentTask.Status = Models.TaskStatus.Paused;
            currentTask.IsFocused = false;
            currentTask.PausedAt = DateTime.UtcNow;
            currentTask.PauseReason = reason;
            await _repository.UpdateTaskAsync(currentTask);

            return $"⏸️ Task {currentTask.Id} paused: {currentTask.Description}\n" +
                   $"📝 Reason: {reason}\n" +
                   $"🔄 Use '!focus next {currentTask.Id}' to resume this task later.";
        }
    }
}