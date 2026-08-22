using Hangfire;

namespace ReportSchedulerApi.Services
{
    /// <summary>
    /// Registers the recurring sweep and keeps a heartbeat in the log.
    ///
    /// Registration used to sit inline in Program.cs before app.Run(), which
    /// made startup hard-depend on SQL Server: if the database was not
    /// reachable at the moment IIS started the app -- common after a server
    /// reboot, where IIS is often ready before SQL Server -- the call threw,
    /// the app failed to start with HTTP 500.30, and the scheduler never ran
    /// at all. Retrying here lets the app come up and connect once SQL is
    /// available.
    ///
    /// The heartbeat log line makes an app-pool death visible after the fact:
    /// the last timestamp in the log is when the process stopped existing.
    /// </summary>
    public class SchedulerBootstrapService : BackgroundService
    {
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMinutes(5);

        private readonly SchedulerHealthState _health;
        private readonly ILogger<SchedulerBootstrapService> _logger;

        public SchedulerBootstrapService(
            SchedulerHealthState health,
            ILogger<SchedulerBootstrapService> logger)
        {
            _health = health;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RegisterRecurringJobWithRetryAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _health.MarkHeartbeat();

                _logger.LogInformation(
                    "Scheduler heartbeat. ProcessStartedUtc={ProcessStartedUtc}, LastJobRunUtc={LastJobRunUtc}",
                    _health.ProcessStartedUtc,
                    _health.LastJobRunUtc);

                try
                {
                    await Task.Delay(HeartbeatInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogWarning(
                "Scheduler host is shutting down. Hangfire will stop dispatching until the application starts again.");
        }

        private async Task RegisterRecurringJobWithRetryAsync(CancellationToken stoppingToken)
        {
            var delay = TimeSpan.FromSeconds(5);
            var maxDelay = TimeSpan.FromMinutes(2);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    RecurringJob.AddOrUpdate<ReportSchedulerJob>(
                        "execute-due-report-schedules",
                        job => job.ExecuteDueSchedulesAsync(),
                        Cron.Minutely);

                    _health.RecurringJobRegistered = true;

                    _logger.LogInformation(
                        "Recurring job 'execute-due-report-schedules' registered.");

                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Could not register the recurring job (is SQL Server reachable?). Retrying in {Delay}.",
                        delay);

                    try
                    {
                        await Task.Delay(delay, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    delay = delay < maxDelay
                        ? TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelay.TotalSeconds))
                        : maxDelay;
                }
            }
        }
    }
}
