namespace TaskManager.CLI.Models;

public class TaskModel
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public TaskStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public string PauseReason { get; set; } = string.Empty;
    public bool IsFocused { get; set; }
    public TimeSpan FocusTime { get; set; }
}

public enum TaskStatus
{
    Pending,
    InProgress,
    Paused,
    Completed,
    Deleted,
    OnBreak
}

public class FocusSession
{
    public DateTime SessionDate { get; set; }
    public TimeSpan TotalFocusTime { get; set; }
    public TimeSpan TotalBreakTime { get; set; }
    public int CompletedFocusSessions { get; set; }
    public int CompletedBreakSessions { get; set; }
    public int FocusMinutes { get; set; } = 25;
    public int BreakMinutes { get; set; } = 5;
    public DateTime? DayStartTime { get; set; }
    public DateTime? DayEndTime { get; set; }
    public bool IsWorkDayActive { get; set; }
}

public class WorkDay
{
    public DateTime Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan PlannedDuration { get; set; } = TimeSpan.FromHours(8.5);
    public bool IsActive { get; set; }
    public List<SessionLog> Sessions { get; set; } = new();
}

public class SessionLog
{
    public DateTime Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public SessionType Type { get; set; }
    public int? TaskId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public enum SessionType
{
    Focus,
    Break,
    Pause,
    Application,
    Command
}

public class DayStatistics
{
    public DateTime Date { get; set; }
    public TimeSpan TotalFocusTime { get; set; }
    public TimeSpan TotalBreakTime { get; set; }
    public int TasksCompleted { get; set; }
    public int FocusSessionsCompleted { get; set; }
    public int BreakSessionsCompleted { get; set; }
    public double ProductivityScore { get; set; }
}