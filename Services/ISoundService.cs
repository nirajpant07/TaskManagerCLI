using System;
using System.Threading.Tasks;

namespace TaskManagerCLI.Services
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