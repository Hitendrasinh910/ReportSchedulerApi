namespace ReportSchedulerApi.Models.DTOs
{
    public class UserDto
    {
        public int IDUser { get; set; }

        public string? PersonName { get; set; }
        public string? UserType { get; set; }
        public string? ContactNo { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public int TotalCount { get; set; }

        // Use this like E_By in ReportScheduleDto
        public int? E_By { get; set; }
    }
}
