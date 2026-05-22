namespace ReportSchedulerApi.Models.DTOs
{
    public class LoginRequestDto
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public int IDUser { get; set; }
        public string? PersonName { get; set; }
        public string? UserType { get; set; }
        public string? Username { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
