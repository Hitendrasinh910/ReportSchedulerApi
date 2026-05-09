namespace ReportSchedulerApi.Repositories.Interfaces
{
    public interface IScheduleExecutorService
    {
        Task ExecuteDueSchedulesAsync();
        Task ExecuteScheduleAsync(int idSchedule);
    }
}
