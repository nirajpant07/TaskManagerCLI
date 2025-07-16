using TaskManagerCLI.Services;
using System;
using System.Threading.Tasks;
using TaskManagerCLI.Utilities;

namespace TaskManagerCLI.Commands.Implementations
{
    public class StartDayCommand : ICommand
    {
        private readonly WorkDayManagerService _workDayManager;
        private readonly ConsoleHelper _console;

        public StartDayCommand(WorkDayManagerService workDayManager, ConsoleHelper console)
        {
            _workDayManager = workDayManager;
            _console = console;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            var isAlreadyActive = await _workDayManager.IsWorkDayActiveAsync();
            if (isAlreadyActive)
            {
                return "⚠️ Work day is already active. Use '!workday' to check status or '!endday' to end current day.";
            }

            var workDay = await _workDayManager.StartWorkDayAsync();
            var endTime = workDay.StartTime.Add(workDay.PlannedDuration);

            return $"🌅 Work day started at {workDay.StartTime:HH:mm}!\n" +
                   $"⏰ Planned end time: {endTime:HH:mm} ({workDay.PlannedDuration.TotalHours} hours)\n" +
                   $"🔔 You'll receive a warning 15 minutes before end of day\n" +
                   $"💾 Daily backup will be created automatically\n\n" +
                   $"💡 Ready to be productive! Use '!task' to add tasks and '!focus next' to start working.";
        }
    }
}