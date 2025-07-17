using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using TaskManager.CLI.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.CLI.Services
{
    public class FocusSessionManagerService
    {
        private readonly ITaskRepository _repository;
        private readonly INotificationService _notificationService;
        private readonly ISoundService _soundService;
        private DateTime? _currentSessionStart;
        private TaskModel? _currentTask;
        private SessionType _currentSessionType;

        public FocusSessionManagerService(ITaskRepository repository, INotificationService notificationService, ISoundService soundService)
        {
            _repository = repository;
            _notificationService = notificationService;
            _soundService = soundService;
        }

        public async Task StartFocusSessionAsync(TaskModel task)
        {
            // End current session if active
            if (_currentTask != null && _currentSessionStart.HasValue)
            {
                await EndCurrentSessionAsync();
            }

            _currentTask = task;
            _currentSessionStart = DateTime.UtcNow;
            _currentSessionType = SessionType.Focus;

            // Add session log
            await _repository.AddSessionLogAsync(new SessionLog
            {
                Date = DateTime.UtcNow.Date,
                StartTime = _currentSessionStart.Value,
                Type = SessionType.Focus,
                TaskId = task.Id,
                Notes = $"Started focus on: {task.Description}"
            });

            // Get timer settings and set up automatic completion
            var session = await _repository.GetTodaySessionAsync();
            var focusDuration = TimeSpan.FromMinutes(session.FocusMinutes);

            // Schedule automatic session completion
            _ = Task.Delay(focusDuration).ContinueWith(async _ =>
            {
                if (_currentSessionType == SessionType.Focus && _currentTask?.Id == task.Id)
                {
                    await EndCurrentSessionAsync();
                    await _soundService.PlaySessionCompleteAsync();

                    var nextAction = "Start break with !break or continue with next task";
                    await _notificationService.ShowSessionCompleteAsync(
                        SessionType.Focus, focusDuration, nextAction);
                }
            });
        }

        public async Task StartBreakSessionAsync()
        {
            _currentSessionStart = DateTime.UtcNow;
            _currentSessionType = SessionType.Break;
            _currentTask = null;

            // Add session log
            await _repository.AddSessionLogAsync(new SessionLog
            {
                Date = DateTime.UtcNow.Date,
                StartTime = _currentSessionStart.Value,
                Type = SessionType.Break,
                Notes = "Break session started"
            });

            // Set up automatic break completion
            var session = await _repository.GetTodaySessionAsync();
            var breakDuration = TimeSpan.FromMinutes(session.BreakMinutes);

            _ = Task.Delay(breakDuration).ContinueWith(async _ =>
            {
                if (_currentSessionType == SessionType.Break)
                {
                    await EndBreakSessionAsync();
                    await _soundService.PlayBreakCompleteAsync();

                    var nextAction = "Resume work with !focus next or start new task";
                    await _notificationService.ShowSessionCompleteAsync(
                        SessionType.Break, breakDuration, nextAction);
                }
            });
        }

        public async Task EndBreakSessionAsync()
        {
            if (_currentSessionStart.HasValue && _currentSessionType == SessionType.Break)
            {
                var sessionDuration = DateTime.UtcNow - _currentSessionStart.Value;

                var session = await _repository.GetTodaySessionAsync();
                session.TotalBreakTime += sessionDuration;
                session.CompletedBreakSessions++;
                await _repository.UpdateSessionAsync(session);

                // Update session log
                var sessionLogs = await _repository.GetTodaySessionLogsAsync();
                var currentLog = sessionLogs
                    .Where(l => l.Type == SessionType.Break && !l.EndTime.HasValue)
                    .OrderByDescending(l => l.StartTime)
                    .FirstOrDefault();

                if (currentLog != null)
                {
                    currentLog.EndTime = DateTime.UtcNow;
                    currentLog.Notes += $" - Duration: {sessionDuration:mm\\:ss}";
                }

                _currentSessionStart = null;
                _currentSessionType = SessionType.Focus; // Reset for next session
            }
        }

        public async Task EndCurrentSessionAsync()
        {
            if (_currentTask != null && _currentSessionStart.HasValue && _currentSessionType == SessionType.Focus)
            {
                var sessionDuration = DateTime.UtcNow - _currentSessionStart.Value;
                _currentTask.FocusTime += sessionDuration;
                await _repository.UpdateTaskAsync(_currentTask);

                var session = await _repository.GetTodaySessionAsync();
                session.TotalFocusTime += sessionDuration;
                session.CompletedFocusSessions++;
                await _repository.UpdateSessionAsync(session);

                // Update session log
                var sessionLogs = await _repository.GetTodaySessionLogsAsync();
                var currentLog = sessionLogs
                    .Where(l => l.Type == SessionType.Focus && l.TaskId == _currentTask.Id && !l.EndTime.HasValue)
                    .OrderByDescending(l => l.StartTime)
                    .FirstOrDefault();

                if (currentLog != null)
                {
                    currentLog.EndTime = DateTime.UtcNow;
                    currentLog.Notes += $" - Duration: {sessionDuration:mm\\:ss}";
                }

                _currentTask = null;
                _currentSessionStart = null;
            }
        }

        public async Task PauseCurrentSessionAsync(string reason = "")
        {
            if (_currentTask != null && _currentSessionStart.HasValue && _currentSessionType == SessionType.Focus)
            {
                var sessionDuration = DateTime.UtcNow - _currentSessionStart.Value;
                _currentTask.FocusTime += sessionDuration;
                _currentTask.PauseReason = reason;
                _currentTask.PausedAt = DateTime.UtcNow;
                await _repository.UpdateTaskAsync(_currentTask);

                var session = await _repository.GetTodaySessionAsync();
                session.TotalFocusTime += sessionDuration;
                await _repository.UpdateSessionAsync(session);

                // Add pause log
                await _repository.AddSessionLogAsync(new SessionLog
                {
                    Date = DateTime.UtcNow.Date,
                    StartTime = DateTime.UtcNow,
                    Type = SessionType.Pause,
                    TaskId = _currentTask.Id,
                    Notes = $"Paused task: {_currentTask.Description}" +
                           (string.IsNullOrEmpty(reason) ? "" : $" - Reason: {reason}")
                });

                _currentSessionStart = null;
            }
        }

        public bool IsSessionActive => _currentSessionStart.HasValue;
        public TaskModel? CurrentTask => _currentTask;
        public SessionType CurrentSessionType => _currentSessionType;
        public TimeSpan? CurrentSessionDuration => _currentSessionStart.HasValue ?
            DateTime.UtcNow - _currentSessionStart.Value : null;
    }
}