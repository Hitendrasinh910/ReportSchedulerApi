using Dapper;
using ReportSchedulerApi.Helpers;
using ReportSchedulerApi.Models.Common;
using ReportSchedulerApi.Models.DTOs;
using ReportSchedulerApi.Repositories.Interfaces;
using System.Data;

namespace ReportSchedulerApi.Repositories.Services
{
    public class UserRepository: IUserRepository
    {
        private const string SchedulerDb = "ReportSchedulerDb";

        private readonly IDapperHelper _dapper;
        private readonly ILogger<UserRepository>? _logger;

        public UserRepository(
            IDapperHelper dapperHelper,
            ILogger<UserRepository>? logger = null)
        {
            _dapper = dapperHelper;
            _logger = logger;
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync(
            string search = "",
            int pageNumber = 1,
            int pageSize = 10)
        {
            try
            {
                var parameters = new DynamicParameters();

                parameters.Add("@Search", search);
                parameters.Add("@PageNumber", pageNumber);
                parameters.Add("@PageSize", pageSize);

                return await _dapper.QueryAsync<UserDto>(
                    "usp_User_SelectAll",
                    parameters,
                    CommandType.StoredProcedure,
                    SchedulerDb);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in UserRepository.GetAllAsync");
                return Enumerable.Empty<UserDto>();
            }
        }

        public async Task<UserDto?> GetByIdAsync(int userId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@IDUser", userId);

                return await _dapper.QueryFirstOrDefaultAsync<UserDto>(
                    "usp_User_SelectById",
                    parameters,
                    CommandType.StoredProcedure,
                    SchedulerDb);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error in UserRepository.GetByIdAsync | IDUser={IDUser}",
                    userId);

                return null;
            }
        }

        public async Task<SaveResult> SaveAsync(UserDto model)
        {
            try
            {
                var parameters = new DynamicParameters();

                parameters.Add("@IDUser", model.IDUser);
                parameters.Add("@PersonName", model.PersonName);
                parameters.Add("@UserType", model.UserType);
                parameters.Add("@ContactNo", model.ContactNo);
                parameters.Add("@Username", model.Username);
                parameters.Add("@Password", model.Password);
                parameters.Add("@UserActionBy", model.E_By);

                var result = await _dapper.QueryFirstOrDefaultAsync<SaveResult>(
                    "usp_User_Save",
                    parameters,
                    CommandType.StoredProcedure,
                    SchedulerDb);

                return result ?? SaveResult.Fail("No response from database.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in UserRepository.SaveAsync");
                return SaveResult.Fail("Failed to save user. " + ex.Message);
            }
        }

        public async Task<SaveResult> DeleteAsync(int userId, string deletedBy)
        {
            try
            {
                var parameters = new DynamicParameters();

                parameters.Add("@IDUser", userId);
                parameters.Add("@DeletedBy", deletedBy);

                var result = await _dapper.QueryFirstOrDefaultAsync<SaveResult>(
                    "usp_User_Delete",
                    parameters,
                    CommandType.StoredProcedure,
                    SchedulerDb);

                return result ?? SaveResult.Fail("No response from database.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error in UserRepository.DeleteAsync | IDUser={IDUser}",
                    userId);

                return SaveResult.Fail("Failed to delete user. " + ex.Message);
            }
        }

        public async Task<UserDto?> ValidateLoginAsync(string username, string password)
        {
            try
            {
                var parameters = new DynamicParameters();

                parameters.Add("@Username", username);
                parameters.Add("@Password", password);

                return await _dapper.QueryFirstOrDefaultAsync<UserDto>(
                    "usp_User_ValidateLogin",
                    parameters,
                    CommandType.StoredProcedure,
                    SchedulerDb);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in UserRepository.ValidateLoginAsync");
                return null;
            }
        }
    }
}
