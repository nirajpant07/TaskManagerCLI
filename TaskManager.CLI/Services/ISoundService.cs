using System;
using System.Threading.Tasks;

namespace TaskManager.CLI.Services
{

    public interface ISoundService
    {
        Task PlaySessionCompleteAsync();
        Task PlayBreakCompleteAsync();
        Task PlayWorkDayWarningAsync();
        Task PlayWorkDayEndAsync();
        Task PlayErrorSoundAsync();
    }
}