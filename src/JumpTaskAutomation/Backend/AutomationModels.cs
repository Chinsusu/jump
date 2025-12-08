namespace JumpTaskAutomation.Backend;

public record AutomationResult(bool Success, string? Notes = null, TimeSpan? Duration = null);

public record AutomationJob(string Name, Func<CancellationToken, Task<AutomationResult>> ExecuteAsync);
