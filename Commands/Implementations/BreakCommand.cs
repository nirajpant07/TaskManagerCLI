using TaskManagerCLI.Models;
using TaskManagerCLI.Repositories;
using TaskManagerCLI.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManagerCLI.Commands.Implementations
{
    public class BreakCommand : ICommand
    {
        private readonly ITaskRepository _repository;
        private readonly FocusSessionManagerService _sessionManager;
        private readonly INotificationService _notificationService;
        private readonly ISoundService _soundService;

        public BreakCommand(ITaskRepository repository, FocusSessionManagerService sessionManager,
                           INotificationService notificationService, ISoundService soundService)
        {
            _repository = repository;
            _sessionManager = sessionManager;
            _notificationService = notificationService;
            _soundService = soundService;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            var tasks = await _repository.GetAllTasksAsync();
            var currentTask = tasks.FirstOrDefault(t => t.IsFocused);

            string previousTaskInfo = "";

            if (currentTask != null)
            {
                // End current focus session
                await _sessionManager.EndCurrentSessionAsync();

                // Set task status to on break but keep it accessible
                currentTask.Status = Models.TaskStatus.OnBreak;
                currentTask.IsFocused = false;
                await _repository.UpdateTaskAsync(currentTask);

                previousTaskInfo = $"Task {currentTask.Id} ({currentTask.Description}) set to break status.";
            }

            // Start break session
            await _sessionManager.StartBreakSessionAsync();

            var session = await _repository.GetTodaySessionAsync();
            var breakDuration = TimeSpan.FromMinutes(session.BreakMinutes);

            var result = $"☕ Break session started for {session.BreakMinutes} minutes.\n" +
                        $"🔔 Timer will notify when break is complete.";

            if (!string.IsNullOrEmpty(previousTaskInfo))
            {
                result += $"\n📝 {previousTaskInfo}";
            }

            // Provide helpful break suggestions
            result += $"\n\n💡 Break suggestions:\n" +
                     "   • Stand up and stretch\n" +
                     "   • Get some water or tea\n" +
                     "   • Take a short walk\n" +
                     "   • Rest your eyes from the screen";

            return result;
        }
    }
}