using Hangfire;
using ReportSchedulerApi.Repositories.Interfaces;

namespace ReportSchedulerApi.Services
{
    public class ReportSchedulerJob
    {
        private readonly IScheduleExecutorRepository _scheduleExecutorRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReportSchedulerJob> _logger;
        private readonly SchedulerHealthState _health;

        public ReportSchedulerJob(
            IScheduleExecutorRepository scheduleExecutorRepository,
            IConfiguration configuration,
            ILogger<ReportSchedulerJob> logger,
            SchedulerHealthState health)
        {
            _scheduleExecutorRepository = scheduleExecutorRepository;
            _configuration = configuration;
            _logger = logger;
            _health = health;
        }

        // Only one sweep may run at a time across every Hangfire server
        // sharing this database -- a web garden, an overlapped app-pool
        // recycle, or a second node would otherwise run two sweeps at once.
        //
        // Retries are off because the sweep already repeats every minute:
        // Hangfire's default of 10 retries would re-enter a partially
        // completed sweep and re-notify recipients who were already messaged.
        [DisableConcurrentExecution(timeoutInSeconds: 300)]
        [AutomaticRetry(Attempts = 0)]
        public async Task ExecuteDueSchedulesAsync()
        {
            var enabled = _configuration.GetValue<bool>("SchedulerWorker:Enabled");

            if (!enabled)
            {
                _logger.LogInformation("Report scheduler Hangfire job is disabled.");
                return;
            }

            try
            {
                _logger.LogInformation("Report scheduler Hangfire job started at {Time}", DateTime.Now);

                await _scheduleExecutorRepository.ExecuteDueSchedulesAsync();

                _health.MarkJobRun();

                _logger.LogInformation("Report scheduler Hangfire job completed at {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Report scheduler Hangfire job failed.");

                throw;
            }
        }
    }
}
