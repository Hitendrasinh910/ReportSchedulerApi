using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace ReportSchedulerApi.Repositories.Notification
{
    public class NotificationApiService : INotificationApiService
    {
        private readonly HttpClient _client;
        private readonly IConfiguration _configuration;

        public NotificationApiService(
            HttpClient client,
            IConfiguration configuration)
        {
            _client = client;
            _configuration = configuration;
        }

        public async Task<string> SendNotificationAsync(
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
             string? base64Data = null)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var apiUrl = _configuration["NotificationApi:Url"];
            var apiKey = _configuration["NotificationApi:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiUrl))
                throw new Exception("NotificationApi:Url is missing in appsettings.json.");

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("NotificationApi:ApiKey is missing in appsettings.json.");

            //userPhones.Add("9727464548");
            userPhones = userPhones?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList() ?? new List<string>();

            if (userPhones.Count == 0)
                throw new Exception("Notification API call skipped because userPhones is empty.");

            subject = CleanNotificationText(subject);
            header = CleanNotificationText(header);
            message = CleanNotificationText(message);
            footer = CleanNotificationText(footer);

            object payload;

            if (!string.IsNullOrWhiteSpace(base64Data))
            {
                payload = new
                {
                    subject = subject ?? string.Empty,
                    header = header ?? subject ?? string.Empty,
                    message = message ?? string.Empty,
                    footer = footer ?? string.Empty,
                    userPhones,
                    category = category ?? string.Empty,
                    subCategory = subCategory ?? string.Empty,
                    forceSend,
                    document = new
                    {
                        name = documentName ?? "report.pdf",
                        mimeType = mimeType ?? "application/pdf",
                        base64Data
                    }
                };
            }
            else
            {
                payload = new
                {
                    subject = subject ?? string.Empty,
                    header = header ?? subject ?? string.Empty,
                    message = message ?? string.Empty,
                    footer = footer ?? string.Empty,
                    userPhones,
                    category = category ?? string.Empty,
                    subCategory = subCategory ?? string.Empty,
                    forceSend
                };
            }

            var json = JsonConvert.SerializeObject(
                payload,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            Console.WriteLine("Notification API Payload:");
            Console.WriteLine(json);

            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);

            request.Headers.Remove("X-API-KEY");
            request.Headers.Add("X-API-KEY", apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine("Notification API Status:");
            Console.WriteLine(response.StatusCode);

            Console.WriteLine("Notification API Response:");
            Console.WriteLine(result);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Notification API failed. Status: {response.StatusCode}, Payload: {json}, Response: {result}");
            }

            return result;
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
