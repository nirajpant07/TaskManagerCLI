using TaskManagerCLI.Models;

namespace TaskManagerCLI.Repositories;

public interface ITaskRepository
{
    Task<List<TaskModel>> GetAllTasksAsync();

    Task<TaskModel?> GetTaskByIdAsync(int id);

    Task<int> AddTaskAsync(TaskModel task);

    Task UpdateTaskAsync(TaskModel task);

    Task DeleteTaskAsync(int id);

    Task<FocusSession> GetTodaySessionAsync();

    Task UpdateSessionAsync(FocusSession session);

    Task<WorkDay?> GetTodayWorkDayAsync();

    Task<WorkDay> StartWorkDayAsync();

    Task<WorkDay> EndWorkDayAsync();

    Task UpdateWorkDayAsync(WorkDay workDay);

    Task AddSessionLogAsync(SessionLog sessionLog);

    Task<List<SessionLog>> GetTodaySessionLogsAsync();

    Task<DayStatistics> GetDayStatisticsAsync(DateTime date);

    Task SaveAsync();

    Task CreateBackupAsync();
}