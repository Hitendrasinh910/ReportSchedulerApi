namespace ReportSchedulerApi.Repositories.Notification
{
    public interface INotificationApiService
    {
        Task<string> SendNotificationAsync(
            string subject,
            string header,
            string message,
            string footer,
            List<string> userPhones,
            string category,
            string subCategory,
            bool forceSend = true,
            string? documentName = null,
            string? mimeType = null,
            string? base64Data = null);
    }
}
