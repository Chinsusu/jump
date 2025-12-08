# JumpTask Automation

C# scaffold for automating Android tasks over ADB (watch video, open websites, stubbed review flow) with basic scheduling, logging, and notification hooks.

## Prerequisites
- Windows 10+ with .NET 8 SDK installed.
- Android platform-tools (adb) on `PATH` or set `ADB_PATH` to the adb executable.
- An Android device with developer options and USB debugging enabled.

## Quick start
```powershell
# From the repo root
adb devices          # verify the device is visible
dotnet build
dotnet run --project src/JumpTaskAutomation/JumpTaskAutomation.csproj
```

`Program.cs` queues two demo jobs (watch YouTube for 30 seconds and open a website). Edit the queue to add your own tasks or call the executor directly.

## Project layout
- `src/JumpTaskAutomation/Program.cs` wires up logging, adb, scheduler, and demo tasks.
- `src/JumpTaskAutomation/Automation/ADBController.cs` runs adb commands (tap, swipe, launch app, open URL).
- `src/JumpTaskAutomation/Automation/TaskExecutor.cs` high-level tasks (watch video, app review stub, web check).
- `src/JumpTaskAutomation/Automation/UIAutomatorHelper.cs` captures UI hierarchy dumps for mapping taps.
- `src/JumpTaskAutomation/Backend/TaskScheduler.cs` background queue using channels; logs and notifies per task.
- `src/JumpTaskAutomation/Backend/DatabaseManager.cs` appends newline-delimited JSON logs to `data/automation-log.ndjson`.
- `src/JumpTaskAutomation/Backend/NotificationManager.cs` notification hook (currently console only).
- `src/JumpTaskAutomation/Logging/ConsoleAutomationLogger.cs` simple console logger.

## Customization roadmap
- Map UI coordinates per device and flesh out the review flow (star taps, text input) using `UIAutomatorHelper`.
- Add persistence beyond NDJSON (SQLite/Firebase) inside `DatabaseManager`.
- Replace console notifications with email/SMS/webhooks in `NotificationManager`.
- Optionally add WPF/WinForms UI under `src/JumpTaskAutomation/Frontend` to manage tasks interactively.
