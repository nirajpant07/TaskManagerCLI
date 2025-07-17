using TaskManager.CLI.Services;
using TaskManager.CLI.Utilities;

namespace TaskManager.CLI.Commands.Implementations
{
    public class EndDayCommand : ICommand
    {
        private readonly WorkDayManagerService _workDayManager;
        private readonly ConsoleHelper _console;

        public EndDayCommand(WorkDayManagerService workDayManager, ConsoleHelper console)
        {
            _workDayManager = workDayManager;
            _console = console;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            var isActive = await _workDayManager.IsWorkDayActiveAsync();
            if (!isActive)
            {
                return "⚠️ No active work day to end. Use '!startday' to begin a new work day.";
            }

            var workDay = await _workDayManager.EndWorkDayAsync();
            var duration = workDay.EndTime!.Value - workDay.StartTime;

            return $"🌆 Work day ended at {workDay.EndTime:HH:mm}\n" +
                   $"⏱️ Total duration: {duration:hh\\:mm}\n" +
                   $"💾 Daily backup created successfully\n" +
                   $"📊 Productivity summary will be displayed in popup\n\n" +
                   $"🎉 Great work today! Rest well and see you tomorrow.";
        }
    }
}