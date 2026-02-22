using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SPEAK.Abstraction.IServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SPEAK.Shared.DTO_s;

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

        [HttpPost("merge-voices")]
        public async Task<IActionResult> MergeVoices([FromForm] List<IFormFile> files, [FromForm] string type = "general")
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest("No files uploaded.");
            }

            try
            {
                var result = await _voiceProcessingService.ProcessVoiceSessionAsync(files, type, _env.ContentRootPath);

                return Ok(new
                {
                    WordCount = result.WordCount,
                    MergedFile = Path.GetFileName(result.MergedFilePath)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("cleanup-merged")]
        public async Task<IActionResult> CleanupMerged([FromForm] string type = "general")
        {
            try
            {
                await _voiceProcessingService.CleanupMergedFileAsync(type, _env.ContentRootPath);
                return Ok(new { message = "Merged file cleaned up successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Cleanup failed: {ex.Message}");
            }
        }

        [HttpPost("calculate-ssi")]
        public async Task<IActionResult> CalculateSSI([FromBody] SSIDetectionRequestDto request)
        {
            try
            {
                 // Extract User ID from Claims
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token.");
                }

                var resultJson = await _voiceProcessingService.CalculateSSIAsync(userId, request, _env.ContentRootPath);
                
                // Return the raw JSON string as JSON content
                return Content(resultJson, "application/json");
            }
             catch (Exception ex)
            {
                return StatusCode(500, $"SSI Calculation failed: {ex.Message}");
            }
        }

        [HttpGet("latest-diagnosis")]
        public async Task<IActionResult> GetLatestDiagnosis()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token.");
                }
                
                var record = await _voiceProcessingService.GetLatestDiagnosisAsync(userId);
                
                // Return Ok with null instead of NotFound to avoid middleware conflicts
                return Ok(record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error fetching diagnosis: {ex.Message}" });
            }
        }
    }
}
