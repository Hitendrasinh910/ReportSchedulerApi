namespace ReportSchedulerApi.Models.Common
{
    public class SaveResult
    {
        public bool IsSuccess { get; set; }
        public int Id { get; set; }
        public string? Message { get; set; }

        public static SaveResult Success(int id, string message)
        {
            return new SaveResult
            {
                IsSuccess = true,
                Id = id,
                Message = message
            };
        }

        public static SaveResult Fail(string message)
        {
            return new SaveResult
            {
                IsSuccess = false,
                Id = 0,
                Message = message
            };
        }
    }
}
