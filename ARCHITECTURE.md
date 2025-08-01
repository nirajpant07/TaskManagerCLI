# TaskManager.CLI Architecture

## 🏗️ System Architecture Overview

The TaskManager.CLI follows a **Clean Architecture** pattern with clear separation of concerns, dependency inversion, and interface-based design for improved testability.

```mermaid
graph TB
    subgraph "Presentation Layer"
        CLI[Console Interface]
        UI[Windows Forms Notifications]
        POPUP[WindowsPopupHelper<br> Exit Popups]
    end

    subgraph "Application Layer"
        TM[TaskManagerService]
        CF[CommandFactory]
        subgraph "Commands"
            AT[AddTaskCommand]
            FC[FocusCommand]
            BC[BreakCommand]
            DC[DeleteCommand]
            EC[EditTaskCommand]
            DN[DoneCommand]
            PC[PauseCommand]
            TC[TimerCommand]
            CC[CheckCommand]
            CL[ClearListCommand]
            CD[ClearDoneCommand]
            SD[StartDayCommand]
            ED[EndDayCommand]
            WD[WorkDayStatusCommand]
            UP[UptimeCommand]
            ST[StatsCommand]
            HC[HelpCommand]
            RC[ReportCommand]
        end
    end

    subgraph "Domain Layer"
        subgraph "Services"
            FSM[FocusSessionManagerService]
            WDM[WorkDayManagerService]
            TS[TimerService]
            BS[BackupService]
            WNS[WindowsNotificationService]
            WSS[WindowsSoundService]
            HRG[HtmlReportGenerator<br/>Enhanced Tooltip System]
        end
        
        subgraph "Interfaces"
            IFSM[IFocusSessionManagerService]
            IWDM[IWorkDayManagerService]
            ITS[ITimerService]
            INOT[INotificationService]
            ISND[ISoundService]
        end
    end

    subgraph "Infrastructure Layer"
        ETR[ExcelTaskRepository]
        ITR[ITaskRepository]
        CH[ConsoleHelper]
    end

    subgraph "Data Layer"
        Excel[(Excel File<br/>tasks.xlsx)]
        subgraph "Excel Sheets"
            Tasks[Tasks Sheet]
            Sessions[Sessions Sheet]
            WorkDays[WorkDays Sheet]
            SessionLogs[SessionLogs Sheet]
            Settings[Settings Sheet]
            UserInfo[UserInfo Sheet]
        end
        Reports[(Reports Directory<br/>HTML Reports)]
    end

    %% Presentation Layer Connections
    CLI --> TM
    UI --> WNS
    POPUP --> TM

    %% Application Layer Connections
    TM --> CF
    CF --> AT
    CF --> FC
    CF --> BC
    CF --> DC
    CF --> EC
    CF --> DN
    CF --> PC
    CF --> TC
    CF --> CC
    CF --> CL
    CF --> CD
    CF --> SD
    CF --> ED
    CF --> WD
    CF --> UP
    CF --> ST
    CF --> HC
    CF --> RC

    %% Command Dependencies
    AT --> ITR
    FC --> ITR
    FC --> IFSM
    BC --> ITR
    BC --> IFSM
    BC --> INOT
    BC --> ISND
    DC --> ITR
    EC --> ITR
    DN --> ITR
    DN --> IFSM
    PC --> ITR
    PC --> IFSM
    TC --> ITR
    TC --> ITS
    TC --> INOT
    TC --> ISND
    CC --> ITR
    CC --> CH
    CL --> ITR
    CD --> ITR
    SD --> IWDM
    SD --> CH
    ED --> IWDM
    ED --> CH
    WD --> IWDM
    WD --> ITR
    WD --> CH
    UP --> ITR
    ST --> ITR
    ST --> CH
    RC --> ITR
    RC --> CH
    RC --> HRG
    HC --> CF

    %% Service Implementations
    FSM -.->|implements| IFSM
    WDM -.->|implements| IWDM
    TS -.->|implements| ITS
    WNS -.->|implements| INOT
    WSS -.->|implements| ISND

    %% Infrastructure Layer
    ETR -.->|implements| ITR
    ETR --> Excel

    %% Data Flow
    Excel --> Tasks
    Excel --> Sessions
    Excel --> WorkDays
    Excel --> SessionLogs
    Excel --> Settings
    Excel --> UserInfo
    HRG --> Reports

    %% Service Dependencies
    FSM --> ITR
    FSM --> INOT
    FSM --> ISND
    WDM --> ITR
    WDM --> INOT
    WDM --> ISND
    WDM --> BS
    TS --> INOT
    TS --> ISND
    BS --> ITR

    %% Styling
    classDef presentationLayer fill:#E6EBE0,stroke:#000000,stroke-width:2px,color:#000000
    classDef applicationLayer fill:#5CA4A9,stroke:#000000,stroke-width:2px,color:#000000
    classDef domainLayer fill:#9BC1BC,stroke:#000000,stroke-width:2px,color:#000000
    classDef infrastructureLayer fill:#F4F1BB,stroke:#000000,stroke-width:2px,color:#000000
    classDef dataLayer fill:#E6EBE0,stroke:#000000,stroke-width:2px,color:#000000

    class CLI,UI,POPUP presentationLayer
    class TM,CF,AT,FC,BC,DC,EC,DN,PC,TC,CC,CL,CD,SD,ED,WD,UP,ST,HC,RC applicationLayer
    class FSM,WDM,TS,BS,WNS,WSS,HRG,IFSM,IWDM,ITS,INOT,ISND domainLayer
    class ETR,ITR,CH infrastructureLayer
    class Excel,Tasks,Sessions,WorkDays,SessionLogs,Settings,UserInfo,Reports dataLayer
```

