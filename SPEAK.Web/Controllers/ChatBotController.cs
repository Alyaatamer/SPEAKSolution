using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPEAK.Shared.DTO_s.ChatDto;
using SPEAK.Web.Services;

namespace SPEAK.Web.Controllers
{
    [Route("api/chatbot")]
    [ApiController]
    [AllowAnonymous]
    public class ChatBotController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly ILogger<ChatBotController> _logger;

        public ChatBotController(IAIService aiService, ILogger<ChatBotController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        // Endpoint بسيط - يرجع JSON عادي { answer: "..." }
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatBotRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { error = "Message cannot be empty" });

            _logger.LogInformation("ChatBot request - Message: {Message}, SessionId: {SessionId}",
                request.Message, request.SessionId);

            try
            {
                await using var stream = await _aiService.GetResponseStreamAsync(request.Message, request.SessionId);
                using var reader = new StreamReader(stream);
                var fullResponse = await reader.ReadToEndAsync();

                return Ok(new { answer = fullResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatBot error: {Message} | Inner: {Inner}",
                    ex.Message, ex.InnerException?.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Endpoint قديم للـ streaming - محتفظين بيه
        [HttpPost("chat-stream")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task ChatStream([FromBody] ChatBotRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("Message cannot be empty");
                return;
            }

            Response.ContentType = "text/plain; charset=utf-8";
            Response.Headers.Append("X-Accel-Buffering", "no");
            Response.Headers.Append("Cache-Control", "no-cache");
            
            var bufferingFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
            bufferingFeature?.DisableBuffering();

            try
            {
                await using var stream = await _aiService.GetResponseStreamAsync(request.Message, request.SessionId);
                byte[] buffer = new byte[256];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await Response.Body.WriteAsync(buffer, 0, bytesRead);
                    await Response.Body.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatStream error: {Message}", ex.Message);
                if (!Response.HasStarted)
                {
                    Response.StatusCode = 500;
                    await Response.WriteAsync($"Server error: {ex.Message}");
                }
            }
        }
    }
}