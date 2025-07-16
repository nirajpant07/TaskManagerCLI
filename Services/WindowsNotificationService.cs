using TaskManagerCLI.Models;
using TaskManagerCLI.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskManagerCLI.Services
{
    public class WindowsNotificationService : INotificationService
    {
        public Task ShowSessionCompleteAsync(SessionType type, TimeSpan duration, string nextAction)
        {
            var title = type == SessionType.Focus ? "🎯 Focus Session Complete!" : "☕ Break Session Complete!";
            var message = $"Duration: {duration:mm\\:ss}\n\n💡 Next: {nextAction}";

            return Task.Run(() =>
            {
                var icon = type == SessionType.Focus ? MessageBoxIcon.Information : MessageBoxIcon.Question;
                MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
            });
        }

        public Task ShowWorkDayWarningAsync(TimeSpan remaining)
        {
            var message = $"⏰ Work day ends in {remaining:hh\\:mm}.\n\n" +
                         "📝 Consider wrapping up current tasks.\n" +
                         "💾 Data will be backed up automatically.";

            return Task.Run(() =>
            {
                MessageBox.Show(message, "⚠️ Work Day Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            });
        }

        public Task ShowWorkDayEndAsync(DayStatistics stats)
        {
            var message = $"🎉 Work day complete!\n\n" +
                         $"📊 Daily Summary:\n" +
                         $"   ⏱️ Focus Time: {stats.TotalFocusTime:hh\\:mm}\n" +
                         $"   ☕ Break Time: {stats.TotalBreakTime:hh\\:mm}\n" +
                         $"   ✅ Tasks Completed: {stats.TasksCompleted}\n" +
                         $"   🎯 Focus Sessions: {stats.FocusSessionsCompleted}\n" +
                         $"   📈 Productivity Score: {stats.ProductivityScore:P0}\n\n" +
                         $"💾 Daily backup created successfully!";

            return Task.Run(() =>
            {
                MessageBox.Show(message, "📅 Work Day Summary", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        public Task ShowTimerAlertAsync(string message, int remainingMinutes)
        {
            var fullMessage = $"⏰ {message}\n\n⌛ {remainingMinutes} minutes remaining.";

            return Task.Run(() =>
            {
                MessageBox.Show(fullMessage, "🔔 Timer Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }

        public Task ShowGeneralNotificationAsync(string title, string message)
        {
            return Task.Run(() =>
            {
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }
    }
}