## 📊 Component Relationships

### 1. **Presentation Layer**
- **Console Interface**: Main CLI interaction point
- **Windows Forms**: Notifications and alerts
- **WindowsPopupHelper**: Handles exit popups for workday management and goodbye messages

### 2. **Application Layer**
- **TaskManagerService**: Main orchestrator and application entry point
- **CommandFactory**: Creates and manages command instances
- **Commands**: 18 command implementations following Command pattern

### 3. **Domain Layer**
- **Services**: Business logic implementation
- **Interfaces**: Contract definitions for dependency inversion

### 4. **Infrastructure Layer**
- **ExcelTaskRepository**: Data persistence implementation
- **ConsoleHelper**: Console I/O utilities

### 5. **Data Layer**
- **Excel File**: Single source of truth with 6 structured sheets

## 🔄 Data Flow Architecture

```mermaid
sequenceDiagram
    participant User
    participant CLI as Console Interface
    participant TM as TaskManagerService
    participant POPUP as WindowsPopupHelper
    participant CF as CommandFactory
    participant CMD as Command
    participant SVC as Service
    participant REPO as Repository
    participant Excel as Excel File

    User->>CLI: Enter command (!task "New task")
    CLI->>TM: ProcessCommandAsync(command)
    TM->>REPO: LogCommandExecution(command)
    REPO->>Excel: Save to SessionLogs sheet
    TM->>CF: CreateCommand("task")
    CF->>CMD: new AddTaskCommand(repository)
    TM->>CMD: ExecuteAsync(parameters)
    CMD->>REPO: AddTaskAsync(task)
    REPO->>Excel: Save to Tasks sheet
    Excel-->>REPO: Confirmation
    REPO-->>CMD: Task ID
    CMD-->>TM: Success message
    TM-->>CLI: Display result
    CLI-->>User: "✅ Task 1 added: New task"
    User->>CLI: Exit app (!exit or Ctrl+C)
    CLI->>POPUP: Show exit popup (end workday?)
    alt User chooses Yes
        POPUP->>TM: End workday, handle active tasks
        TM->>REPO: Update tasks, end workday
        POPUP->>User: Show goodbye popup (auto-close)
    else User chooses No
        POPUP->>User: Show goodbye popup (auto-close)
    end
```

## 🎯 Command Pattern Implementation

