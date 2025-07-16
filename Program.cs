using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaskManagerCLI.Commands;
using TaskManagerCLI.Repositories;
using TaskManagerCLI.Services;
using TaskManagerCLI.Utilities;

namespace TaskManagerCLI;

class Program
{
    [STAThread]
    static async Task Main(string[] args)
    {
        // Enable UTF-8
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        // Enable Windows Forms for notifications
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var host = CreateHostBuilder(args).Build();
        var taskManager = host.Services.GetRequiredService<TaskManagerService>();

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
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<ITaskRepository, ExcelTaskRepository>();
                services.AddSingleton<TaskManagerService>();
                services.AddSingleton<FocusSessionManagerService>();
                services.AddSingleton<WorkDayManagerService>();
                services.AddSingleton<TimerService>();
                services.AddSingleton<INotificationService, WindowsNotificationService>();
                services.AddSingleton<ISoundService, WindowsSoundService>();
                services.AddSingleton<BackupService>();
                services.AddSingleton<ConsoleHelper>();
                services.AddTransient<ICommandFactory, CommandFactory>();
            });
}