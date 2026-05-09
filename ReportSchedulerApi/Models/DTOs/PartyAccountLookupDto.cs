namespace ReportSchedulerApi.Models.DTOs
{
    public class PartyAccountLookupDto
    {
        public int Id { get; set; }
        public string? Text { get; set; }
        public string? Extra { get; set; }

        public int IDPartyAccount { get; set; }
        public string? PartyCode { get; set; }
        public string? PartyType { get; set; }
        public string? Type { get; set; }
        public string? PartyACName { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactNo { get; set; }
        public string? EmailID { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }

        public int? IDCompany { get; set; }
        public int? IDPartyType { get; set; }
        public int? IDMarketingPerson { get; set; }
        public int? IDMarketingHead { get; set; }
    }
}
