using TaskManager.CLI.Models;
using TaskManager.CLI.Repositories;
using TaskManager.CLI.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Threading.Timer;

namespace TaskManager.CLI.Services
{
    public class WorkDayManagerService
    {
        private readonly ITaskRepository _repository;
        private readonly INotificationService _notificationService;
        private readonly ISoundService _soundService;
        private readonly BackupService _backupService;
        private Timer? _warningTimer;
        private Timer? _endDayTimer;

        public WorkDayManagerService(ITaskRepository repository, INotificationService notificationService,
                             ISoundService soundService, BackupService backupService)
        {
            _repository = repository;
            _notificationService = notificationService;
            _soundService = soundService;
            _backupService = backupService;
        }

        public async Task<WorkDay> StartWorkDayAsync()
        {
            var workDay = await _repository.StartWorkDayAsync();
            var session = await _repository.GetTodaySessionAsync();
            session.DayStartTime = workDay.StartTime;
            session.IsWorkDayActive = true;
            await _repository.UpdateSessionAsync(session);

            // Clear any existing timers
            _warningTimer?.Dispose();
            _endDayTimer?.Dispose();

            // Calculate timing for warnings and end
            var totalDuration = workDay.PlannedDuration;
            var warningTime = totalDuration - TimeSpan.FromMinutes(15); // 15 minutes before end

            // Start end-of-day warning timer
            if (warningTime > TimeSpan.Zero)
            {
                _warningTimer = new Timer(async _ =>
                {
                    await _soundService.PlayWorkDayWarningAsync();
                    await _notificationService.ShowWorkDayWarningAsync(TimeSpan.FromMinutes(15));
                }, null, warningTime, Timeout.InfiniteTimeSpan);
            }

            // Start end-of-day timer
            _endDayTimer = new Timer(async _ =>
            {
                await EndWorkDayAsync();
            }, null, totalDuration, Timeout.InfiniteTimeSpan);

            return workDay;
        }

        public async Task<WorkDay> EndWorkDayAsync()
        {
            // Clear timers
            _warningTimer?.Dispose();
            _endDayTimer?.Dispose();

            var workDay = await _repository.EndWorkDayAsync();
            var session = await _repository.GetTodaySessionAsync();
            session.DayEndTime = workDay.EndTime;
            session.IsWorkDayActive = false;
            await _repository.UpdateSessionAsync(session);

            // Create backup
            await _backupService.CreateDailyBackupAsync();

            // Show end of day summary
            var stats = await _repository.GetDayStatisticsAsync(DateTime.UtcNow.Date);
            await _soundService.PlayWorkDayEndAsync();
            await _notificationService.ShowWorkDayEndAsync(stats);

            return workDay;
        }

        public async Task<TimeSpan?> GetRemainingWorkTimeAsync()
        {
            var workDay = await _repository.GetTodayWorkDayAsync();
            if (workDay == null || !workDay.IsActive)
                return null;

            var expectedEndTime = workDay.StartTime.Add(workDay.PlannedDuration);
            var remaining = expectedEndTime - DateTime.UtcNow;

            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        public async Task<bool> IsWorkDayActiveAsync()
        {
            var workDay = await _repository.GetTodayWorkDayAsync();
            return workDay?.IsActive ?? false;
        }

        public void Dispose()
        {
            _warningTimer?.Dispose();
            _endDayTimer?.Dispose();
        }
    }
}