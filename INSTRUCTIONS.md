# TaskManagerCLI: User Instructions

## Overview
TaskManagerCLI is a Windows-based command-line productivity tool for managing tasks, focus sessions, and workdays, with robust analytics and a user-friendly exit workflow. It is designed for knowledge workers, developers, and anyone seeking a structured, distraction-resistant workflow.

## Philosophy
- **Minimal friction**: Fast keyboard-driven workflow
- **Focus-first**: Pomodoro-style sessions and workday tracking
- **Data ownership**: All data is stored in a local Excel file for easy backup and analysis
- **Transparency**: Every command and session is logged
- **Gentle reminders**: Smart popups and notifications help you wrap up your day

---

## Getting Started

### Prerequisites
- Windows 10/11
- .NET 8.0 SDK or later ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Microsoft Excel (for viewing data)

### Installation
1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd TaskManagerCLI
   ```
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Build the project:
   ```bash
   dotnet build
   ```
4. Run the application:
   ```bash
   dotnet run --project TaskManager.CLI
   ```

### First Run
- On first launch, a `tasks.xlsx` file will be created in your Documents/TaskManager folder.
- Use `!help` to see all available commands.

---

## Command Reference

### Task Management
| Command | Description | Example |
|--------|-------------|---------|
| `!task <desc>` | Add new task(s) (comma-separated) | `!task Review code, Write docs` |
| `!edit <id> <desc>` | Edit task description | `!edit 1 Update docs` |
| `!done <id>` | Mark task as completed | `!done 1` |
| `!delete <id>` | Delete task | `!delete 1` |
| `!clearlist` | Delete all tasks | `!clearlist` |
| `!cleardone` | Delete completed tasks | `!cleardone` |

### Focus & Break Management
| Command | Description | Example |
|---------|-------------|---------|
| `!focus` | Show current focused task | `!focus` |
| `!focus next [id]` | Start focusing on task | `!focus next 1` |
| `!break` | Start break session | `!break` |
| `!pause [reason]` | Pause current task | `!pause Lunch` |

### Work Day Management
| Command | Description | Example |
|---------|-------------|---------|
| `!startday` | Begin work day (8.5 hours) | `!startday` |
| `!endday` | End work day and backup | `!endday` |
| `!workday` | Show work day status | `!workday` |

### Information & Settings
| Command | Description | Example |
|---------|-------------|---------|
| `!check` | List all tasks | `!check` |
| `!timer <focus>/<break>` | Set timer | `!timer 25/5` |
| `!uptime` | Show daily focus/break time | `!uptime` |
| `!stats` | Detailed daily statistics | `!stats` |
| `!report` | Generate HTML report (last 30 days) | `!report` |
| `!report <end_date>` | Report for 30 days before end_date | `!report 2024-01-31` |
| `!report <start> <end>` | Report for date range | `!report 2024-01-01 2024-01-31` |

### Help
| Command | Description |
|---------|-------------|
| `!help` or `!commands` | Show all available commands |

---

## Exit Workflow (Popups)
- When you exit (using `!exit` or Ctrl+C) **during an active workday**, a Windows popup will ask if you want to end your workday.
    - If you choose **Yes**:
      - For each active task, you can choose to complete, pause for next day, or skip.
      - The workday is ended, data is backed up, and a summary is shown.
    - If you choose **No**:
      - A goodbye popup appears and auto-closes after 5 seconds.
- If you exit when **no workday is active**, only the goodbye popup appears.
- **Note:** The goodbye popup is only reliably shown on `!exit` or Ctrl+C, not when closing the console window with the X button.

---

## HTML Report: Section-by-Section
- **Summary Cards**: Key stats (tasks completed, focus time, productivity score, etc.)
- **Charts**: Visual breakdowns of task status, completions, session types, hourly activity
- **Detailed Tables**: All tasks, sessions, and workdays in tabular form
- **User/System Info**: Your environment and settings
- **Work Day Section**: Daily breakdowns, start/end times, durations
- **Tooltips**: Hover/click info icons for explanations

---

## Best Practices
- **Start your day** with `!startday` to enable the full exit workflow and ensure your workday is tracked.
- **End your day** with `!endday` or `!exit` for proper backup and summary popups.
- **Add all tasks** at the start, then focus on one at a time
- **Use `!focus next`** to move through your list
- **Take breaks** when prompted for optimal productivity
- **Pause tasks** if you need to switch context
- **Generate reports** weekly to review your progress
- **Use the exit popup** to ensure all tasks are properly wrapped up

---

## Troubleshooting & Tips
- If you only see the goodbye popup when exiting, it means no workday is active. Use `!startday` to begin a workday before exiting to trigger the full workflow.
- If Excel file is locked, close Excel before running the app
- If notifications or sounds don't work, check Windows settings
- If the goodbye popup doesn't show on X button, use `!exit` or Ctrl+C
- For advanced analysis, open `tasks.xlsx` in Excel
- All data is local and portable

---

## Getting Help & Contributing
- For issues, feature requests, or contributions, open an issue or pull request on GitHub
- See the code comments and architecture docs for developer guidance
- Licensed under MIT

---

**Stay focused, finish strong!** 