using System.Text.Json;
using JumpTaskAutomation.Logging;

namespace JumpTaskAutomation.Backend;

public sealed class DatabaseManager
{
    private readonly string databasePath;
    private readonly IAutomationLogger logger;
    private readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = false };

    public DatabaseManager(string databasePath, IAutomationLogger logger)
    {
        this.databasePath = databasePath;
        this.logger = logger;
    }

    public async Task AppendLogAsync(string taskName, AutomationResult result, CancellationToken cancellationToken)
    {
        var entry = new AutomationLogEntry(
            DateTimeOffset.UtcNow,
            taskName,
            result.Success,
            result.Notes,
            result.Duration?.TotalSeconds);

        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        var line = JsonSerializer.Serialize(entry, jsonOptions) + Environment.NewLine;
        await File.AppendAllTextAsync(databasePath, line, cancellationToken);
        logger.Info($"Logged task result: {taskName} ({(result.Success ? "success" : "failure")})");
    }

    private record AutomationLogEntry(
        DateTimeOffset Timestamp,
        string TaskName,
        bool Success,
        string? Notes,
        double? DurationSeconds);
}
