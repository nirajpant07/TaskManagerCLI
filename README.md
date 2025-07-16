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
dotnet run
```

### Single File Distribution
The project is configured to build as a single executable file:
```bash
# Build for release
dotnet publish -c Release

# The executable will be in bin/Release/net8.0-windows/
```

## 🎮 Usage

### Interactive Mode
Run the application without arguments to enter interactive mode:
```bash
TaskManagerCLI.exe
```

### Single Command Mode
Execute a single command and exit:
```bash
TaskManagerCLI.exe "!task Review code, Write tests"
TaskManagerCLI.exe "!focus next 1"
TaskManagerCLI.exe "!stats"
```

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
- **FocusSessionManagerService**: Manages focus and break sessions
- **WorkDayManagerService**: Handles work day tracking
- **TimerService**: Manages Pomodoro timers
- **BackupService**: Handles data backups
- **WindowsNotificationService**: Windows-specific notifications
- **WindowsSoundService**: Audio feedback

### Data Models
- **TaskModel**: Task entity with status tracking
- **FocusSession**: Focus session data and statistics
- **WorkDay**: Work day tracking and session logs
- **DayStatistics**: Productivity metrics and analytics

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
├── Commands/                 # Command pattern implementation
│   ├── Implementations/     # Individual command classes
│   ├── ICommand.cs         # Command interface
│   ├── ICommandFactory.cs  # Factory interface
│   └── CommandFactory.cs   # Command factory
├── Models/
│   └── Models.cs           # Data models and enums
├── Repositories/
│   ├── ITaskRepository.cs  # Repository interface
│   └── ExcelTaskRepository.cs # Excel-based implementation
├── Services/               # Business logic services
├── Utilities/
│   └── ConsoleHelper.cs    # Console I/O utilities
├── Program.cs              # Application entry point
└── TaskManagerCLI.csproj   # Project configuration
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

6. **End your work day**:
   ```
   !endday
   ```

## 📊 Data Storage

Tasks and session data are stored in Excel files:
- **Tasks**: `tasks.xlsx` - Contains task list with status and timestamps
- **Sessions**: `sessions.xlsx` - Contains focus and break session logs
- **Statistics**: `statistics.xlsx` - Contains daily productivity metrics

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

### Error Handling

The application includes comprehensive error handling:
- Invalid commands show helpful error messages
- Data corruption triggers automatic recovery
- Network issues are handled gracefully

---

**Happy Productivity! 🎯✨** 