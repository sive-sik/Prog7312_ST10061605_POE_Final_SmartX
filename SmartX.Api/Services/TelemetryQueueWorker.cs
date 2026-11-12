namespace SmartX.Api.Services;

// Processes queued readings automatically while retaining the manual process-next endpoint.
public sealed class TelemetryQueueWorker : BackgroundService
{
    private readonly RealTimeProcessingStore store;
    private readonly ILogger<TelemetryQueueWorker> logger;
    public TelemetryQueueWorker(RealTimeProcessingStore store, ILogger<TelemetryQueueWorker> logger)
    {
        this.store = store;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Drain a bounded batch per tick; priority packets are processed first.
                for (int i = 0; i < 100 && store.ProcessNext() != null; i++) { }
            }
            catch (Exception error)
            {
                logger.LogError(error, "Automatic telemetry processing failed.");
            }
        }
    }
}
