using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SPEAK.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatMediaController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public ChatMediaController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("upload")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UploadMedia([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var uploadsPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "chat_media");
            
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var request = HttpContext.Request;
            var mediaUrl = $"{request.Scheme}://{request.Host}/uploads/chat_media/{fileName}";

            return Ok(new { mediaUrl });
        }
    }
}
