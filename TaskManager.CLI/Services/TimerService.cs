using Timer = System.Threading.Timer;

namespace TaskManager.CLI.Services
{
    public class TimerService : ITimerService
    {
        private Timer? _focusTimer;
        private Timer? _breakTimer;
        private Timer? _reminderTimer;
        private int _focusMinutes = 25;
        private int _breakMinutes = 5;
        private readonly INotificationService _notificationService;
        private readonly ISoundService _soundService;

        public TimerService(INotificationService notificationService, ISoundService soundService)
        {
            _notificationService = notificationService;
            _soundService = soundService;
        }

        public void SetTimer(int focusMinutes, int breakMinutes)
        {
            _focusMinutes = focusMinutes;
            _breakMinutes = breakMinutes;
        }

        public void StartFocusTimer(Action onFocusComplete)
        {
            StopAllTimers();

            var focusDuration = TimeSpan.FromMinutes(_focusMinutes);
            var reminderTime = focusDuration.Subtract(TimeSpan.FromMinutes(5)); // 5 min warning

            // Set reminder timer (5 minutes before focus ends)
            if (reminderTime > TimeSpan.Zero)
            {
                _reminderTimer = new Timer(async _ =>
                {
                    await _notificationService.ShowTimerAlertAsync("Focus session ending soon", 5);
                }, null, reminderTime, Timeout.InfiniteTimeSpan);
            }

            // Set main focus timer
            _focusTimer = new Timer(async _ =>
            {
                await _soundService.PlaySessionCompleteAsync();
                onFocusComplete();
            }, null, focusDuration, Timeout.InfiniteTimeSpan);
        }

        public void StartBreakTimer(Action onBreakComplete)
        {
            StopAllTimers();

            var breakDuration = TimeSpan.FromMinutes(_breakMinutes);
            var reminderTime = breakDuration.Subtract(TimeSpan.FromMinutes(2)); // 2 min warning for breaks

            // Set reminder timer (2 minutes before break ends)
            if (reminderTime > TimeSpan.Zero)
            {
                _reminderTimer = new Timer(async _ =>
                {
                    await _notificationService.ShowTimerAlertAsync("Break ending soon", 2);
                }, null, reminderTime, Timeout.InfiniteTimeSpan);
            }

            // Set main break timer
            _breakTimer = new Timer(async _ =>
            {
                await _soundService.PlayBreakCompleteAsync();
                onBreakComplete();
            }, null, breakDuration, Timeout.InfiniteTimeSpan);
        }

        public void StopAllTimers()
        {
            _focusTimer?.Dispose();
            _breakTimer?.Dispose();
            _reminderTimer?.Dispose();
            _focusTimer = null;
            _breakTimer = null;
            _reminderTimer = null;
        }

        public (int FocusMinutes, int BreakMinutes) GetCurrentSettings()
        {
            return (_focusMinutes, _breakMinutes);
        }

        public void Dispose()
        {
            StopAllTimers();
        }
    }
}