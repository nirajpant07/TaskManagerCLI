using System.Text;
using TaskManager.CLI.Commands.Implementations;
using TaskManager.CLI.Repositories;
using TaskManager.CLI.Services;
using TaskManager.CLI.Utilities;

namespace TaskManager.CLI.Commands
{
    public class CommandFactory : ICommandFactory
    {
        private readonly ITaskRepository _repository;
        private readonly IFocusSessionManagerService _sessionManager;
        private readonly IWorkDayManagerService _workDayManager;
        private readonly ITimerService _timerService;
        private readonly INotificationService _notificationService;
        private readonly ISoundService _soundService;
        private readonly ConsoleHelper _console;

        public CommandFactory(ITaskRepository repository, IFocusSessionManagerService sessionManager,
                             IWorkDayManagerService workDayManager, ITimerService timerService,
                             INotificationService notificationService, ISoundService soundService,
                             ConsoleHelper console)
        {
            _repository = repository;
            _sessionManager = sessionManager;
            _workDayManager = workDayManager;
            _timerService = timerService;
            _notificationService = notificationService;
            _soundService = soundService;
            _console = console;
        }

        public ICommand? CreateCommand(string commandName)
        {
            return commandName.ToLower() switch
            {
                "task" or "add" => new AddTaskCommand(_repository),
                "focus" or "now" => new FocusCommand(_repository, _sessionManager),
                "break" => new BreakCommand(_repository, _sessionManager, _notificationService, _soundService),
                "edit" => new EditTaskCommand(_repository),
                "done" => new DoneCommand(_repository, _sessionManager),
                "pause" => new PauseCommand(_repository, _sessionManager),
                "delete" => new DeleteCommand(_repository),
                "check" or "backlog" => new CheckCommand(_repository, _console),
                "timer" => new TimerCommand(_repository, _timerService, _notificationService, _soundService),
                "startday" => new StartDayCommand(_workDayManager, _console),
                "endday" => new EndDayCommand(_workDayManager, _console),
                "workday" => new WorkDayStatusCommand(_workDayManager, _repository, _console),
                "clearlist" => new ClearListCommand(_repository),
                "cleardone" => new ClearDoneCommand(_repository),
                "uptime" => new UptimeCommand(_repository),
                "stats" => new StatsCommand(_repository, _console),
                "report" => new ReportCommand(_repository, _console),
                "commands" or "help" => new HelpCommand(this),
                _ => null
            };
        }

        public string GetHelpText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("📋 Available Commands:");
            sb.AppendLine();

            sb.AppendLine("🔨 Task Management:");
            sb.AppendLine("  !task <description>     - Add new task(s) (comma-separated)");
            sb.AppendLine("  !edit <id> <description> - Edit task description");
            sb.AppendLine("  !done <id>              - Mark task as completed");
            sb.AppendLine("  !delete <id>            - Delete task");
            sb.AppendLine();

            sb.AppendLine("🎯 Focus & Break Management:");
            sb.AppendLine("  !focus                  - Show current focused task");
            sb.AppendLine("  !focus next [id]        - Start focusing on task");
            sb.AppendLine("  !break                  - Start break session");
            sb.AppendLine("  !pause [reason]         - Pause current task");
            sb.AppendLine();

            sb.AppendLine("📅 Work Day Management:");
            sb.AppendLine("  !startday              - Begin work day (8.5 hours)");
            sb.AppendLine("  !endday                - End work day and backup");
            sb.AppendLine("  !workday               - Show work day status");
            sb.AppendLine();

            sb.AppendLine("📊 Information & Settings:");
            sb.AppendLine("  !check                 - List all tasks");
            sb.AppendLine("  !timer <focus>/<break> - Set timer (e.g., !timer 25/5)");
            sb.AppendLine("  !uptime                - Show daily focus/break time");
            sb.AppendLine("  !stats                 - Detailed daily statistics");
            sb.AppendLine("  !report                - Generate HTML report (last 30 days)");
            sb.AppendLine("  !report <end_date>     - Generate report (30 days before end_date)");
            sb.AppendLine("  !report <start> <end>  - Generate report for date range");
            sb.AppendLine("                          Examples:");
            sb.AppendLine("                            !report 2024-01-31");
            sb.AppendLine("                            !report 2024-01-01 2024-01-31");
            sb.AppendLine();

            sb.AppendLine("🧹 Cleanup:");
            sb.AppendLine("  !clearlist             - Clear all tasks");
            sb.AppendLine("  !cleardone             - Clear completed tasks");
            sb.AppendLine();

            sb.AppendLine("💡 Examples:");
            sb.AppendLine("  !task Review code, Write tests, Deploy feature");
            sb.AppendLine("  !focus next 1");
            sb.AppendLine("  !timer 45/15");
            sb.AppendLine("  !break");
            sb.AppendLine("  !report");
            sb.AppendLine("  !report 2024-01-31");

            return sb.ToString();
        }
    }
}