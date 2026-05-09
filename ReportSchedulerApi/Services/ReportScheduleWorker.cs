using ReportSchedulerApi.Repositories.Interfaces;

namespace ReportSchedulerApi.Services
{
    public class ReportScheduleWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReportScheduleWorker> _logger;

        public ReportScheduleWorker(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<ReportScheduleWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var enabled = _configuration.GetValue<bool>("SchedulerWorker:Enabled");
            var checkEverySeconds = _configuration.GetValue<int>("SchedulerWorker:CheckEverySeconds");

            if (checkEverySeconds <= 0)
                checkEverySeconds = 60;

            if (!enabled)
            {
                _logger.LogInformation("Report scheduler worker is disabled.");
                return;
            }

            _logger.LogInformation("Report scheduler worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var executor = scope.ServiceProvider
                        .GetRequiredService<IScheduleExecutorService>();

                    await executor.ExecuteDueSchedulesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Report scheduler worker failed.");
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(checkEverySeconds),
                    stoppingToken);
            }
        }
    }

}
