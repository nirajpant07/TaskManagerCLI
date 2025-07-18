using TaskManager.CLI.Models;
using System;
using System.Threading.Tasks;

namespace TaskManager.CLI.Services
{
    public interface IWorkDayManagerService
    {
        Task<WorkDay> StartWorkDayAsync();
        Task<WorkDay> EndWorkDayAsync();
        Task<TimeSpan?> GetRemainingWorkTimeAsync();
        Task<bool> IsWorkDayActiveAsync();
        void Dispose();
    }
} 