namespace ReportSchedulerApi.Models.DTOs
{
    public class ReportScheduleRecipientDto
    {
        public int IDRecipient { get; set; }
        public int IDSchedule { get; set; }

        public string RecipientType { get; set; }
        // User / PartyAccount

        public int IDReference { get; set; }
    }
}
