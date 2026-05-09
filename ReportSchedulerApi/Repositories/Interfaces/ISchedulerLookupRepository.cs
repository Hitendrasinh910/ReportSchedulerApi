using ReportSchedulerApi.Models.DTOs;

namespace ReportSchedulerApi.Repositories.Interfaces
{
    public interface ISchedulerLookupRepository
    {
        Task<IEnumerable<UserLookupDto>> GetUsersAsync(
                string? search,
                string? userType,
                string? adminType);

        Task<IEnumerable<PartyAccountLookupDto>> GetPartyAccountsAsync(
            string? search,
            string? partyType,
            string? branchType,
            string? dealerType);
    }
}
