using TaskManager.CLI.Models;

namespace TaskManager.CLI.Repositories;

public interface ITaskRepository
{
    Task<List<TaskModel>> GetAllTasksAsync();

    Task<TaskModel?> GetTaskByIdAsync(Guid id);

    Task<Guid> AddTaskAsync(TaskModel task);

    Task UpdateTaskAsync(TaskModel task);

    Task DeleteTaskAsync(Guid id);

    Task<FocusSession> GetTodaySessionAsync();

    Task UpdateSessionAsync(FocusSession session);

    Task<WorkDay?> GetTodayWorkDayAsync();

    Task<WorkDay> StartWorkDayAsync();

    Task<WorkDay> StartWorkDayAsync(bool overrideDuplicate);

    Task<WorkDay> EndWorkDayAsync();

    Task UpdateWorkDayAsync(WorkDay workDay);

    Task AddSessionLogAsync(SessionLog sessionLog);

    Task<List<SessionLog>> GetTodaySessionLogsAsync();

    Task<DayStatistics> GetDayStatisticsAsync(DateTime date);

    Task SaveAsync();

    Task CreateBackupAsync();
}