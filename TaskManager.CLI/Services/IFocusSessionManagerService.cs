using TaskManager.CLI.Models;
using System;
using System.Threading.Tasks;

namespace TaskManager.CLI.Services
{
    public interface IFocusSessionManagerService
    {
        Task StartFocusSessionAsync(TaskModel task);
        Task StartBreakSessionAsync();
        Task EndBreakSessionAsync();
        Task EndCurrentSessionAsync();
        Task PauseCurrentSessionAsync(string reason = "");
        bool IsSessionActive { get; }
        TaskModel? CurrentTask { get; }
        SessionType CurrentSessionType { get; }
        TimeSpan? CurrentSessionDuration { get; }
    }
} 