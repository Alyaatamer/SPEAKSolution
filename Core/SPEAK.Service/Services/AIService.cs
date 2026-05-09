using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SPEAK.Web.Services
{
    public class AIService : IAIService
    {
        private readonly IConfiguration _configuration;

        public AIService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<Stream> GetResponseStreamAsync(string message, string sessionId)
        {
            // Bypass SSL for ngrok / self-signed certificates
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            var client = new HttpClient(handler);

            var aiModelUrl = _configuration["AI:ChatUrl"];
            var payload = JsonSerializer.Serialize(new
            {
                message = message,
                session_id = sessionId ?? Guid.NewGuid().ToString()
            });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var requestMsg = new HttpRequestMessage(HttpMethod.Post, aiModelUrl)
            {
                Content = content
            };

            // ResponseHeadersRead عشان نبدأ stream فورًا
            var response = await client.SendAsync(requestMsg, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"AI model error: {response.StatusCode} - {response.ReasonPhrase}");

            return await response.Content.ReadAsStreamAsync();
        }
    }
}