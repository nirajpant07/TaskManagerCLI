# Task Manager CLI

A powerful command-line interface (CLI) application for personal task management with focus sessions, work day tracking, and productivity analytics. Built with .NET 8.0 and designed for Windows environments.

## 🚀 Features

### 📋 Task Management
- **Add tasks** with comma-separated descriptions
- **Edit task** descriptions and details
- **Mark tasks as completed** with timestamps
- **Delete tasks** permanently
- **Pause tasks** with optional reasons
- **Clear completed tasks** or entire task list

### 🎯 Focus & Productivity
- **Pomodoro-style focus sessions** with customizable timers
- **Break management** with automatic notifications
- **Focus tracking** with detailed time logging
- **Session statistics** and productivity scoring

### 📅 Work Day Management
- **8.5-hour work day** tracking
- **Automatic day start/end** management
- **Session logging** for focus, break, and pause periods
- **Daily statistics** and productivity metrics

### 🔔 Smart Notifications
- **Windows notifications** for session completion
- **Sound alerts** for focus/break transitions
- **Error notifications** with audio feedback

### 💾 Data Management
- **Excel-based storage** for easy data sharing and analysis
- **Automatic backups** on work day completion
- **UTF-8 support** for international characters
- **Command logging** tracks all user interactions for audit and analysis
- **HTML reports** with interactive charts, detailed analytics, and enhanced tooltip system

### 🧪 Quality Assurance
- **Comprehensive unit test suite** with 125+ test cases
- **Interface-based architecture** for improved testability
- **Mock-based testing** with Moq framework
- **Test coverage reporting** with coverlet.collector

## 🛠️ Requirements

- **.NET 8.0** or later
- **Windows 10/11** (uses Windows Forms for notifications)
- **Excel** (for data storage and viewing)

## 📦 Installation