```mermaid
graph TB
    subgraph "Command Pattern"
        ICommand[ICommand Interface]
        subgraph "Concrete Commands"
            AT[AddTaskCommand]
            FC[FocusCommand]
            BC[BreakCommand]
            DC[DeleteCommand]
            EC[EditTaskCommand]
            DN[DoneCommand]
            PC[PauseCommand]
            TC[TimerCommand]
            CC[CheckCommand]
            CL[ClearListCommand]
            CD[ClearDoneCommand]
            SD[StartDayCommand]
            ED[EndDayCommand]
            WD[WorkDayStatusCommand]
            UP[UptimeCommand]
            ST[StatsCommand]
            HC[HelpCommand]
        end
    end

    subgraph "Dependencies"
        REPO[ITaskRepository]
        SESSION[IFocusSessionManagerService]
        WORKDAY[IWorkDayManagerService]
        TIMER[ITimerService]
        NOTIFY[INotificationService]
        SOUND[ISoundService]
        CONSOLE[ConsoleHelper]
    end

    ICommand --> AT
    ICommand --> FC
    ICommand --> BC
    ICommand --> DC
    ICommand --> EC
    ICommand --> DN
    ICommand --> PC
    ICommand --> TC
    ICommand --> CC
    ICommand --> CL
    ICommand --> CD
    ICommand --> SD
    ICommand --> ED
    ICommand --> WD
    ICommand --> UP
    ICommand --> ST
    ICommand --> HC

    AT --> REPO
    FC --> REPO
    FC --> SESSION
    BC --> REPO
    BC --> SESSION
    BC --> NOTIFY
    BC --> SOUND
    DC --> REPO
    EC --> REPO
    DN --> REPO
    DN --> SESSION
    PC --> REPO
    PC --> SESSION
    TC --> REPO
    TC --> TIMER
    TC --> NOTIFY
    TC --> SOUND
    CC --> REPO
    CC --> CONSOLE
    CL --> REPO
    CD --> REPO
    SD --> WORKDAY
    SD --> CONSOLE
    ED --> WORKDAY
    ED --> CONSOLE
    WD --> WORKDAY
    WD --> REPO
    WD --> CONSOLE
    UP --> REPO
    ST --> REPO
    ST --> CONSOLE
    HC --> CF

    %% Styling
    classDef commandInterface fill:#E6EBE0,stroke:#000000,stroke-width:2px,color:#000000
    classDef concreteCommands fill:#5CA4A9,stroke:#000000,stroke-width:2px,color:#000000
    classDef dependencies fill:#9BC1BC,stroke:#000000,stroke-width:2px,color:#000000

    class ICommand commandInterface
    class AT,FC,BC,DC,EC,DN,PC,TC,CC,CL,CD,SD,ED,WD,UP,ST,HC concreteCommands
    class REPO,SESSION,WORKDAY,TIMER,NOTIFY,SOUND,CONSOLE dependencies
```

## 🧪 Testing Architecture

