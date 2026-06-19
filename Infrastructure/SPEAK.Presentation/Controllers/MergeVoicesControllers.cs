using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SPEAK.Abstraction.IServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SPEAK.Shared.DTO_s;
using SPEAK.Shared.ErrorModels;

namespace SPEAK.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MergeVoicesController : ControllerBase
    {
        private readonly IVoiceProcessingService _voiceProcessingService;
        private readonly IWebHostEnvironment _env;

        public MergeVoicesController(IVoiceProcessingService voiceProcessingService, IWebHostEnvironment env)
        {
            _voiceProcessingService = voiceProcessingService;
            _env = env;
        }

        private string GetWebRootPath()
        {
            return string.IsNullOrEmpty(_env.WebRootPath)
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : _env.WebRootPath;
        }

        private string? GetUserId() =>
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        // ─────────────────────────────────────────────────────────────────────
        //  POST api/MergeVoices/merge-voices
        //  Merges uploaded recordings with any existing merged WAV, calls
        //  /segment, and returns the resulting word count so Flutter can
        //  decide whether to proceed to physical concomitants or ask for more.
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("merge-voices")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> MergeVoices(
            [FromForm] List<IFormFile> files,
            [FromForm] string type = "general")
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token.");

            try
            {
                var result = await _voiceProcessingService.ProcessVoiceSessionAsync(
                    userId, files, type, GetWebRootPath());

                return Ok(new
                {
                    WordCount  = result.WordCount,
                    MergedFile = Path.GetFileName(result.MergedFilePath)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  POST api/MergeVoices/cleanup-merged
        //  Deletes the merged WAV and its ZIP for the given task type once the
        //  SSI calculation is complete.
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("cleanup-merged")]
        public async Task<IActionResult> CleanupMerged([FromForm] string type = "general")
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token.");

            try
            {
                await _voiceProcessingService.CleanupMergedFileAsync(userId, type, GetWebRootPath());
                return Ok(new { message = "Merged file cleaned up successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Cleanup failed: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  POST api/MergeVoices/calculate-ssi
        //  Sends the stored ZIP(s) to /analyze and returns the SSI result JSON.
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("calculate-ssi")]
        public async Task<IActionResult> CalculateSSI([FromBody] SSIDetectionRequestDto request)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token.");

            try
            {
                var resultJson = await _voiceProcessingService.CalculateSSIAsync(
                    userId, request, GetWebRootPath());

                return Content(resultJson, "application/json");
            }
            catch (WordCountException ex)
            {
                return BadRequest(new
                {
                    status     = "error",
                    word_count = ex.WordCount,
                    message_en = ex.MessageEn,
                    message_ar = ex.MessageAr
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"SSI Calculation failed: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  GET api/MergeVoices/latest-diagnosis
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("latest-diagnosis")]
        public async Task<IActionResult> GetLatestDiagnosis()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token.");

            try
            {
                var record = await _voiceProcessingService.GetLatestDiagnosisAsync(userId);

                if (record == null)
                    return Ok(null);

                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(record.FullResultJson);
                    var merged = new System.Collections.Generic.Dictionary<string, object?>
                    {
                        ["id"]             = record.Id,
                        ["userId"]         = record.UserId,
                        ["severity"]       = record.Severity,
                        ["labelCountsJson"] = record.LabelCountsJson,
                        ["createdAt"]      = record.CreatedAt,
                    };

                    foreach (var prop in doc.RootElement.EnumerateObject())
                        merged[prop.Name] = prop.Value.Clone();

                    return new ContentResult
                    {
                        Content     = System.Text.Json.JsonSerializer.Serialize(merged),
                        ContentType = "application/json",
                        StatusCode  = 200
                    };
                }
                catch
                {
                    return Ok(record);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error fetching diagnosis: {ex.Message}" });
            }
        }
    }
}
