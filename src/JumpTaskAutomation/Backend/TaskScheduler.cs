using System.Diagnostics;
using System.Threading.Channels;
using JumpTaskAutomation.Logging;

namespace JumpTaskAutomation.Backend;

public sealed class TaskSchedulerService : IAsyncDisposable
{
    private readonly Channel<AutomationJob> channel = Channel.CreateUnbounded<AutomationJob>();
    private readonly DatabaseManager database;
    private readonly NotificationManager notifier;
    private readonly IAutomationLogger logger;

    public TaskSchedulerService(DatabaseManager database, NotificationManager notifier, IAutomationLogger logger)
    {
        this.database = database;
        this.notifier = notifier;
        this.logger = logger;
    }

    public void Enqueue(AutomationJob job)
    {
        if (!channel.Writer.TryWrite(job))
        {
            throw new InvalidOperationException("Unable to queue task.");
        }

        logger.Info($"Enqueued: {job.Name}");
    }

    public void Complete() => channel.Writer.TryComplete();

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        await foreach (var job in channel.Reader.ReadAllAsync(cancellationToken))
        {
            AutomationResult result;
            var stopwatch = Stopwatch.StartNew();

            logger.Info($"Starting: {job.Name}");
            try
            {
                result = await job.ExecuteAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.Warn($"Task canceled: {job.Name}");
                throw;
            }
            catch (Exception ex)
            {
                logger.Error($"Task failed: {job.Name}", ex);
                result = new AutomationResult(false, ex.Message, stopwatch.Elapsed);
            }
            finally
            {
                stopwatch.Stop();
            }

            var completedResult = result.Duration is null
                ? result with { Duration = stopwatch.Elapsed }
                : result;

            await database.AppendLogAsync(job.Name, completedResult, cancellationToken);
            await notifier.NotifyAsync(job.Name, completedResult, cancellationToken);
        }
    }

    public ValueTask DisposeAsync()
    {
        channel.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}
