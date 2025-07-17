using TaskManagerCLI.Commands;
using TaskManagerCLI.Models;
using TaskManagerCLI.Repositories;
using TaskManagerCLI.Services;
using System;
using System.Threading.Tasks;
using TaskManagerCLI.Utilities;

namespace TaskManagerCLI.Services
{
    public class TaskManagerService
    {
        private readonly ITaskRepository _repository;
        private readonly ICommandFactory _commandFactory;
        private readonly ConsoleHelper _console;
        private readonly ISoundService _soundService;
        private bool _applicationStarted = false;
        private DateTime? _applicationStartTime;

        public TaskManagerService(ITaskRepository repository, ICommandFactory commandFactory,
                          ConsoleHelper console, ISoundService soundService)
        {
            _repository = repository;
            _commandFactory = commandFactory;
            _console = console;
            _soundService = soundService;
        }

        public async Task RunInteractiveAsync()
        {
            await LogApplicationStartAsync();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
╔══════════════════════════════════════════════════════════════╗
║                     Task Manager CLI                         ║
║              Personal Productivity Assistant                 ║
╚══════════════════════════════════════════════════════════════╝
");
            Console.ResetColor();

            Console.WriteLine("🎯 Welcome to your personal task management system!");
            Console.WriteLine();
            Console.WriteLine("✨ Features:");
            Console.WriteLine("   • Focus session tracking with Pomodoro timers");
            Console.WriteLine("   • Automatic work day management (8.5 hours)");
            Console.WriteLine("   • Smart notifications and sound alerts");
            Console.WriteLine("   • Daily backups and productivity analytics");
            Console.WriteLine("   • Excel-based data storage for easy sharing");
            Console.WriteLine();
            Console.WriteLine("🚀 Quick Start:");
            Console.WriteLine("   1. Type !startday to begin your work day");
            Console.WriteLine("   2. Type !task [description] to add tasks");
            Console.WriteLine("   3. Type !focus next to start working");
            Console.WriteLine("   4. Type !break when the timer completes");
            Console.WriteLine("   5. Type !help for all available commands");
            Console.WriteLine();

            while (true)
            {
                _console.Write("📝 > ");
                var input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.Equals("!exit", StringComparison.OrdinalIgnoreCase))
                {
                    _console.WriteLine("👋 Goodbye! Have a productive day!");
                    break;
                }

                try
                {
                    var result = await ProcessCommandAsync(input);
                    if (!string.IsNullOrEmpty(result))
                    {
                        _console.WriteSuccess(result);
                    }
                }
                catch (Exception ex)
                {
                    await _soundService.PlayErrorSoundAsync();
                    _console.WriteError($"❌ Error: {ex.Message}");
                }

                _console.WriteLine();
            }
        }

        public async Task<string> ProcessCommandAsync(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine) || !commandLine.StartsWith("!"))
            {
                return "⚠️ Commands must start with '!' (e.g., !task, !focus, !help)";
            }

            var parts = commandLine[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return "❌ Invalid command format";
            }

            var commandName = parts[0].ToLower();
            var parameters = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

            var command = _commandFactory.CreateCommand(commandName);
            if (command == null)
            {
                await _soundService.PlayErrorSoundAsync();
                return $"❓ Unknown command: '{commandName}'. Type '!help' for available commands.";
            }

            try
            {
                var result = await command.ExecuteAsync(parameters);
                await _repository.SaveAsync();
                return result;
            }
            catch (Exception ex)
            {
                await _soundService.PlayErrorSoundAsync();
                throw new Exception($"Command execution failed: {ex.Message}", ex);
            }
        }

        public async Task LogApplicationStartAsync()
        {
            _applicationStarted = true;
            _applicationStartTime = DateTime.UtcNow;

            await _repository.AddSessionLogAsync(new SessionLog
            {
                Date = DateTime.UtcNow.Date,
                StartTime = _applicationStartTime.Value,
                Type = SessionType.Application,
                Notes = "Application started"
            });
        }

        public async Task LogApplicationEndAsync()
        {
            if (_applicationStarted && _applicationStartTime.HasValue)
            {
                var sessionDuration = DateTime.UtcNow - _applicationStartTime.Value;

                await _repository.AddSessionLogAsync(new SessionLog
                {
                    Date = DateTime.UtcNow.Date,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow,
                    Type = SessionType.Application,
                    Notes = $"Application ended - Total session time: {sessionDuration:hh\\:mm\\:ss}"
                });

                await _repository.SaveAsync();
            }
        }
    }
}