```mermaid
graph TB
    subgraph "Test Suite (125+ Tests)"
        subgraph "Command Tests"
            ATT[AddTaskCommandTests]
            FCT[FocusCommandTests]
            BCT[BreakCommandTests]
            DCT[DeleteCommandTests]
            ECT[EditTaskCommandTests]
            DNT[DoneCommandTests]
            PCT[PauseCommandTests]
            TCT[TimerCommandTests]
            CCT[CheckCommandTests]
            CLT[ClearListCommandTests]
            CDT[ClearDoneCommandTests]
            SDT[StartDayCommandTests]
            EDT[EndDayCommandTests]
            WDT[WorkDayStatusCommandTests]
            UPT[UptimeCommandTests]
            STT[StatsCommandTests]
            HCT[HelpCommandTests]
        end
        
        subgraph "Service Tests"
            TMT[TaskManagerServiceTests]
            HRGT[HtmlReportGeneratorTests]
        end
    end

    subgraph "Test Dependencies"
        subgraph "Mocks"
            MREPO[Mock<ITaskRepository>]
            MSESSION[Mock<IFocusSessionManagerService>]
            MWORKDAY[Mock<IWorkDayManagerService>]
            MTIMER[Mock<ITimerService>]
            MNOTIFY[Mock<INotificationService>]
            MSOUND[Mock<ISoundService>]
            MCONSOLE[Mock<ConsoleHelper>]
        end
        
        subgraph "Test Framework"
            XUNIT[xUnit]
            MOQ[Moq]
            COVERLET[Coverlet.Collector]
        end
    end

    ATT --> MREPO
    FCT --> MREPO
    FCT --> MSESSION
    BCT --> MREPO
    BCT --> MSESSION
    BCT --> MNOTIFY
    BCT --> MSOUND
    DCT --> MREPO
    ECT --> MREPO
    DNT --> MREPO
    DNT --> MSESSION
    PCT --> MREPO
    PCT --> MSESSION
    TCT --> MREPO
    TCT --> MTIMER
    TCT --> MNOTIFY
    TCT --> MSOUND
    CCT --> MREPO
    CCT --> MCONSOLE
    CLT --> MREPO
    CDT --> MREPO
    SDT --> MWORKDAY
    SDT --> MCONSOLE
    EDT --> MWORKDAY
    EDT --> MCONSOLE
    WDT --> MWORKDAY
    WDT --> MREPO
    WDT --> MCONSOLE
    UPT --> MREPO
    STT --> MREPO
    STT --> MCONSOLE
    HCT --> MCF
    TMT --> MREPO
    TMT --> MCF
    TMT --> MSOUND
    TMT --> MCONSOLE

    XUNIT --> ATT
    XUNIT --> FCT
    XUNIT --> BCT
    XUNIT --> DCT
    XUNIT --> ECT
    XUNIT --> DNT
    XUNIT --> PCT
    XUNIT --> TCT
    XUNIT --> CCT
    XUNIT --> CLT
    XUNIT --> CDT
    XUNIT --> SDT
    XUNIT --> EDT
    XUNIT --> WDT
    XUNIT --> UPT
    XUNIT --> STT
    XUNIT --> HCT
    XUNIT --> TMT
    XUNIT --> HRGT

    MOQ --> MREPO
    MOQ --> MSESSION
    MOQ --> MWORKDAY
    MOQ --> MTIMER
    MOQ --> MNOTIFY
    MOQ --> MSOUND
    MOQ --> MCONSOLE
    MOQ --> MCF

    COVERLET --> XUNIT

    %% Styling
    classDef testSuite fill:#E6EBE0,stroke:#000000,stroke-width:2px,color:#000000
    classDef commandTests fill:#5CA4A9,stroke:#000000,stroke-width:2px,color:#000000
    classDef serviceTests fill:#5CA4A9,stroke:#000000,stroke-width:2px,color:#000000
    classDef mocks fill:#9BC1BC,stroke:#000000,stroke-width:2px,color:#000000
    classDef framework fill:#F4F1BB,stroke:#000000,stroke-width:2px,color:#000000

    class ATT,FCT,BCT,DCT,ECT,DNT,PCT,TCT,CCT,CLT,CDT,SDT,EDT,WDT,UPT,STT,HCT commandTests
    class TMT,HRGT serviceTests
    class MREPO,MSESSION,MWORKDAY,MTIMER,MNOTIFY,MSOUND,MCONSOLE mocks
    class XUNIT,MOQ,COVERLET framework
```

## 📁 Data Storage Architecture

```mermaid
graph TB
    subgraph "Excel File Structure (tasks.xlsx)"
        subgraph "Core Data Sheets"
            Tasks[📋 Tasks Sheet<br/>Task management data]
            Sessions[📊 Sessions Sheet<br/>Daily session tracking]
            WorkDays[📅 WorkDays Sheet<br/>Work day schedule]
            SessionLogs[📝 SessionLogs Sheet<br/>Detailed activity log<br/>including command executions]
        end
        
        subgraph "Configuration Sheets"
            Settings[⚙️ Settings Sheet<br/>Application configuration]
            UserInfo[👤 UserInfo Sheet<br/>System information]
        end
    end

    subgraph "Data Relationships"
        Tasks --> Sessions
        Sessions --> WorkDays
        Sessions --> SessionLogs
        WorkDays --> SessionLogs
    end

    subgraph "Backup System"
        Archive[📦 Archive Folder]
        DailyBackups[📅 Daily Backups<br/>YYYY-MM-DD format]
        TimestampedFiles[⏰ Timestamped Files<br/>HHMMSS format]
    end

    Excel --> Archive
    Archive --> DailyBackups
    DailyBackups --> TimestampedFiles

    %% Styling
    classDef excelFile fill:#E6EBE0,stroke:#000000,stroke-width:2px,color:#000000
    classDef coreData fill:#5CA4A9,stroke:#000000,stroke-width:2px,color:#000000
    classDef configData fill:#9BC1BC,stroke:#000000,stroke-width:2px,color:#000000
    classDef backupSystem fill:#F4F1BB,stroke:#000000,stroke-width:2px,color:#000000

    class Tasks,Sessions,WorkDays,SessionLogs coreData
    class Settings,UserInfo configData
    class Archive,DailyBackups,TimestampedFiles backupSystem
```

