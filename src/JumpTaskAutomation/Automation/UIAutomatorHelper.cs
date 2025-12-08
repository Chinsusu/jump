using JumpTaskAutomation.Logging;

namespace JumpTaskAutomation.Automation;

public sealed class UIAutomatorHelper
{
    private readonly ADBController adb;
    private readonly IAutomationLogger logger;

    public UIAutomatorHelper(ADBController adb, IAutomationLogger logger)
    {
        this.adb = adb;
        this.logger = logger;
    }

    public async Task<bool> CaptureUiDumpAsync(string localPath, CancellationToken cancellationToken)
    {
        const string remotePath = "/sdcard/window_dump.xml";
        var dumpResult = await adb.RunCommandAsync($"shell uiautomator dump {remotePath}", cancellationToken);
        if (dumpResult.ExitCode != 0)
        {
            logger.Warn($"ui automator dump failed: {dumpResult.Stderr}");
            return false;
        }

        var directory = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        var pullResult = await adb.RunCommandAsync($"pull {remotePath} \"{localPath}\"", cancellationToken);
        if (pullResult.ExitCode != 0)
        {
            logger.Warn($"Failed to pull UI dump: {pullResult.Stderr}");
            return false;
        }

        logger.Info($"UI hierarchy saved to {localPath}");
        return true;
    }
}
