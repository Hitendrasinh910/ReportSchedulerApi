namespace ReportSchedulerApi.Services
{
    /// <summary>
    /// Process-wide liveness record for the scheduler.
    ///
    /// Exists so a dead scheduler is externally detectable. When IIS recycles
    /// the app pool and the app does not come back, the site still answers
    /// (or fails to) -- but nothing previously revealed that the Hangfire
    /// server had stopped dispatching. The watchdog polls /health/scheduler
    /// and compares LastJobRunUtc against now.
    /// </summary>
    public class SchedulerHealthState
    {
        private long _lastJobRunUtcTicks;
        private long _lastHeartbeatUtcTicks;

        public DateTime ProcessStartedUtc { get; } = DateTime.UtcNow;

        public bool RecurringJobRegistered { get; set; }

        /// <summary>Last time the Hangfire sweep actually executed.</summary>
        public DateTime? LastJobRunUtc =>
            Interlocked.Read(ref _lastJobRunUtcTicks) == 0
                ? null
                : new DateTime(Interlocked.Read(ref _lastJobRunUtcTicks), DateTimeKind.Utc);

        /// <summary>Last time the in-process heartbeat loop ticked.</summary>
        public DateTime? LastHeartbeatUtc =>
            Interlocked.Read(ref _lastHeartbeatUtcTicks) == 0
                ? null
                : new DateTime(Interlocked.Read(ref _lastHeartbeatUtcTicks), DateTimeKind.Utc);

        public void MarkJobRun() =>
            Interlocked.Exchange(ref _lastJobRunUtcTicks, DateTime.UtcNow.Ticks);

        public void MarkHeartbeat() =>
            Interlocked.Exchange(ref _lastHeartbeatUtcTicks, DateTime.UtcNow.Ticks);
    }
}
