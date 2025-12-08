using JumpTaskAutomation.Automation;
using JumpTaskAutomation.Backend;
using JumpTaskAutomation.Logging;

var logger = new ConsoleAutomationLogger();
var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, args) =>
{
    args.Cancel = true;
    cancellation.Cancel();
    logger.Warn("Cancellation requested. Finishing current task...");
};

var adbPath = Environment.GetEnvironmentVariable("ADB_PATH") ?? "adb";
var adb = new ADBController(adbPath, logger);

if (!await adb.CheckAdbAvailableAsync(cancellation.Token))
{
    logger.Error("ADB is not available. Install platform-tools and ensure adb is on PATH or set ADB_PATH.");
    return;
}

if (!await adb.EnsureDeviceReadyAsync(cancellation.Token))
{
    logger.Error("No Android device is connected or authorized for adb.");
    return;
}

var uiHelper = new UIAutomatorHelper(adb, logger);
var executor = new TaskExecutor(adb, uiHelper, logger);
var database = new DatabaseManager(Path.Combine("data", "automation-log.ndjson"), logger);
var notifier = new NotificationManager(logger);

await using var scheduler = new TaskSchedulerService(database, notifier, logger);

logger.Info("Queueing demo tasks. Update Program.cs to enqueue your own automation flows.");

scheduler.Enqueue(new AutomationJob(
    "Watch demo video",
    ct => executor.ExecuteWatchVideoAsync("com.google.android.youtube", TimeSpan.FromSeconds(30), ct)));

scheduler.Enqueue(new AutomationJob(
    "Open example website",
    ct => executor.ExecuteWebCheckAsync("https://example.org", ct)));

scheduler.Complete();

try
{
    await scheduler.ProcessAsync(cancellation.Token);
}
catch (OperationCanceledException)
{
    logger.Warn("Automation canceled by user.");
}
