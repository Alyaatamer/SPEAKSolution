using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SPEAK.Abstraction.IServices;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class VoiceController : ControllerBase
{
    private readonly IServicesManger _services;
    private readonly IWebHostEnvironment _environment;

    public VoiceController(IServicesManger services, IWebHostEnvironment environment)
    {
        _services = services;
        _environment = environment;
    }
    [AllowAnonymous]
    [HttpPost("upload")]
    public async Task<IActionResult> UploadVoice(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        string contentRootPath = _environment.ContentRootPath;
        string webRootPath = _environment.WebRootPath;

        // If WebRootPath is null (can happen in some environments), fallback to ContentRootPath/wwwroot
        if (string.IsNullOrEmpty(webRootPath))
        {
            webRootPath = Path.Combine(contentRootPath, "wwwroot");
        }

        var uploadsFolder = Path.Combine(webRootPath, "voices");

        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        var fileUrl = $"{Request.Scheme}://{Request.Host}/voices/{fileName}";

        return Ok(new
        {
            voiceUrl = fileUrl,
            savedPath = filePath
        });
    }
}