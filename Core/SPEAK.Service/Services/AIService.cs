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
            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");

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

        public async Task<Stream> GetVoiceToVoiceResponseAsync(Stream audioStream, string fileName)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");

            // Using the ngrok URL or falling back from configuration
            var aiVoiceUrl = _configuration["AI:VoiceToVoiceUrl"];
            
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(audioStream);
            content.Add(streamContent, "file", fileName);
            content.Add(new StringContent("default"), "session_id");

            var requestMsg = new HttpRequestMessage(HttpMethod.Post, aiVoiceUrl)
            {
                Content = content
            };

            var response = await client.SendAsync(requestMsg, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"AI voice model error: {response.StatusCode} - {response.ReasonPhrase}");

            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<string> GetVoiceToTextResponseAsync(Stream audioStream, string fileName)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");

            var aiVoiceToTextUrl = _configuration["AI:VoiceToTextUrl"];// fallback dummy URL
            
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(audioStream);
            content.Add(streamContent, "file", fileName);

            var requestMsg = new HttpRequestMessage(HttpMethod.Post, aiVoiceToTextUrl)
            {
                Content = content
            };

            var response = await client.SendAsync(requestMsg);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"AI voice-to-text model error: {response.StatusCode} - {response.ReasonPhrase}");

            var responseString = await response.Content.ReadAsStringAsync();
            return responseString;
        }
    }
}