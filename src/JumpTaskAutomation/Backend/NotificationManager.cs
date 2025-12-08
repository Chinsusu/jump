using JumpTaskAutomation.Logging;

namespace JumpTaskAutomation.Backend;

public sealed class NotificationManager
{
    private readonly IAutomationLogger logger;

    public NotificationManager(IAutomationLogger logger)
    {
        this.logger = logger;
    }

    public Task NotifyAsync(string taskName, AutomationResult result, CancellationToken cancellationToken)
    {
        var status = result.Success ? "success" : "failure";
        logger.Info($"Notify: {taskName} => {status} ({result.Notes})");
        return Task.CompletedTask;
    }
}
