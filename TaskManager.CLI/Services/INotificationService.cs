using TaskManager.CLI.Models;

namespace TaskManager.CLI.Services
{
    public interface INotificationService
    {
        Task ShowSessionCompleteAsync(SessionType type, TimeSpan duration, string nextAction);
        Task ShowWorkDayWarningAsync(TimeSpan remaining);
        Task ShowWorkDayEndAsync(DayStatistics stats);
        Task ShowTimerAlertAsync(string message, int remainingMinutes);
        Task ShowGeneralNotificationAsync(string title, string message);
    }
}