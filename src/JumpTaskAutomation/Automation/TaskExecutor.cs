using System.Diagnostics;
using JumpTaskAutomation.Backend;
using JumpTaskAutomation.Logging;

namespace JumpTaskAutomation.Automation;

public sealed class TaskExecutor
{
    private readonly ADBController adb;
    private readonly UIAutomatorHelper uiHelper;
    private readonly IAutomationLogger logger;

    public TaskExecutor(ADBController adb, UIAutomatorHelper uiHelper, IAutomationLogger logger)
    {
        this.adb = adb;
        this.uiHelper = uiHelper;
        this.logger = logger;
    }

    public async Task<AutomationResult> ExecuteWatchVideoAsync(string appPackage, TimeSpan duration, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        await adb.LaunchAppAsync(appPackage, cancellationToken);
        logger.Info($"Launched video app: {appPackage}. Watching for {duration.TotalSeconds} seconds.");

        await Task.Delay(duration, cancellationToken);
        stopwatch.Stop();

        return new AutomationResult(true, $"Watched video for {duration.TotalSeconds} seconds.", stopwatch.Elapsed);
    }

    public async Task<AutomationResult> ExecuteAppReviewAsync(string packageName, int stars, string reviewText, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        await adb.OpenPlayStorePageAsync(packageName, cancellationToken);
        logger.Info($"Opened Play Store page for {packageName}. Stars requested: {stars}");

        // UI coordinates vary by device; capture UI dump for later mapping.
        await uiHelper.CaptureUiDumpAsync(Path.Combine("artifacts", $"{packageName}-review-ui.xml"), cancellationToken);

        // Stub for tapping star rating and typing review.
        await adb.PressBackAsync(cancellationToken);

        stopwatch.Stop();
        return new AutomationResult(true, "App review flow stub executed. Wire up taps based on the captured UI dump.", stopwatch.Elapsed);
    }

    public async Task<AutomationResult> ExecuteWebCheckAsync(string url, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        await adb.OpenUrlAsync(url, cancellationToken);
        logger.Info($"Opened URL on device: {url}");

        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        stopwatch.Stop();

        return new AutomationResult(true, $"Visited {url}", stopwatch.Elapsed);
    }
}
