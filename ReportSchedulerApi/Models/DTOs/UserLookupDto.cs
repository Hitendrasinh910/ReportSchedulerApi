namespace ReportSchedulerApi.Models.DTOs
{
    public class UserLookupDto
    {
        public int Id { get; set; }
        public string? Text { get; set; }
        public string? Extra { get; set; }

        public int IDUser { get; set; }
        public string? UserType { get; set; }
        public string? PersonName { get; set; }
        public string? ContactNo { get; set; }
        public string? Username { get; set; }

        public bool IsMainAdmin { get; set; }
        public bool IsHod { get; set; }
        public bool IsUser { get; set; }
        public bool IsDispatchUser { get; set; }
    }
}
