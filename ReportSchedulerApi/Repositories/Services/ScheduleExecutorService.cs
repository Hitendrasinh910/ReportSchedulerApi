using CNCMachineService.Helper;
using Cronos;
using Dapper;
using iTextSharp.text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ReportSchedulerApi.Helpers;
using ReportSchedulerApi.Models.Common;
using ReportSchedulerApi.Models.DTOs;
using ReportSchedulerApi.Repositories.Interfaces;
using ReportSchedulerApi.Repositories.Notification;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ReportSchedulerApi.Repositories.Services
{
    public class ScheduleExecutorService : IScheduleExecutorService
    {
        private const string SchedulerDb = "ReportSchedulerDb";
        private const string BillingDb = "AiraBillingDb";

        private readonly IDapperHelper _dapper;
        private readonly INotificationApiService _notificationApiService;
        private readonly ILogger<ScheduleExecutorService> _logger;

        public ScheduleExecutorService(
            IDapperHelper dapper,
            INotificationApiService notificationApiService,
            ILogger<ScheduleExecutorService> logger)
        {
            _dapper = dapper;
            _notificationApiService = notificationApiService;
            _logger = logger;
        }

        public async Task ExecuteDueSchedulesAsync()
        {
            var schedules = await _dapper.QueryAsync<ReportScheduleDto>(
                "usp_ReportSchedule_Active_Select",
                null,
                CommandType.StoredProcedure,
                SchedulerDb);

            foreach (var schedule in schedules)
            {
                try
                {
                    if (!IsScheduleDue(schedule))
                        continue;

                    await ExecuteScheduleInternalAsync(schedule);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Schedule execution failed. IDSchedule={IDSchedule}",
                        schedule.IDSchedule);

                    await SaveRunLogAsync(
                        schedule.IDSchedule,
                        schedule.ScheduleName,
                        "Failed",
                        ex.Message,
                        null,
                        DateTime.Now,
                        DateTime.Now);
                }
            }
        }

        public async Task ExecuteScheduleAsync(int idSchedule)
        {
            var param = new DynamicParameters();
            param.Add("@IDSchedule", idSchedule);

            var schedule = await _dapper.QueryFirstOrDefaultAsync<ReportScheduleDto>(
                "usp_ReportSchedule_SelectById",
                param,
                CommandType.StoredProcedure,
                SchedulerDb);

            if (schedule == null)
                throw new Exception("Schedule not found.");

            await ExecuteScheduleInternalAsync(schedule);
        }

        private bool IsScheduleDue(ReportScheduleDto schedule)
        {
            if (string.IsNullOrWhiteSpace(schedule.CronExpression))
                return false;

            try
            {
                var cron = CronExpression.Parse(
                    schedule.CronExpression,
                    CronFormat.IncludeSeconds);

                var now = DateTimeOffset.Now;
                var from = now.AddMinutes(-1);

                var next = cron.GetNextOccurrence(from, TimeZoneInfo.Local);

                if (next == null)
                    return false;

                return next.Value <= now;
            }
            catch
            {
                return false;
            }
        }

        private async Task ExecuteScheduleInternalAsync(ReportScheduleDto schedule)
        {
            var startedOn = DateTime.Now;

            await SaveRunLogAsync(
                schedule.IDSchedule,
                schedule.ScheduleName,
                "Started",
                "Schedule execution started.",
                null,
                startedOn,
                null);

            var parameters = await GetScheduleParametersAsync(schedule.IDSchedule);
            var recipients = await GetScheduleRecipientsAsync(schedule.IDSchedule);

            var spParams = BuildStoredProcedureParameters(schedule, parameters);

            var rows = await ExecuteConfiguredStoredProcedureAsync(
                schedule.StoredProcedureName,
                spParams);

            if (schedule.OutputType == "Message")
            {
                await SendMessageScheduleAsync(schedule, rows, recipients);
            }
            else if (schedule.OutputType == "Report")
            {
                await SendReportScheduleAsync(schedule, rows, recipients);
            }
            else
            {
                throw new Exception("Invalid OutputType.");
            }

            await SaveRunLogAsync(
                schedule.IDSchedule,
                schedule.ScheduleName,
                "Success",
                "Schedule executed successfully.",
                JsonConvert.SerializeObject(new
                {
                    RowCount = rows.Count,
                    schedule.OutputType
                }),
                startedOn,
                DateTime.Now);
        }

        private async Task<List<ReportScheduleParameterDto>> GetScheduleParametersAsync(int idSchedule)
        {
            var param = new DynamicParameters();
            param.Add("@IDSchedule", idSchedule);

            var data = await _dapper.QueryAsync<ReportScheduleParameterDto>(
                "usp_ReportScheduleParameter_SelectBySchedule",
                param,
                CommandType.StoredProcedure,
                SchedulerDb);

            return data.ToList();
        }

        private async Task<List<ReportScheduleRecipientDto>> GetScheduleRecipientsAsync(int idSchedule)
        {
            var param = new DynamicParameters();
            param.Add("@IDSchedule", idSchedule);

            var data = await _dapper.QueryAsync<ReportScheduleRecipientDto>(
                "usp_ReportScheduleRecipient_SelectBySchedule",
                param,
                CommandType.StoredProcedure,
                SchedulerDb);

            return data.ToList();
        }

        private DynamicParameters BuildStoredProcedureParameters(
            ReportScheduleDto schedule,
            List<ReportScheduleParameterDto> parameters)
        {
            var spParams = new DynamicParameters();
            var dateRange = ResolveDateRange(schedule.DateRangeType, schedule.CustomDays);

            foreach (var item in parameters)
            {
                object? value = null;

                if (item.ParameterType == "DateRangeFrom")
                    value = dateRange.FromDate;

                else if (item.ParameterType == "DateRangeTo")
                    value = dateRange.ToDate;

                else if (item.ParameterType == "Today")
                    value = DateTime.Today;

                else if (item.ParameterType == "Yesterday")
                    value = DateTime.Today.AddDays(-1);

                else if (item.ParameterType == "Static")
                    value = item.ParameterValue;

                spParams.Add(item.ParameterName, value);
            }

            return spParams;
        }

        private (DateTime FromDate, DateTime ToDate) ResolveDateRange(
    string? dateRangeType,
    int? customDays = null)
        {
            var today = DateTime.Today;

            switch (dateRangeType)
            {
                case "Today":
                    return (today, today);

                case "Last7Days":
                    return (today.AddDays(-6), today);

                case "Last30Days":
                    return (today.AddDays(-29), today);

                case "MonthDays":
                    var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                    return (firstDayOfMonth, today);

                case "CustomDays":
                    var days = customDays.GetValueOrDefault(1);

                    if (days <= 0)
                        days = 1;

                    return (today.AddDays(-(days - 1)), today);

                default:
                    return (today, today);
            }
        }

        private async Task<List<IDictionary<string, object>>> ExecuteConfiguredStoredProcedureAsync(
            string? storedProcedureName,
            DynamicParameters parameters)
        {
            if (string.IsNullOrWhiteSpace(storedProcedureName))
                throw new Exception("Stored procedure name is missing.");

            var result = await _dapper.QueryAsync<dynamic>(
                storedProcedureName,
                parameters,
                CommandType.StoredProcedure,
                BillingDb);

            return result
                .Select(row => (IDictionary<string, object>)row)
                .ToList();
        }

        private async Task SendMessageScheduleAsync(
    ReportScheduleDto schedule,
    List<IDictionary<string, object>> rows,
    List<ReportScheduleRecipientDto> recipients)
        {
            if (rows.Count == 0)
                throw new Exception("SP returned no rows.");

            var dateRange = ResolveDateRange(schedule.DateRangeType, schedule.CustomDays);

            var configuredPhones = await ResolveConfiguredRecipientPhonesAsync(schedule, recipients);
            configuredPhones = CleanPhones(configuredPhones);

            bool useSpPhones = ShouldUseSpMobileNumbers(schedule);

            // Use SP mobile numbers only for FromSP or Mixed
            if (useSpPhones &&
                 !string.IsNullOrWhiteSpace(schedule.MobileColumnName) &&
                 rows.Any(x => x.ContainsKey(schedule.MobileColumnName)))
            {
                // PER ROW MODE
                if (schedule.SpMobileMode == "PerRow")
                {
                    foreach (var row in rows)
                    {
                        var rowPhones = ResolvePhonesForSingleRow(schedule, row);
                        rowPhones = CleanPhones(rowPhones);

                        if (rowPhones.Count == 0)
                            continue;

                        var message = BuildMessageFromTemplate(
                            schedule.MessageTemplate,
                            new List<IDictionary<string, object>> { row },
                            dateRange.FromDate,
                            dateRange.ToDate);

                        await _notificationApiService.SendNotificationAsync(
                            subject: schedule.MessageSubject ?? schedule.ScheduleName ?? "Notification",
                            header: schedule.MessageHeader ?? schedule.MessageSubject ?? "",
                            message: message,
                            footer: "This is a system generated notification.",
                            userPhones: rowPhones,
                            category: "Atlas",
                            subCategory: "Message",
                            forceSend: true);
                    }

                    return;
                }

                // DISTINCT MODE
                var groupedRows = rows
                    .SelectMany(row =>
                    {
                        var phones = ResolvePhonesForSingleRow(schedule, row);

                        return phones.Select(phone => new
                        {
                            Phone = phone,
                            Row = row
                        });
                    })
                    .GroupBy(x => x.Phone);

                foreach (var group in groupedRows)
                {
                    var phone = group.Key;
                    var phoneRows = group.Select(x => x.Row).ToList();

                    var message = BuildMessageFromTemplate(
                        schedule.MessageTemplate,
                        phoneRows,
                        dateRange.FromDate,
                        dateRange.ToDate);

                    var sendPhones = CleanPhones(new List<string> { phone });

                    if (sendPhones.Count == 0)
                        continue;

                    await _notificationApiService.SendNotificationAsync(
                        subject: schedule.MessageSubject ?? schedule.ScheduleName ?? "Notification",
                        header: schedule.MessageHeader ?? schedule.MessageSubject ?? "",
                        message: message,
                        footer: "This is a system generated notification.",
                        userPhones: sendPhones,
                        category: "Atlas",
                        subCategory: "Message",
                        forceSend: true);
                }

                return;
            }

            // For Custom / User / PartyAccount, ignore SP ContactNo
            if (configuredPhones.Count == 0)
                throw new Exception("No configured recipient phone numbers found.");

            var commonMessage = BuildMessageFromTemplate(
                schedule.MessageTemplate,
                rows,
                dateRange.FromDate,
                dateRange.ToDate);

            await _notificationApiService.SendNotificationAsync(
                subject: schedule.MessageSubject ?? schedule.ScheduleName ?? "Notification",
                header: schedule.MessageHeader ?? schedule.MessageSubject ?? "",
                message: commonMessage,
                footer: "This is a system generated notification.",
                userPhones: configuredPhones,
                category: "Atlas",
                subCategory: "Message",
                forceSend: true);
        }

        private bool ShouldUseSpMobileNumbers(ReportScheduleDto schedule)
        {
            return schedule.ReceiverSource == "FromSP"
                || schedule.ReceiverSource == "Mixed";
        }

        private async Task SendReportScheduleAsync(
            ReportScheduleDto schedule,
            List<IDictionary<string, object>> rows,
            List<ReportScheduleRecipientDto> recipients)
        {
            if (rows.Count == 0)
                throw new Exception("SP returned no rows.");

            //var phonesFromSp = ResolvePhonesFromRows(schedule, rows);
            var configuredPhones = await ResolveConfiguredRecipientPhonesAsync(schedule, recipients);
            var allPhones = new List<string>();

            if (ShouldUseSpMobileNumbers(schedule))
            {
                var phonesFromSp = ResolvePhonesFromRows(schedule, rows);
                allPhones.AddRange(phonesFromSp);
            }

            allPhones.AddRange(configuredPhones);
            allPhones = CleanPhones(allPhones);

            if (allPhones.Count == 0)
                throw new Exception("No recipient phone numbers found.");

            var columns = BuildPdfColumns(rows);
            var pdfRows = BuildPdfRows(rows, columns);

            var dateRange = ResolveDateRange(schedule.DateRangeType, schedule.CustomDays);

            using var pdfStream = AtlasReportHelper.GenerateTableReport(
                schedule.PdfTitle ?? schedule.ScheduleName ?? "Report",
                dateRange.FromDate,
                dateRange.ToDate,
                columns,
                pdfRows);

            var pdfBytes = pdfStream.ToArray();
            var base64Pdf = Convert.ToBase64String(pdfBytes);

            var fileName = schedule.PdfFileName;

            if (string.IsNullOrWhiteSpace(fileName))
                fileName = $"{schedule.ScheduleName}_{DateTime.Now:yyyyMMddHHmm}.pdf";

            await _notificationApiService.SendNotificationAsync(
                subject: schedule.PdfTitle ?? schedule.ScheduleName ?? "Report", //schedule.MessageSubject
                header: schedule.PdfTitle ?? "Report", // schedule.MessageHeader
                message: schedule.MessageTemplate ?? "Please find attached report.",
                footer: "This is a system generated report.",
                userPhones: allPhones,
                category: "Atlas",
                subCategory: "Report",
                forceSend: true,
                documentName: fileName,
                mimeType: "application/pdf",
                base64Data: base64Pdf);
        }

        private string ApplyTemplate(
            string? template,
            IDictionary<string, object> row)
        {
            var message = template ?? "";

            foreach (var col in row)
            {
                var key = "{{" + col.Key + "}}";
                var value = col.Value?.ToString() ?? "";

                message = message.Replace(key, value);
            }

            return message;
        }

        private List<string> ResolvePhonesForSingleRow(
            ReportScheduleDto schedule,
            IDictionary<string, object> row)
        {
            var phones = new List<string>();

            if (string.IsNullOrWhiteSpace(schedule.MobileColumnName))
                return phones;

            if (!row.ContainsKey(schedule.MobileColumnName))
                return phones;

            var value = row[schedule.MobileColumnName]?.ToString();

            if (!string.IsNullOrWhiteSpace(value))
                phones.AddRange(SplitPhoneNumbers(value));

            return CleanPhones(phones);
        }

        private List<string> ResolvePhonesFromRows(
            ReportScheduleDto schedule,
            List<IDictionary<string, object>> rows)
        {
            var phones = new List<string>();

            if (string.IsNullOrWhiteSpace(schedule.MobileColumnName))
                return phones;

            foreach (var row in rows)
            {
                if (!row.ContainsKey(schedule.MobileColumnName))
                    continue;

                var value = row[schedule.MobileColumnName]?.ToString();

                if (!string.IsNullOrWhiteSpace(value))
                    phones.AddRange(SplitPhoneNumbers(value));
            }

            return CleanPhones(phones);
        }

        private async Task<List<string>> ResolveConfiguredRecipientPhonesAsync(
            ReportScheduleDto schedule,
            List<ReportScheduleRecipientDto> recipients)
        {
            var phones = new List<string>();

            if (schedule.ReceiverSource == "Custom" || schedule.ReceiverSource == "Mixed")
            {
                if (!string.IsNullOrWhiteSpace(schedule.CustomContactNos))
                    phones.AddRange(SplitPhoneNumbers(schedule.CustomContactNos));
            }

            if (schedule.ReceiverSource == "User" || schedule.ReceiverSource == "Mixed")
            {
                if (schedule.UserSelectionMode == "SelectedUsers")
                {
                    foreach (var r in recipients.Where(x => x.RecipientType == "User"))
                    {
                        var param = new DynamicParameters();
                        param.Add("@IDUser", r.IDReference);

                        var phone = await _dapper.QueryFirstOrDefaultAsync<string>(
                            "usp_SchedulerRuntime_UserPhone_Select",
                            param,
                            CommandType.StoredProcedure,
                            BillingDb);

                        if (!string.IsNullOrWhiteSpace(phone))
                            phones.Add(phone);
                    }
                }
                else
                {
                    var param = new DynamicParameters();
                    param.Add("@Search", null);
                    param.Add("@UserType", schedule.UserType);
                    param.Add("@AdminType", schedule.AdminType);

                    var users = await _dapper.QueryAsync<UserLookupDto>(
                        "usp_SchedulerLookup_User_Select",
                        param,
                        CommandType.StoredProcedure,
                        BillingDb);

                    phones.AddRange(users.Select(x => x.ContactNo ?? ""));
                }
            }

            if (schedule.ReceiverSource == "PartyAccount" || schedule.ReceiverSource == "Mixed")
            {
                if (schedule.PartySelectionMode == "SelectedParties")
                {
                    foreach (var r in recipients.Where(x => x.RecipientType == "PartyAccount"))
                    {
                        var param = new DynamicParameters();
                        param.Add("@IDPartyAccount", r.IDReference);

                        var phone = await _dapper.QueryFirstOrDefaultAsync<string>(
                            "usp_SchedulerRuntime_PartyPhone_Select",
                            param,
                            CommandType.StoredProcedure,
                            BillingDb);

                        if (!string.IsNullOrWhiteSpace(phone))
                            phones.Add(phone);
                    }
                }
                else
                {
                    var param = new DynamicParameters();
                    param.Add("@Search", null);
                    param.Add("@PartyType", schedule.PartyType);
                    param.Add("@BranchType", schedule.BranchType);
                    param.Add("@DealerType", schedule.DealerType);

                    var parties = await _dapper.QueryAsync<PartyAccountLookupDto>(
                        "usp_SchedulerLookup_PartyAccount_Select",
                        param,
                        CommandType.StoredProcedure,
                        BillingDb);

                    phones.AddRange(parties.Select(x => x.ContactNo ?? ""));
                }
            }

            return CleanPhones(phones);
        }

        private List<string> SplitPhoneNumbers(string value)
        {
            return value
                .Split(',', ';', '/', '|', '\n', '\r')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        private List<string> CleanPhones(List<string> phones)
        {
            return phones
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Where(x => x != "-")
                .Select(x => new string(x.Where(char.IsDigit).ToArray()))
                .Select(x =>
                {
                    // Remove India country code if 12 digits starts with 91
                    if (x.Length == 12 && x.StartsWith("91"))
                        return x.Substring(2);

                    return x;
                })
                .Where(x => x.Length == 10)
                .Distinct()
                .ToList();
        }

        private List<PdfColumn> BuildPdfColumns(List<IDictionary<string, object>> rows)
        {
            var firstRow = rows.First();

            return firstRow.Keys
                .Where(x => !string.Equals(x, "ContactNo", StringComparison.OrdinalIgnoreCase))
                .Select(x => new PdfColumn
                {
                    Header = x,
                    Width = 2f,
                    Alignment = Element.ALIGN_CENTER
                })
                .ToList();
        }

        private List<List<string>> BuildPdfRows(
            List<IDictionary<string, object>> rows,
            List<PdfColumn> columns)
        {
            var result = new List<List<string>>();

            foreach (var row in rows)
            {
                var values = new List<string>();

                foreach (var col in columns)
                {
                    values.Add(row.ContainsKey(col.Header)
                        ? row[col.Header]?.ToString() ?? ""
                        : "");
                }

                result.Add(values);
            }

            return result;
        }

        private async Task SaveRunLogAsync(
            int? idSchedule,
            string? scheduleName,
            string status,
            string? message,
            string? detailsJson,
            DateTime? startedOn,
            DateTime? completedOn)
        {
            var param = new DynamicParameters();

            param.Add("@IDSchedule", idSchedule);
            param.Add("@ScheduleName", scheduleName);
            param.Add("@Status", status);
            param.Add("@Message", message);
            param.Add("@DetailsJson", detailsJson);
            param.Add("@StartedOn", startedOn);
            param.Add("@CompletedOn", completedOn);

            await _dapper.QueryFirstOrDefaultAsync<SaveResult>(
                "usp_ReportScheduleRunLog_Save",
                param,
                CommandType.StoredProcedure,
                SchedulerDb);
        }

        private string BuildMessageFromTemplate(
            string? template,
            List<IDictionary<string, object>> rows,
            DateTime fromDate,
            DateTime toDate)
        {
            var message = template ?? "";

            message = message.Replace("{{FromDate}}", fromDate.ToString("dd-MM-yyyy"));
            message = message.Replace("{{ToDate}}", toDate.ToString("dd-MM-yyyy"));

            var rowsText = BuildRowsText(rows);
            message = message.Replace("{{Rows}}", rowsText);

            // Replace single-value placeholders from first row also.
            // Example: {{Branch}}, {{PartyName}}, {{InvoiceNo}}
            if (rows.Count > 0)
            {
                foreach (var col in rows[0])
                {
                    var key = "{{" + col.Key + "}}";
                    var value = col.Value?.ToString() ?? "";
                    message = message.Replace(key, value);
                }
            }

            // Final safety: remove any unreplaced {{Something}}
            message = Regex.Replace(message, @"\{\{.*?\}\}", "");
            message = CleanNotificationText(message);

            return message.Trim();
        }

        private string BuildRowsText(List<IDictionary<string, object>> rows)
        {
            var lines = new List<string>();

            int srNo = 1;

            foreach (var row in rows)
            {
                var parts = new List<string>();

                foreach (var col in row)
                {
                    if (string.Equals(col.Key, "ContactNo", StringComparison.OrdinalIgnoreCase))
                        continue;

                    parts.Add($"{col.Key}: {col.Value}");
                }

                lines.Add($"{srNo}. " + string.Join(", ", parts));
                srNo++;
            }

            return string.Join("\n", lines);
        }

        private string CleanNotificationText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var text = value;

            // Normalize line breaks
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");

            // Remove all non-ASCII characters like emoji, symbols, special icons
            //text = Regex.Replace(text, @"[^\u0009\u000A\u000D\u0020-\u007E]", "");

            // Remove markdown symbols if API does not support them
            text = text.Replace("*", "");

            // Remove unresolved template variables
            text = Regex.Replace(text, @"\{\{.*?\}\}", "");

            // Avoid too many blank lines
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            return text.Trim();
        }
    }
}
