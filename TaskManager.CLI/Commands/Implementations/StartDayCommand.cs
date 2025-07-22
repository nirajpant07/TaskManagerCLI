using TaskManager.CLI.Services;
using System;
using System.Threading.Tasks;
using TaskManager.CLI.Utilities;

namespace TaskManager.CLI.Commands.Implementations
{
    public class StartDayCommand : ICommand
    {
        private readonly IWorkDayManagerService _workDayManager;
        private readonly ConsoleHelper _console;

        public StartDayCommand(IWorkDayManagerService workDayManager, ConsoleHelper console)
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

            try
            {
                var workDay = await _workDayManager.StartWorkDayAsync();
                var endTime = workDay.StartTime.Add(workDay.PlannedDuration);

                return $"🌅 Work day started at {workDay.StartTime:HH:mm}!\n" +
                       $"⏰ Planned end time: {endTime:HH:mm} ({workDay.PlannedDuration.TotalHours} hours)\n" +
                       $"🔔 You'll receive a warning 15 minutes before end of day\n" +
                       $"💾 Daily backup will be created automatically\n\n" +
                       $"💡 Ready to be productive! Use '!task' to add tasks and '!focus next' to start working.";
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                _console.WriteWarning($"⚠️ {ex.Message}");
                _console.Write("Do you want to override and add a new workday anyway? (y/n): ");
                var input = _console.ReadLine();
                if (input != null && input.Trim().ToLower() == "y")
                {
                    var workDay = await _workDayManager.StartWorkDayAsync(true);
                    var endTime = workDay.StartTime.Add(workDay.PlannedDuration);
                    return $"🌅 Work day started at {workDay.StartTime:HH:mm}!\n" +
                           $"⏰ Planned end time: {endTime:HH:mm} ({workDay.PlannedDuration.TotalHours} hours)\n" +
                           $"🔔 You'll receive a warning 15 minutes before end of day\n" +
                           $"💾 Daily backup will be created automatically\n\n" +
                           $"💡 Ready to be productive! Use '!task' to add tasks and '!focus next' to start working.";
                }
                else
                {
                    return "❌ Work day start cancelled.";
                }
            }
        }
    }
}