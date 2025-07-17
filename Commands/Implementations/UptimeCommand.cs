using TaskManagerCLI.Repositories;

namespace TaskManagerCLI.Commands.Implementations
{
    public class UptimeCommand : ICommand
    {
        private readonly ITaskRepository _repository;

        public UptimeCommand(ITaskRepository repository)
        {
            _repository = repository;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            var session = await _repository.GetTodaySessionAsync();
            var workDay = await _repository.GetTodayWorkDayAsync();

            var totalActiveTime = session.TotalFocusTime + session.TotalBreakTime;
            var focusPercentage = totalActiveTime.TotalMinutes > 0
                ? session.TotalFocusTime.TotalMinutes / totalActiveTime.TotalMinutes * 100
                : 0;

            var result = $"⏱️ Daily Time Summary ({DateTime.UtcNow.Date:yyyy-MM-dd}):\n\n" +
                        $"🎯 Focus Time: {session.TotalFocusTime:hh\\:mm\\:ss}\n" +
                        $"☕ Break Time: {session.TotalBreakTime:hh\\:mm\\:ss}\n" +
                        $"📊 Total Active: {totalActiveTime:hh\\:mm\\:ss}\n" +
                        $"📈 Focus Efficiency: {focusPercentage:F1}%\n\n" +
                        $"🔄 Sessions Completed:\n" +
                        $"   Focus: {session.CompletedFocusSessions} | Break: {session.CompletedBreakSessions}";

            if (workDay?.IsActive == true)
            {
                var elapsed = DateTime.UtcNow - workDay.StartTime;
                var remaining = workDay.StartTime.Add(workDay.PlannedDuration) - DateTime.UtcNow;
                result += $"\n\n📅 Work Day Progress:\n" +
                         $"   Elapsed: {elapsed:hh\\:mm} | Remaining: {remaining:hh\\:mm}";
            }

            return result;
        }
    }
}