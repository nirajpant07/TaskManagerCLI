using TaskManager.CLI.Repositories;
using TaskManager.CLI.Utilities;
using TaskManager.CLI.Services;

namespace TaskManager.CLI.Commands.Implementations
{
    public class WorkDayStatusCommand : ICommand
    {
        private readonly WorkDayManagerService _workDayManager;
        private readonly ITaskRepository _repository;
        private readonly ConsoleHelper _console;

        public WorkDayStatusCommand(WorkDayManagerService workDayManager, ITaskRepository repository, ConsoleHelper console)
        {
            _workDayManager = workDayManager;
            _repository = repository;
            _console = console;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            var workDay = await _repository.GetTodayWorkDayAsync();
            if (workDay == null || !workDay.IsActive)
            {
                return "📅 No active work day.\n💡 Use '!startday' to begin your work day and activate time tracking.";
            }

            var currentTime = DateTime.UtcNow;
            var elapsed = currentTime - workDay.StartTime;
            var remaining = await _workDayManager.GetRemainingWorkTimeAsync();
            var plannedEnd = workDay.StartTime.Add(workDay.PlannedDuration);
            var session = await _repository.GetTodaySessionAsync();

            var progressPercentage = elapsed.TotalMinutes / workDay.PlannedDuration.TotalMinutes * 100;
            var productivityRatio = session.TotalFocusTime.TotalMinutes > 0 && session.TotalBreakTime.TotalMinutes > 0
                ? session.TotalFocusTime.TotalMinutes / session.TotalBreakTime.TotalMinutes
                : 0;

            return $"📅 Work Day Status ({DateTime.UtcNow.Date:yyyy-MM-dd}):\n\n" +
                   $"⏰ Time Tracking:\n" +
                   $"   Started: {workDay.StartTime:HH:mm}\n" +
                   $"   Planned End: {plannedEnd:HH:mm}\n" +
                   $"   Elapsed: {elapsed:hh\\:mm} ({progressPercentage:F1}%)\n" +
                   $"   Remaining: {remaining:hh\\:mm}\n\n" +
                   $"📊 Today's Activity:\n" +
                   $"   🎯 Focus Time: {session.TotalFocusTime:hh\\:mm}\n" +
                   $"   ☕ Break Time: {session.TotalBreakTime:hh\\:mm}\n" +
                   $"   📈 Focus/Break Ratio: {productivityRatio:F1}:1\n\n" +
                   $"🔄 Sessions Completed:\n" +
                   $"   Focus: {session.CompletedFocusSessions} | Break: {session.CompletedBreakSessions}\n\n" +
                   $"⚙️ Timer Settings:\n" +
                   $"   Focus: {session.FocusMinutes} min | Break: {session.BreakMinutes} min";
        }
    }
}