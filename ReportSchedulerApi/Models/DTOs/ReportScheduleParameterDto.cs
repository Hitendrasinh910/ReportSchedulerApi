namespace ReportSchedulerApi.Models.DTOs
{
    public class ReportScheduleParameterDto
    {
        public int IDParameter { get; set; }
        public int IDSchedule { get; set; }

        public string ParameterName { get; set; }
        public string ParameterType { get; set; }
        public string ParameterValue { get; set; }

        public int SortOrder { get; set; }
    }
}
