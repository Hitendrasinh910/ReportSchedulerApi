using ReportSchedulerApi.Repositories.Interfaces;

namespace ReportSchedulerApi.Services
{
    public class ReportSchedulerJob
    {
        private readonly IScheduleExecutorRepository _scheduleExecutorRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReportSchedulerJob> _logger;

        public ReportSchedulerJob(
            IScheduleExecutorRepository scheduleExecutorRepository,
            IConfiguration configuration,
            ILogger<ReportSchedulerJob> logger)
        {
            _scheduleExecutorRepository = scheduleExecutorRepository;
            _configuration = configuration;
            _logger = logger;
        }

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
