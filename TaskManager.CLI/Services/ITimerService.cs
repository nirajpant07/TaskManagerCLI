using System;
using System.Threading.Tasks;

namespace TaskManager.CLI.Services
{
    public interface ITimerService
    {
        void SetTimer(int focusMinutes, int breakMinutes);
        void StartFocusTimer(Action onFocusComplete);
        void StartBreakTimer(Action onBreakComplete);
        void StopAllTimers();
        (int FocusMinutes, int BreakMinutes) GetCurrentSettings();
        void Dispose();
    }
} 