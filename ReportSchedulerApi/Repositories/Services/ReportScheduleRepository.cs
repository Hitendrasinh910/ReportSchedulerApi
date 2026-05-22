using Dapper;
using ReportSchedulerApi.Helpers;
using ReportSchedulerApi.Models.Common;
using ReportSchedulerApi.Models.DTOs;
using ReportSchedulerApi.Repositories.Interfaces;
using System.Data;

public class ReportScheduleRepository : IReportScheduleRepository
{
    private const string SchedulerDb = "ReportSchedulerDb";

    private readonly IDapperHelper _dapper;
    private readonly ILogger<ReportScheduleRepository>? _logger;

    public ReportScheduleRepository(
        IDapperHelper dapperHelper,
        ILogger<ReportScheduleRepository>? logger = null)
    {
        _dapper = dapperHelper;
        _logger = logger;
    }

    public async Task<IEnumerable<ReportScheduleDto>> GetAllAsync(string search = "", int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            var parameters = new DynamicParameters();

            parameters.Add("@Search", search);
            parameters.Add("@PageNumber", pageNumber);
            parameters.Add("@PageSize", pageSize);

            return await _dapper.QueryAsync<ReportScheduleDto>(
                "usp_ReportSchedule_SelectAll",
                parameters,
                CommandType.StoredProcedure,
                SchedulerDb);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in ReportScheduleRepository.GetAllAsync");
            return Enumerable.Empty<ReportScheduleDto>();
        }
    }

    public async Task<ReportScheduleDto?> GetByIdAsync(int scheduleId)
    {
        try
        {
            var param = new DynamicParameters();
            param.Add("@IDSchedule", scheduleId);

            var schedule = await _dapper.QueryFirstOrDefaultAsync<ReportScheduleDto>(
                "usp_ReportSchedule_SelectById",
                param,
                CommandType.StoredProcedure,
                SchedulerDb);

            if (schedule == null)
                return null;

            schedule.Parameters = (await GetParametersAsync(scheduleId)).ToList();
            schedule.Recipients = (await GetRecipientsAsync(scheduleId)).ToList();

            return schedule;
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Error in ReportScheduleRepository.GetByIdAsync | IDSchedule={IDSchedule}",
                scheduleId);

            return null;
        }
    }

    public async Task<SaveResult> SaveAsync(ReportScheduleDto model)
    {
        try
        {
            var param = new DynamicParameters();

            param.Add("@IDSchedule", model.IDSchedule);

            param.Add("@ScheduleName", model.ScheduleName);
            param.Add("@Description", model.Description);

            param.Add("@OutputType", model.OutputType);
            param.Add("@StoredProcedureName", model.StoredProcedureName);

            param.Add("@MessageSubject", model.MessageSubject);
            param.Add("@MessageHeader", model.MessageHeader);
            param.Add("@MessageTemplate", model.MessageTemplate);

            param.Add("@PdfTitle", model.PdfTitle);
            param.Add("@PdfFileName", model.PdfFileName);

            param.Add("@DateRangeType", model.DateRangeType);
            param.Add("@CustomDays", model.CustomDays);

            param.Add("@ReceiverSource", model.ReceiverSource);

            param.Add("@UserSelectionMode", model.UserSelectionMode);
            param.Add("@AdminType", model.AdminType);
            param.Add("@UserType", model.UserType);

            param.Add("@PartySelectionMode", model.PartySelectionMode);
            param.Add("@PartyType", model.PartyType);
            param.Add("@BranchType", model.BranchType);
            param.Add("@DealerType", model.DealerType);

            param.Add("@CustomContactNos", model.CustomContactNos);

            param.Add("@MobileColumnName", model.MobileColumnName);
            param.Add("@SpMobileMode", model.SpMobileMode);

            param.Add("@Frequency", model.Frequency);
            param.Add("@RunTime", model.RunTime);
            param.Add("@WeekDays", model.WeekDays);
            param.Add("@MonthDay", model.MonthDay);

            param.Add("@CronMode", model.CronMode);
            param.Add("@CronExpression", model.CronExpression);

            param.Add("@IsActive", model.IsActive);
            param.Add("@UserActionBy", model.E_By);

            var result = await _dapper.QueryFirstOrDefaultAsync<SaveResult>(
                "usp_ReportSchedule_Save",
                param,
                CommandType.StoredProcedure,
                SchedulerDb);

            if (result == null)
                return SaveResult.Fail("No response from database.");

            if (!result.IsSuccess)
                return result;

            var scheduleId = result.Id;

            await SaveParametersAsync(scheduleId, model.Parameters);
            await SaveRecipientsAsync(scheduleId, model.Recipients);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in ReportScheduleRepository.SaveAsync");
            return SaveResult.Fail("Failed to save report schedule. " + ex.Message);
        }
    }

    public async Task<SaveResult> DeleteAsync(int scheduleId, string deletedBy)
    {
        try
        {
            var param = new DynamicParameters();
            param.Add("@IDSchedule", scheduleId);
            param.Add("@DeletedBy", deletedBy);

            var result = await _dapper.QueryFirstOrDefaultAsync<SaveResult>(
                "usp_ReportSchedule_Delete",
                param,
                CommandType.StoredProcedure,
                SchedulerDb);

            return result ?? SaveResult.Fail("No response from database.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Error in ReportScheduleRepository.DeleteAsync | IDSchedule={IDSchedule}",
                scheduleId);

            return SaveResult.Fail("Failed to delete schedule. " + ex.Message);
        }
    }

    public async Task<SaveResult> SetActiveAsync(
        int scheduleId,
        bool isActive,
        int? userActionBy)
    {
        try
        {
            var param = new DynamicParameters();
            param.Add("@IDSchedule", scheduleId);
            param.Add("@IsActive", isActive);
            param.Add("@UserActionBy", userActionBy);

            var result = await _dapper.QueryFirstOrDefaultAsync<SaveResult>(
                "usp_ReportSchedule_SetActive",
                param,
                CommandType.StoredProcedure,
                SchedulerDb);

            return result ?? SaveResult.Fail("No response from database.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Error in ReportScheduleRepository.SetActiveAsync | IDSchedule={IDSchedule}",
                scheduleId);

            return SaveResult.Fail("Failed to update schedule status. " + ex.Message);
        }
    }

    private async Task<IEnumerable<ReportScheduleParameterDto>> GetParametersAsync(
        int scheduleId)
    {
        try
        {
            var param = new DynamicParameters();
            param.Add("@IDSchedule", scheduleId);

            return await _dapper.QueryAsync<ReportScheduleParameterDto>(
                "usp_ReportScheduleParameter_SelectBySchedule",
                param,
                CommandType.StoredProcedure,
                SchedulerDb);
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Error in ReportScheduleRepository.GetParametersAsync | IDSchedule={IDSchedule}",
                scheduleId);

            return Enumerable.Empty<ReportScheduleParameterDto>();
        }
    }

    private async Task<IEnumerable<ReportScheduleRecipientDto>> GetRecipientsAsync(
        int scheduleId)
    {
        try
        {
            var param = new DynamicParameters();
            param.Add("@IDSchedule", scheduleId);

            return await _dapper.QueryAsync<ReportScheduleRecipientDto>(
                "usp_ReportScheduleRecipient_SelectBySchedule",
                param,
                CommandType.StoredProcedure,
                SchedulerDb);
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Error in ReportScheduleRepository.GetRecipientsAsync | IDSchedule={IDSchedule}",
                scheduleId);

            return Enumerable.Empty<ReportScheduleRecipientDto>();
        }
    }

    private async Task SaveParametersAsync(
        int scheduleId,
        List<ReportScheduleParameterDto>? parameters)
    {
        try
        {
            var deleteParam = new DynamicParameters();
            deleteParam.Add("@IDSchedule", scheduleId);

            await _dapper.ExecuteAsync(
                "usp_ReportScheduleParameter_DeleteBySchedule",
                deleteParam,
                CommandType.StoredProcedure,
                SchedulerDb);

            if (parameters == null || parameters.Count == 0)
                return;

            for (int i = 0; i < parameters.Count; i++)
            {
                var item = parameters[i];

                var param = new DynamicParameters();
                param.Add("@IDSchedule", scheduleId);
                param.Add("@ParameterName", item.ParameterName);
                param.Add("@ParameterType", item.ParameterType);
                param.Add("@ParameterValue", item.ParameterValue);
                param.Add("@SortOrder", i);

                await _dapper.ExecuteAsync(
                    "usp_ReportScheduleParameter_Save",
                    param,
                    CommandType.StoredProcedure,
                    SchedulerDb);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Error in ReportScheduleRepository.SaveParametersAsync | IDSchedule={IDSchedule}",
                scheduleId);

            throw;
        }
    }

    private async Task SaveRecipientsAsync(
        int scheduleId,
        List<ReportScheduleRecipientDto>? recipients)
    {
        try
        {
            var deleteParam = new DynamicParameters();
            deleteParam.Add("@IDSchedule", scheduleId);

            await _dapper.ExecuteAsync(
                "usp_ReportScheduleRecipient_DeleteBySchedule",
                deleteParam,
                CommandType.StoredProcedure,
                SchedulerDb);

            if (recipients == null || recipients.Count == 0)
                return;

            foreach (var item in recipients)
            {
                var param = new DynamicParameters();
                param.Add("@IDSchedule", scheduleId);
                param.Add("@RecipientType", item.RecipientType);
                param.Add("@IDReference", item.IDReference);

                await _dapper.ExecuteAsync(
                    "usp_ReportScheduleRecipient_Save",
                    param,
                    CommandType.StoredProcedure,
                    SchedulerDb);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                ex,
                "Error in ReportScheduleRepository.SaveRecipientsAsync | IDSchedule={IDSchedule}",
                scheduleId);

            throw;
        }
    }
}