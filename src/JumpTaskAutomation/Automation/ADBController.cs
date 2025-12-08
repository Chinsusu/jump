using System.Diagnostics;
using System.Text;
using JumpTaskAutomation.Logging;

namespace JumpTaskAutomation.Automation;

public sealed class ADBController
{
    private readonly string adbPath;
    private readonly IAutomationLogger logger;

    public ADBController(string adbPath, IAutomationLogger logger)
    {
        this.adbPath = adbPath;
        this.logger = logger;
    }

    public async Task<bool> CheckAdbAvailableAsync(CancellationToken cancellationToken)
    {
        var result = await RunCommandAsync("version", cancellationToken);
        if (result.ExitCode != 0)
        {
            logger.Error($"adb not found or failed: {result.Stderr}");
            return false;
        }

        logger.Info($"adb detected: {result.Stdout.Trim()}");
        return true;
    }

    public async Task<bool> EnsureDeviceReadyAsync(CancellationToken cancellationToken)
    {
        var devices = await ListConnectedDevicesAsync(cancellationToken);
        if (devices.Count == 0)
        {
            logger.Warn("No connected devices returned by 'adb devices'.");
            return false;
        }

        logger.Info($"Connected device: {devices[0]}");
        return true;
    }

    public async Task<IReadOnlyList<string>> ListConnectedDevicesAsync(CancellationToken cancellationToken)
    {
        var result = await RunCommandAsync("devices", cancellationToken);
        if (result.ExitCode != 0)
        {
            logger.Error($"Failed to list devices: {result.Stderr}");
            return Array.Empty<string>();
        }

        var devices = result.Stdout
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(line => line.Trim())
            .Where(line => line.EndsWith("\tdevice", StringComparison.OrdinalIgnoreCase))
            .Select(line => line.Split('\t')[0])
            .ToList();

        return devices;
    }

    public Task<AdbCommandResult> TapAsync(int x, int y, CancellationToken cancellationToken) =>
        RunCommandAsync($"shell input tap {x} {y}", cancellationToken);

    public Task<AdbCommandResult> SwipeAsync(int startX, int startY, int endX, int endY, int durationMs, CancellationToken cancellationToken) =>
        RunCommandAsync($"shell input swipe {startX} {startY} {endX} {endY} {durationMs}", cancellationToken);

    public Task<AdbCommandResult> InputTextAsync(string text, CancellationToken cancellationToken)
    {
        var escaped = text.Replace(" ", "%s", StringComparison.Ordinal);
        return RunCommandAsync($"shell input text \"{escaped}\"", cancellationToken);
    }

    public Task<AdbCommandResult> LaunchAppAsync(string packageName, CancellationToken cancellationToken) =>
        RunCommandAsync($"shell monkey -p {packageName} -c android.intent.category.LAUNCHER 1", cancellationToken);

    public Task<AdbCommandResult> OpenUrlAsync(string url, CancellationToken cancellationToken) =>
        RunCommandAsync($"shell am start -a android.intent.action.VIEW -d \"{url}\"", cancellationToken);

    public Task<AdbCommandResult> OpenPlayStorePageAsync(string packageName, CancellationToken cancellationToken) =>
        RunCommandAsync($"shell am start -a android.intent.action.VIEW -d \"market://details?id={packageName}\"", cancellationToken);

    public Task<AdbCommandResult> PressBackAsync(CancellationToken cancellationToken) =>
        RunCommandAsync("shell input keyevent 4", cancellationToken);

    public async Task<AdbCommandResult> RunCommandAsync(string arguments, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = adbPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            return new AdbCommandResult(process.ExitCode, stdout, stderr);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to run adb command: adb {arguments}", ex);
            return new AdbCommandResult(-1, string.Empty, ex.Message);
        }
    }
}

public record AdbCommandResult(int ExitCode, string Stdout, string Stderr);
