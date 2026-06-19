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
            Response.Headers.Append("Connection", "keep-alive");

            var bodyFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
            bodyFeature?.DisableBuffering();

            try
            {
                await using var stream = await _aiService.GetResponseStreamAsync(request.Message, request.SessionId);
                var buffer = new byte[1]; // Read byte by byte to enforce flushing!
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, HttpContext.RequestAborted)) > 0)
                {
                    await Response.Body.WriteAsync(buffer, 0, bytesRead, HttpContext.RequestAborted);
                    await Response.Body.FlushAsync(HttpContext.RequestAborted);
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

        [HttpPost("test-stream")]
        [AllowAnonymous]
        public async Task TestStream()
        {
            Response.ContentType = "text/plain; charset=utf-8";
            Response.Headers.Append("X-Accel-Buffering", "no");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            var bodyFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
            bodyFeature?.DisableBuffering();

            string[] words = { "Hello", " this", " is", " a", " streaming", " test", " message", " from", " the", " server!" };
            foreach (var word in words)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(word);
                await Response.Body.WriteAsync(bytes, 0, bytes.Length);
                await Response.Body.FlushAsync();
                await Task.Delay(500); // 500ms delay between words
            }
        }


        [HttpPost("voice-to-voice")]
        public async Task<IActionResult> VoiceToVoice(IFormFile audio)
        {
            if (audio == null || audio.Length == 0)
                return BadRequest(new { error = "Audio file is required." });

            try
            {
                var stream = await _aiService.GetVoiceToVoiceResponseAsync(audio.OpenReadStream(), audio.FileName);
                return File(stream, "audio/mpeg");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VoiceToVoice error: {Message}", ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("voice-to-text")]
        public async Task<IActionResult> VoiceToText(IFormFile audio)
        {
            if (audio == null || audio.Length == 0)
                return BadRequest(new { error = "Audio file is required." });

            try
            {
                var aiResponseJson = await _aiService.GetVoiceToTextResponseAsync(audio.OpenReadStream(), audio.FileName);
                
                // Python API only returns the transcribed plain text for STT
                return Ok(new { userText = aiResponseJson });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VoiceToText error: {Message}", ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}