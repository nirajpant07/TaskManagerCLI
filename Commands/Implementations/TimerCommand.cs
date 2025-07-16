using TaskManagerCLI.Repositories;
using TaskManagerCLI.Services;

namespace TaskManagerCLI.Commands.Implementations
{
    public class TimerCommand : ICommand
    {
        private readonly ITaskRepository _repository;
        private readonly TimerService _timerService;
        private readonly INotificationService _notificationService;
        private readonly ISoundService _soundService;

        public TimerCommand(ITaskRepository repository, TimerService timerService,
                           INotificationService notificationService, ISoundService soundService)
        {
            _repository = repository;
            _timerService = timerService;
            _notificationService = notificationService;
            _soundService = soundService;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            if (parameters.Length == 0)
            {
                var currentSettings = _timerService.GetCurrentSettings();
                return $"⏰ Current timer settings:\n" +
                       $"   🎯 Focus: {currentSettings.FocusMinutes} minutes\n" +
                       $"   ☕ Break: {currentSettings.BreakMinutes} minutes\n\n" +
                       $"💡 To change: !timer <focus_minutes>/<break_minutes> (e.g., !timer 25/5)";
            }

            var timerInput = string.Join(" ", parameters);
            var parts = timerInput.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                return "❌ Invalid timer format.\n💡 Usage: !timer <focus_minutes>/<break_minutes> (e.g., !timer 25/5, !timer 45/15)";
            }

            if (!int.TryParse(parts[0].Trim(), out int focusMinutes) ||
                !int.TryParse(parts[1].Trim(), out int breakMinutes))
            {
                return "❌ Invalid time values. Please use numbers only.";
            }

            if (focusMinutes < 5 || focusMinutes > 120)
            {
                return "❌ Focus time must be between 5 and 120 minutes.";
            }

            if (breakMinutes < 2 || breakMinutes > 60)
            {
                return "❌ Break time must be between 2 and 60 minutes.";
            }

            // Update timer service
            _timerService.SetTimer(focusMinutes, breakMinutes);

            // Update session settings
            var session = await _repository.GetTodaySessionAsync();
            session.FocusMinutes = focusMinutes;
            session.BreakMinutes = breakMinutes;
            await _repository.UpdateSessionAsync(session);

            return $"⏰ Timer updated successfully!\n" +
                   $"   🎯 Focus sessions: {focusMinutes} minutes\n" +
                   $"   ☕ Break sessions: {breakMinutes} minutes\n\n" +
                   $"🔔 You'll receive notifications when sessions complete\n" +
                   $"⚠️ Reminders: 5 min before focus ends, 2 min before break ends";
        }
    }
}