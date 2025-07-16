using TaskManagerCLI.Repositories;
using TaskManagerCLI.Utilities;
using TaskStatus = TaskManagerCLI.Models.TaskStatus;

namespace TaskManagerCLI.Commands.Implementations
{
    public class StatsCommand : ICommand
    {
        private readonly ITaskRepository _repository;
        private readonly ConsoleHelper _console;

        public StatsCommand(ITaskRepository repository, ConsoleHelper console)
        {
            _repository = repository;
            _console = console;
        }

        public async Task<string> ExecuteAsync(string[] parameters)
        {
            var stats = await _repository.GetDayStatisticsAsync(DateTime.Today);
            var session = await _repository.GetTodaySessionAsync();
            var tasks = await _repository.GetAllTasksAsync();

            var totalTasks = tasks.Count(t => t.Status != TaskStatus.Deleted);
            var pendingTasks = tasks.Count(t => t.Status == TaskStatus.Pending);
            var avgFocusPerSession = stats.FocusSessionsCompleted > 0
                ? stats.TotalFocusTime.TotalMinutes / stats.FocusSessionsCompleted
                : 0;
            var focusBreakRatio = stats.TotalBreakTime.TotalMinutes > 0
                ? stats.TotalFocusTime.TotalMinutes / stats.TotalBreakTime.TotalMinutes
                : 0;

            return $"📊 Detailed Statistics ({DateTime.Today:yyyy-MM-dd}):\n\n" +
                   $"⏱️ Time Tracking:\n" +
                   $"   🎯 Focus Time: {stats.TotalFocusTime:hh\\:mm\\:ss}\n" +
                   $"   ☕ Break Time: {stats.TotalBreakTime:hh\\:mm\\:ss}\n" +
                   $"   📊 Total Active: {stats.TotalFocusTime + stats.TotalBreakTime:hh\\:mm\\:ss}\n\n" +
                   $"🔄 Session Counts:\n" +
                   $"   Focus Sessions: {stats.FocusSessionsCompleted}\n" +
                   $"   Break Sessions: {stats.BreakSessionsCompleted}\n" +
                   $"   ✅ Tasks Completed: {stats.TasksCompleted}\n" +
                   $"   📝 Total Tasks: {totalTasks} | Pending: {pendingTasks}\n\n" +
                   $"📈 Productivity Metrics:\n" +
                   $"   Productivity Score: {stats.ProductivityScore:P1}\n" +
                   $"   Avg Focus/Session: {avgFocusPerSession:F1} minutes\n" +
                   $"   Focus/Break Ratio: {focusBreakRatio:F1}:1\n\n" +
                   $"⚙️ Settings:\n" +
                   $"   Focus Duration: {session.FocusMinutes} min\n" +
                   $"   Break Duration: {session.BreakMinutes} min";
        }
    }
}