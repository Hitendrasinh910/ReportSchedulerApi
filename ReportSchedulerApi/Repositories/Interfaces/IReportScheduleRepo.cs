using Microsoft.AspNetCore.Mvc.RazorPages;
using ReportSchedulerApi.Models.Common;
using ReportSchedulerApi.Models.DTOs;

namespace ReportSchedulerApi.Repositories.Interfaces
{
    public interface IReportScheduleRepo
    {
        Task<IEnumerable<ReportScheduleDto>> GetAllAsync(string search = "", int pageNumber = 1, int pageSize = 10);

        Task<ReportScheduleDto?> GetByIdAsync(int scheduleId);

        Task<SaveResult> SaveAsync(ReportScheduleDto model);

        Task<SaveResult> DeleteAsync(int scheduleId, string deletedBy);

        Task<SaveResult> SetActiveAsync(
            int scheduleId,
            bool isActive,
            int? userActionBy);
    }
}
