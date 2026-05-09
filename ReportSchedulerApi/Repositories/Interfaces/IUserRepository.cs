using ReportSchedulerApi.Models.Common;
using ReportSchedulerApi.Models.DTOs;

namespace ReportSchedulerApi.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<UserDto>> GetAllAsync(
            string search = "",
            int pageNumber = 1,
            int pageSize = 10);

        Task<UserDto?> GetByIdAsync(int userId);

        Task<SaveResult> SaveAsync(UserDto model);

        Task<SaveResult> DeleteAsync(int userId, string deletedBy);
    }
}