## 🔧 Dependency Injection Configuration

```mermaid
graph TB
    subgraph "DI Container Configuration"
        subgraph "Singleton Services"
            REPO[ITaskRepository → ExcelTaskRepository]
            FSM[IFocusSessionManagerService → FocusSessionManagerService]
            WDM[IWorkDayManagerService → WorkDayManagerService]
            TS[ITimerService → TimerService]
            NOTIFY[INotificationService → WindowsNotificationService]
            SOUND[ISoundService → WindowsSoundService]
            BS[BackupService]
            CH[ConsoleHelper]
            TM[TaskManagerService]
        end
        
        subgraph "Transient Services"
            CF[ICommandFactory → CommandFactory]
        end
    end

    subgraph "Service Dependencies"
        TM --> REPO
        TM --> CF
        TM --> SOUND
        TM --> CH
        
        FSM --> REPO
        FSM --> NOTIFY
        FSM --> SOUND
        
        WDM --> REPO
        WDM --> NOTIFY
        WDM --> SOUND
        WDM --> BS
        
        TS --> NOTIFY
        TS --> SOUND
        
        BS --> REPO
        
        CF --> REPO
        CF --> FSM
        CF --> WDM
        CF --> TS
        CF --> NOTIFY
        CF --> SOUND
        CF --> CH
    end

    %% Styling
    classDef diContainer fill:#E6EBE0,stroke:#000000,stroke-width:2px,color:#000000
    classDef singletonServices fill:#5CA4A9,stroke:#000000,stroke-width:2px,color:#000000
    classDef transientServices fill:#9BC1BC,stroke:#000000,stroke-width:2px,color:#000000
    classDef dependencies fill:#F4F1BB,stroke:#000000,stroke-width:2px,color:#000000

    class REPO,FSM,WDM,TS,NOTIFY,SOUND,BS,CH,TM singletonServices
    class CF transientServices
```

## 🎯 Key Architectural Principles

### 1. **Dependency Inversion Principle**
- All services depend on interfaces, not concrete implementations
- Enables easy mocking for unit testing
- Allows for future implementation swaps

### 2. **Single Responsibility Principle**
- Each command handles one specific operation
- Services have focused responsibilities
- Clear separation between data access and business logic

### 3. **Command Pattern**
- Encapsulates requests as objects
- Enables easy command history and undo functionality
- Provides consistent interface for all operations

### 4. **Repository Pattern**
- Abstracts data access logic
- Enables easy switching between data sources
- Centralizes data persistence concerns

### 5. **Interface Segregation**
- Services expose only necessary methods
- Commands depend only on required interfaces
- Reduces coupling between components

## 🚀 Benefits of This Architecture

### **Testability**
- Interface-based design enables comprehensive unit testing
- Mock-based testing with 125+ test cases
- High test coverage with coverlet.collector

### **Maintainability**
- Clear separation of concerns
- Easy to add new commands or modify existing ones
- Consistent patterns throughout the codebase

### **Extensibility**
- Easy to add new data sources (database, cloud storage)
- Simple to implement new notification systems
- Modular design allows for feature additions

### **Reliability**
- Comprehensive error handling
- Automatic backup system
- Graceful degradation for failures
- **User-friendly exit workflow**: Ensures users are prompted to end their workday and handle active tasks, reducing data loss and improving workflow continuity
- **Note:** The goodbye popup is only reliably shown on `!exit` or Ctrl+C, not when closing the console window with the X button due to OS limitations

## 📊 HTML Report Generator & Tooltip System

### Enhanced Tooltip Architecture

The HTML Report Generator includes a sophisticated tooltip system designed for optimal user experience:

