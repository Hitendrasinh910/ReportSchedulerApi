namespace ReportSchedulerApi.Models.DTOs
{
    public class ReportScheduleDto
    {
        public int IDSchedule { get; set; }

        public string ScheduleName { get; set; }
        public string Description { get; set; }

        public string OutputType { get; set; }
        // Message / Report

        public string StoredProcedureName { get; set; }

        public string MessageSubject { get; set; }
        public string MessageHeader { get; set; }
        public string MessageTemplate { get; set; }

        public string PdfTitle { get; set; }
        public string PdfFileName { get; set; }

        public string DateRangeType { get; set; }
        public int? CustomDays { get; set; }

        public string ReceiverSource { get; set; }

        public string UserSelectionMode { get; set; }
        public string AdminType { get; set; }
        public string UserType { get; set; }

        public string PartySelectionMode { get; set; }
        public string PartyType { get; set; }
        public string BranchType { get; set; }
        public string DealerType { get; set; }

        public string CustomContactNos { get; set; }

        public string MobileColumnName { get; set; }
        public string SpMobileMode { get; set; }

        public string Frequency { get; set; }
        public TimeSpan? RunTime { get; set; }
        public string WeekDays { get; set; }
        public int? MonthDay { get; set; }

        public string CronMode { get; set; }
        public string CronExpression { get; set; }

        public bool IsActive { get; set; }
        public int? E_By { get; set; }

        public int TotalCount { get; set; } // for Pagination

        public List<ReportScheduleParameterDto> Parameters { get; set; } = new();
        public List<ReportScheduleRecipientDto> Recipients { get; set; } = new();
    }
}
