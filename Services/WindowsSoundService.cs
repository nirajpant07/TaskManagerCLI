using TaskManagerCLI.Services;
using System.Media;
using System.Threading.Tasks;

namespace TaskManagerCLI.Services
{
    public class WindowsSoundService : ISoundService
    {
        public Task PlaySessionCompleteAsync()
        {
            return Task.Run(() =>
            {
                // Pleasant completion sound for focus sessions
                SystemSounds.Asterisk.Play();
            });
        }

        public Task PlayBreakCompleteAsync()
        {
            return Task.Run(() =>
            {
                // Gentle reminder sound for break completion
                SystemSounds.Question.Play();
            });
        }

        public Task PlayWorkDayWarningAsync()
        {
            return Task.Run(() =>
            {
                // Attention sound for work day warning
                SystemSounds.Exclamation.Play();
            });
        }

        public Task PlayWorkDayEndAsync()
        {
            return Task.Run(() =>
            {
                // End of day sound
                SystemSounds.Hand.Play();
            });
        }

        public Task PlayErrorSoundAsync()
        {
            return Task.Run(() =>
            {
                // Error/warning sound
                SystemSounds.Beep.Play();
            });
        }
    }
}