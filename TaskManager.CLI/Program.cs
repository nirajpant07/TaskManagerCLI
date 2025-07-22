using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using TaskManager.CLI.Commands;
using TaskManager.CLI.Repositories;
using TaskManager.CLI.Services;
using TaskManager.CLI.Utilities;

namespace TaskManager.CLI;

internal class Program
{
    private static bool exitHandled = false;
    private static IWorkDayManagerService? workDayManager;
    private static ITaskRepository? repository;
    private static TaskManagerService? taskManager;

    [STAThread]
    private static async Task Main(string[] args)
    {
        // Enable UTF-8
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        // Enable Windows Forms for notifications
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var host = CreateHostBuilder(args).Build();
        taskManager = host.Services.GetRequiredService<TaskManagerService>();
        workDayManager = host.Services.GetRequiredService<IWorkDayManagerService>();
        repository = host.Services.GetRequiredService<ITaskRepository>();
        var focusSessionManager = host.Services.GetRequiredService<IFocusSessionManagerService>();

        AppDomain.CurrentDomain.ProcessExit += (s, e) => HandleExit().GetAwaiter().GetResult();
        Console.CancelKeyPress += (s, e) => { HandleExit().GetAwaiter().GetResult(); e.Cancel = false; };

        try
        {
            if (args.Length > 0)
            {
                // Single command execution
                var command = string.Join(" ", args);
                await taskManager.ProcessCommandAsync(command);
            }
            else
            {
                // Interactive mode
                await taskManager.RunInteractiveAsync();
                // After loop breaks (e.g., !exit), explicitly call exit logic
                await HandleExit();
            }
        }
        finally
        {
            // Ensure exit logic runs if not already handled
            if (!exitHandled)
            {
                await HandleExit();
            }
        }
    }

    public static async Task HandleExit()
    {
        if (exitHandled) return;
        exitHandled = true;
        if (workDayManager == null || repository == null || taskManager == null)
            return;

        // Check if workday is active
        bool isWorkDayActive = false;
        try { isWorkDayActive = await workDayManager.IsWorkDayActiveAsync(); } catch { }

        if (isWorkDayActive)
        {
            var endWorkday = WindowsPopupHelper.ShowEndWorkdayDialog();
            if (endWorkday)
            {
                // Get all active tasks (InProgress or IsFocused)
                var tasks = await repository.GetAllTasksAsync();
                var activeTasks = tasks.Where(t => (t.Status == TaskManager.CLI.Models.TaskStatus.InProgress || t.IsFocused) && t.Status != TaskManager.CLI.Models.TaskStatus.Completed && t.Status != TaskManager.CLI.Models.TaskStatus.Deleted).ToList();
                foreach (var task in activeTasks)
                {
                    string action = WindowsPopupHelper.ShowTaskActionDialog(task.Description);
                    if (action == "complete")
                    {
                        task.Status = TaskManager.CLI.Models.TaskStatus.Completed;
                        task.CompletedAt = DateTime.UtcNow;
                        task.IsFocused = false;
                        await repository.UpdateTaskAsync(task);
                    }
                    else if (action == "pause")
                    {
                        task.Status = TaskManager.CLI.Models.TaskStatus.Paused;
                        task.PausedAt = DateTime.UtcNow;
                        task.PauseReason = "Paused for next day";
                        task.IsFocused = false;
                        await repository.UpdateTaskAsync(task);
                    }
                    // If skip, do nothing
                }
                await repository.SaveAsync();
                // End the workday
                try { await workDayManager.EndWorkDayAsync(); } catch { }
            }
            else
            {
                WindowsPopupHelper.ShowGoodbyeMessageWithTimer(5);
            }
        }
        else
        {
            WindowsPopupHelper.ShowGoodbyeMessageWithTimer(5);
        }

        // Log application end
        await taskManager.LogApplicationEndAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<ITaskRepository, ExcelTaskRepository>();
                services.AddSingleton<TaskManagerService>();
                services.AddSingleton<IFocusSessionManagerService, FocusSessionManagerService>();
                services.AddSingleton<IWorkDayManagerService, WorkDayManagerService>();
                services.AddSingleton<ITimerService, TimerService>();
                services.AddSingleton<INotificationService, WindowsNotificationService>();
                services.AddSingleton<ISoundService, WindowsSoundService>();
                services.AddSingleton<BackupService>();
                services.AddSingleton<ConsoleHelper>();
                services.AddTransient<ICommandFactory, CommandFactory>();
            });
}