### Prerequisites
1. Install [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Ensure you have Excel installed for data viewing

### Build from Source
```bash
# Clone the repository
git clone <repository-url>
cd TaskManagerCLI

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run --project TaskManager.CLI
```

### Run Tests
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests with verbose output
dotnet test --verbosity normal
```

### Single File Distribution
The project is configured to build as a single executable file:
```bash
# Build for release
dotnet publish TaskManager.CLI -c Release

# The executable will be in TaskManager.CLI/bin/Release/net8.0-windows/
```

## 🎮 Usage

### Interactive Mode
Run the application without arguments to enter interactive mode:
```bash
TaskManager.CLI.exe
```

### Single Command Mode
Execute a single command and exit:
```bash
TaskManagerCLI.exe "!task Review code, Write tests"
TaskManagerCLI.exe "!focus next 1"
TaskManagerCLI.exe "!stats"
```

### Exit Workflow (Windows Popup)
- When you exit the app (using `!exit` or Ctrl+C), a Windows popup will ask if you want to end your workday.
- If you choose **Yes**, you will be prompted for each active task to complete, pause for next day, or skip.
- If you choose **No**, a goodbye popup will appear and auto-close after 5 seconds.
- **Note:** Due to Windows limitations, the goodbye popup is only reliably shown on `!exit` or Ctrl+C, not when closing the console window with the X button.

## 📖 Available Commands

### 🔨 Task Management
| Command | Description | Example |
|---------|-------------|---------|
| `!task <description>` | Add new task(s) (comma-separated) | `!task Review code, Write tests` |
| `!edit <id> <description>` | Edit task description | `!edit 1 Updated task description` |
| `!done <id>` | Mark task as completed | `!done 1` |
| `!delete <id>` | Delete task | `!delete 1` |

### 🎯 Focus & Break Management
| Command | Description | Example |
|---------|-------------|---------|
| `!focus` | Show current focused task | `!focus` |
| `!focus next [id]` | Start focusing on task | `!focus next 1` |
| `!break` | Start break session | `!break` |
| `!pause [reason]` | Pause current task | `!pause Lunch break` |

### 📅 Work Day Management
| Command | Description | Example |
|---------|-------------|---------|
| `!startday` | Begin work day (8.5 hours) | `!startday` |
| `!endday` | End work day and backup | `!endday` |
| `!workday` | Show work day status | `!workday` |

### 📊 Information & Settings
| Command | Description | Example |
|---------|-------------|---------|
| `!check` | List all tasks | `!check` |
| `!timer <focus>/<break>` | Set timer | `!timer 25/5` |
| `!uptime` | Show daily focus/break time | `!uptime` |
| `!stats` | Detailed daily statistics | `!stats` |
| `!report` | Generate HTML report (last 30 days) | `!report` |
| `!report <end_date>` | Generate report (30 days before end_date) | `!report 2024-01-31` |
| `!report <start> <end>` | Generate report for specific date range | `!report 2024-01-01 2024-01-31` |

### 🧹 Cleanup
| Command | Description | Example |
|---------|-------------|---------|
| `!clearlist` | Clear all tasks | `!clearlist` |
| `!cleardone` | Clear completed tasks | `!cleardone` |

### 💡 Help
| Command | Description | Example |
|---------|-------------|---------|
| `!help` or `!commands` | Show all available commands | `!help` |

## 🏗️ Architecture

The application follows a clean architecture pattern with the following components:

### Core Components
- **Commands**: Command pattern implementation for CLI operations
- **Services**: Business logic and cross-cutting concerns
- **Repositories**: Data access layer (Excel-based storage)
- **Models**: Data entities and enums
- **Utilities**: Helper classes and console utilities

### Key Services
- **TaskManagerService**: Main application orchestrator
- **IFocusSessionManagerService**: Manages focus and break sessions
- **IWorkDayManagerService**: Handles work day tracking
- **ITimerService**: Manages Pomodoro timers
- **BackupService**: Handles data backups
- **WindowsNotificationService**: Windows-specific notifications
- **WindowsSoundService**: Audio feedback

### Data Models
- **TaskModel**: Task entity with status tracking
- **FocusSession**: Focus session data and statistics
- **WorkDay**: Work day tracking and session logs
- **DayStatistics**: Productivity metrics and analytics

### Testing Architecture
- **Interface-based design**: All services implement interfaces for testability
- **Dependency injection**: Proper DI container configuration
- **Mock-based testing**: Comprehensive unit tests with Moq
- **Test coverage**: 125+ test cases covering all major functionality

## 🔧 Configuration

The application uses dependency injection with the following default configuration:

- **Task Repository**: Excel-based storage
- **Notifications**: Windows notification service
- **Sound**: Windows sound service
- **Timers**: 25-minute focus / 5-minute break (Pomodoro)
- **Work Day**: 8.5-hour duration

## 📁 File Structure

```
TaskManagerCLI/
├── TaskManager.CLI/           # Main project directory
│   ├── Commands/             # Command pattern implementation
│   │   ├── Implementations/ # Individual command classes
│   │   ├── ICommand.cs     # Command interface
│   │   ├── ICommandFactory.cs # Factory interface
│   │   └── CommandFactory.cs  # Command factory
│   ├── Models/
│   │   └── Models.cs        # Data models and enums
│   ├── Repositories/
│   │   ├── ITaskRepository.cs # Repository interface
│   │   └── ExcelTaskRepository.cs # Excel-based implementation
│   ├── Services/            # Business logic services
│   │   ├── IFocusSessionManagerService.cs # Focus session interface
│   │   ├── IWorkDayManagerService.cs # Work day interface
│   │   ├── ITimerService.cs # Timer interface
│   │   ├── FocusSessionManagerService.cs # Focus session implementation
│   │   ├── WorkDayManagerService.cs # Work day implementation
│   │   ├── TimerService.cs # Timer implementation
│   │   ├── TaskManagerService.cs # Main service
│   │   ├── BackupService.cs # Backup functionality
│   │   ├── WindowsNotificationService.cs # Windows notifications
│   │   └── WindowsSoundService.cs # Audio feedback
│   ├── Utilities/
│   │   └── ConsoleHelper.cs # Console I/O utilities
│   ├── Program.cs           # Application entry point
│   └── TaskManager.CLI.csproj # Project configuration
├── TaskManager.CLI.Tests/   # Comprehensive test suite
│   ├── AddTaskCommandTests.cs
│   ├── BreakCommandTests.cs
│   ├── CheckCommandTests.cs
│   ├── ClearDoneCommandTests.cs
│   ├── ClearListCommandTests.cs
│   ├── DeleteCommandTests.cs
│   ├── DoneCommandTests.cs
│   ├── EditTaskCommandTests.cs
│   ├── EndDayCommandTests.cs
│   ├── FocusCommandTests.cs
│   ├── HelpCommandTests.cs
│   ├── PauseCommandTests.cs
│   ├── StartDayCommandTests.cs
│   ├── StatsCommandTests.cs
│   ├── TaskManagerServiceTests.cs
│   ├── TimerCommandTests.cs
│   ├── UptimeCommandTests.cs
│   ├── WorkDayStatusCommandTests.cs
│   └── TaskManager.CLI.Tests.csproj # Test project configuration
├── TaskManager.sln          # Solution file
├── README.md               # This file
├── LICENSE                 # License information
└── .gitignore             # Git ignore rules
```

## 🚀 Quick Start Guide

1. **Start your work day**:
   ```
   !startday
   ```

2. **Add your tasks**:
   ```
   !task Review pull requests, Write documentation, Fix bugs
   ```

3. **Begin focusing**:
   ```
   !focus next 1
   ```

4. **Take breaks when prompted**:
   ```
   !break
   ```

5. **Check your progress**:
   ```
   !stats
   ```

6. **End your work day or exit the app**:
   ```
   !endday
   # or simply type !exit and follow the popup instructions
   ```

7. **Generate productivity report**:
   ```
   !report
   ```
   Or for a specific period:
   ```
   !report 2024-01-01 2024-01-31
   ```

## 📊 Data Storage

All data is stored in a single Excel file: **`tasks.xlsx`** (located in your Documents/TaskManager folder). This file contains the following six sheets:

1. **Tasks**
   - Stores the main task list, including task IDs, descriptions, status, timestamps (created, completed, paused), focus time, and other metadata.

2. **Sessions**
   - Logs all focus, break, pause, and application sessions with start/end times, session type, associated task IDs, and notes.

3. **WorkDays**
   - Tracks each work day, including start/end times, planned duration, active status, and a list of session logs for that day.

4. **SessionLogs**
   - Detailed chronological log of all activities including focus sessions, breaks, pauses, application events, and **command executions** with timestamps and activity notes.

5. **Settings**
   - Application configuration and user preferences including timer settings, work day duration, and notification preferences.

6. **UserInfo**
   - System and user environment information captured at application startup for compatibility and troubleshooting.

**Backup System:**  
- The application automatically creates timestamped backups of `tasks.xlsx` when you end a work day, complete focus sessions, or request a manual backup.  
- Backups are stored in the `Archive` subfolder for easy recovery.

**HTML Reports:**
- Generate comprehensive productivity reports with interactive charts and analytics
- **Multiple report variants:**
  - `!report` - Generate report for the last 30 days
  - `!report <end_date>` - Generate report from 30 days before the specified end date
  - `!report <start_date> <end_date>` - Generate report for a specific date range
- Enhanced tooltip system with mouse-tracked positioning and information icons
- Black background tooltips with white text for optimal readability
- Automatic tooltip positioning and viewport bounds detection
- Support for hover, click, and keyboard interactions (Escape key)
- **Report includes:**
  - Task completion trends and analytics
  - Focus session performance metrics
  - Productivity scoring and efficiency analysis
  - Interactive charts (task status, daily completions, session distribution)
  - Hourly activity patterns and top commands used
  - Work day statistics and user/system information
  - Archived data analysis from previous periods

**Note:**  
- You can open and analyze `tasks.xlsx` in Excel for advanced filtering, reporting, or sharing your productivity data.
- HTML reports are saved in Documents/TaskManager/Reports folder with date range in filename.

## 🧪 Testing

The project includes a comprehensive test suite with 125+ unit tests covering:

### Test Coverage
- **Command Implementations**: All 17 command classes tested
- **Service Layer**: Core business logic services tested
- **Edge Cases**: Error handling and boundary conditions
- **State Changes**: Task status transitions and session management
- **Mock Integration**: Proper dependency mocking with Moq

### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~FocusCommandTests"

# Run with verbose output
dotnet test --verbosity normal
```

### Test Architecture
- **xUnit framework** for test execution
- **Moq library** for dependency mocking
- **Interface-based design** for improved testability
- **Comprehensive assertions** for behavior verification

## 🔄 Backup System

The application automatically creates backups when:
- Ending a work day
- Completing focus sessions
- Manual backup requests

Backups are stored with timestamps for easy recovery.

## 🐛 Troubleshooting

### Common Issues

1. **Excel file locked**: Close any open Excel files before running the application
2. **Notifications not working**: Ensure Windows notifications are enabled
3. **Sound not playing**: Check system volume and audio settings
4. **Goodbye popup not shown on X button**: The goodbye popup is only reliably shown when exiting with `!exit` or Ctrl+C. When closing the console window with the X button, Windows may terminate the process before the popup can display.

### Error Handling

The application includes comprehensive error handling:
- Invalid commands show helpful error messages
- Data corruption triggers automatic recovery
- Network issues are handled gracefully

## 📝 License

This project is licensed under the [MIT License](LICENSE).

You are free to use, modify, and distribute this software for personal or commercial purposes, provided that the original copyright and license notice are included in all copies or substantial portions of the software.

**Summary:**
- ✅ Free for personal and commercial use
- ✅ Modification and redistribution allowed
- ❌ No warranty is provided

See the [LICENSE](LICENSE) file for the full license text.

---

**Happy Productivity! 🎯✨** 