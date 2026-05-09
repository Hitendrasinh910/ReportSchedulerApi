using Dapper;
using ReportSchedulerApi.Helpers;
using ReportSchedulerApi.Models.DTOs;
using ReportSchedulerApi.Repositories.Interfaces;
using System.Data;

namespace ReportSchedulerApi.Repositories.Services
{
    public class SchedulerLookupRepository : ISchedulerLookupRepository
    {
        private const string BillingDb = "AiraBillingDb";

        private readonly IDapperHelper _dapper;
        private readonly ILogger<SchedulerLookupRepository>? _logger;

        public SchedulerLookupRepository(
            IDapperHelper dapper,
            ILogger<SchedulerLookupRepository>? logger = null)
        {
            _dapper = dapper;
            _logger = logger;
        }

        public async Task<IEnumerable<UserLookupDto>> GetUsersAsync(
            string? search,
            string? userType,
            string? adminType)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("@Search", search);
                param.Add("@UserType", userType);
                param.Add("@AdminType", adminType);

                return await _dapper.QueryAsync<UserLookupDto>(
                    "usp_SchedulerLookup_User_Select",
                    param,
                    CommandType.StoredProcedure,
                    BillingDb);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading user lookup");
                return Enumerable.Empty<UserLookupDto>();
            }
        }

        public async Task<IEnumerable<PartyAccountLookupDto>> GetPartyAccountsAsync(
            string? search,
            string? partyType,
            string? branchType,
            string? dealerType)
        {
            try
            {
                var param = new DynamicParameters();
                param.Add("@Search", search);
                param.Add("@PartyType", partyType);
                param.Add("@BranchType", branchType);
                param.Add("@DealerType", dealerType);

                return await _dapper.QueryAsync<PartyAccountLookupDto>(
                    "usp_SchedulerLookup_PartyAccount_Select",
                    param,
                    CommandType.StoredProcedure,
                    BillingDb);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading party account lookup");
                return Enumerable.Empty<PartyAccountLookupDto>();
            }
        }
    }
}
