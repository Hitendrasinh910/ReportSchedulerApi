namespace ReportSchedulerApi.Repositories.Interfaces
{
    public interface IScheduleExecutorRepository
    {
        Task ExecuteDueSchedulesAsync();
        Task ExecuteScheduleAsync(int idSchedule);
    }
}