```mermaid
graph TB
    subgraph "Tooltip System Components"
        subgraph "User Interaction"
            HOVER[Mouse Hover]
            CLICK[Mouse Click]
            MOVE[Mouse Move]
            LEAVE[Mouse Leave]
            ESCAPE[Escape Key]
        end
        
        subgraph "Tooltip Engine"
            POSITION[Position Calculator]
            BOUNDS[Viewport Bounds Checker]
            TIMEOUT[Delay Manager]
            CLEANUP[Cleanup Handler]
        end
        
        subgraph "Visual Components"
            ICON[Info Icon ℹ️]
            TOOLTIP[Black Tooltip Box]
            ARROW[Position Arrow]
            ANIMATION[Fade Animation]
        end
        
        subgraph "Positioning Logic"
            TOP[Top Position]
            BOTTOM[Bottom Position]
            LEFT[Left Position]
            RIGHT[Right Position]
            ADJUST[Auto Adjust]
        end
    end
    
    subgraph "Event Flow"
        HOVER --> TIMEOUT
        TIMEOUT --> POSITION
        MOVE --> POSITION
        POSITION --> BOUNDS
        BOUNDS --> ADJUST
        ADJUST --> TOOLTIP
        LEAVE --> CLEANUP
        CLICK --> CLEANUP
        ESCAPE --> CLEANUP
    end
    
    subgraph "Styling System"
        ICON --> TOOLTIP
        TOOLTIP --> ARROW
        TOOLTIP --> ANIMATION
    end
    
    %% Styling
    classDef interaction fill:#E6EBE0,stroke:#000000,stroke-width:2px,color:#000000
    classDef engine fill:#5CA4A9,stroke:#000000,stroke-width:2px,color:#000000
    classDef visual fill:#9BC1BC,stroke:#000000,stroke-width:2px,color:#000000
    classDef positioning fill:#F4F1BB,stroke:#000000,stroke-width:2px,color:#000000
    
    class HOVER,CLICK,MOVE,LEAVE,ESCAPE interaction
    class POSITION,BOUNDS,TIMEOUT,CLEANUP engine
    class ICON,TOOLTIP,ARROW,ANIMATION visual
    class TOP,BOTTOM,LEFT,RIGHT,ADJUST positioning
```

### Key Features

1. **Mouse-Tracked Positioning**
   - Tooltips follow mouse cursor position
   - Real-time position updates during mouse movement
   - Precise placement relative to information icons

2. **Smart Viewport Management**
   - Automatic detection of screen boundaries
   - Dynamic position adjustment to prevent clipping
   - Fallback positioning when primary position is unavailable

3. **Enhanced User Experience**
   - 300ms delay to prevent accidental triggers
   - Smooth fade-in/fade-out animations
   - Multiple interaction modes (hover, click, keyboard)

4. **Accessibility Features**
   - Keyboard navigation support (Escape key)
   - High contrast design (black background, white text)
   - Screen reader friendly information icons

5. **Performance Optimizations**
   - Efficient DOM manipulation
   - Proper event cleanup and memory management
   - Minimal reflows and repaints

### Technical Implementation

```javascript
// Core tooltip positioning algorithm
function showInfoTooltip(mouseX, mouseY, description) {
    // Calculate optimal position based on mouse coordinates
    let tooltipX = mouseX - (tooltipRect.width / 2);
    let tooltipY = mouseY - tooltipRect.height - 15;
    
    // Adjust for viewport bounds
    if (tooltipY < 10) tooltipY = mouseY + 15;
    if (tooltipX < 10) tooltipX = 10;
    if (tooltipX + tooltipRect.width > viewportWidth - 10) {
        tooltipX = viewportWidth - tooltipRect.width - 10;
    }
    
    // Apply position and show tooltip
    tooltip.style.left = tooltipX + 'px';
    tooltip.style.top = tooltipY + 'px';
}
```

### CSS Styling System

```css
/* Info icon styling */
.info-icon {
    display: inline-block;
    cursor: pointer;
    font-size: 0.8em;
    margin-left: 8px;
    color: #667eea;
    transition: all 0.3s ease;
    position: relative;
    vertical-align: middle;
}

/* Tooltip styling */
.info-tooltip {
    position: fixed;
    background: #000000;
    color: #ffffff;
    padding: 12px 16px;
    border-radius: 8px;
    font-size: 0.85em;
    max-width: 300px;
    white-space: normal;
    text-align: left;
    box-shadow: 0 6px 20px rgba(0, 0, 0, 0.5);
    z-index: 10000;
    opacity: 0;
    visibility: hidden;
    transition: all 0.3s ease;
    pointer-events: none;
    border: 1px solid #333333;
}
```

This architecture provides a solid foundation for a maintainable, testable, and extensible task management system while following industry best practices and design patterns. 