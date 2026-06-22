using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SPEAK.Abstraction.IServices;
using SPEAK.Shared.DTO_s.IdentityDto;

namespace SPEAK.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController(IServicesManger servicesManger) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto dto)
        {
            var User = await servicesManger.AuthenticationServices.LoginAsync(dto);
            return Ok(User);
        }

        [HttpPost("Register")]
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto dto)
        {
            var User = await servicesManger.AuthenticationServices.RegisterAsync(dto);
            return Ok(User);
        }

        [HttpPost("verify-registration-otp")]
        public async Task<ActionResult<UserDto>> VerifyRegistrationOtp([FromBody] VerifyOtpDto dto)
        {
            var User = await servicesManger.AuthenticationServices.VerifyRegistrationOtpAsync(dto);
            return Ok(User);
        }

        [HttpPost("resend-registration-otp")]
        public async Task<IActionResult> ResendRegistrationOtp([FromBody] ResendOtpDto dto)
        {
            await servicesManger.AuthenticationServices.SendRegistrationOtpAsync(dto.Email ?? "");
            return Ok(new { message = "Verification code has been resent to your email." });
        }

        [HttpGet("CheckEmail")]
        public async Task<ActionResult<bool>> CheckEmail(string email)
        {
            var IsEmailExist = await servicesManger.AuthenticationServices.CheckEmailAsync(email);
            return Ok(IsEmailExist);
        }

        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordDto dto)
        {
            await servicesManger.AuthenticationServices.SendForgetPasswordOtpAsync(dto.Email);
            return Ok(new { message = "A verification code has been sent to your email." });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            var token = await servicesManger.AuthenticationServices.VerifyOtpAndGetResetTokenAsync(dto);
            return Ok(new { resetToken = token });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            await servicesManger.AuthenticationServices.ResetPasswordAsync(dto);
            return Ok(new { message = "Password has been reset successfully. A confirmation email has been sent." });
        }

        [HttpPost("login-google")]
        public async Task<ActionResult<UserDto>> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            var User = await servicesManger.AuthenticationServices.GoogleLoginAsync(dto);
            return Ok(User);
        }

        [HttpPost("complete-google-profile")]
        [Authorize]
        public async Task<ActionResult<UserDto>> CompleteGoogleProfile([FromBody] ChildProfileDto dto)
        {
            var result = await servicesManger.AuthenticationServices.CompleteGoogleProfileAsync(User, dto);
            return Ok(result);
        }

        [HttpPost("register-doctor")]
        [Consumes("multipart/form-data")]
        [ApiExplorerSettings(IgnoreApi = true)] // Hidden from Swagger - uses mixed FromForm+IFormFile params
        public async Task<ActionResult<UserDto>> RegisterDoctor(
         [FromForm] DoctorRegisterDto dto,
         [FromForm] IFormFile syndicateCardImage,
         [FromForm] IFormFile nationalIdImage,
         [FromForm] string? vezeetaLink, 
         [FromServices] IWebHostEnvironment env)
        {
            if (syndicateCardImage == null || nationalIdImage == null)
                return BadRequest(new { message = "Both syndicate card image and national ID image are required." });

            // Build upload path inside wwwroot/uploads/doctors/
            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "uploads", "doctors");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Generate unique file names to avoid collisions
            var syndicateName = $"{Guid.NewGuid()}_{Path.GetFileName(syndicateCardImage.FileName)}";
            var nationalIdName = $"{Guid.NewGuid()}_{Path.GetFileName(nationalIdImage.FileName)}";

            // Save files to disk
            using (var stream = new FileStream(Path.Combine(uploadsFolder, syndicateName), FileMode.Create))
                await syndicateCardImage.CopyToAsync(stream);

            using (var stream = new FileStream(Path.Combine(uploadsFolder, nationalIdName), FileMode.Create))
                await nationalIdImage.CopyToAsync(stream);

            // Build public URLs
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var syndicateUrl = $"{baseUrl}/uploads/doctors/{syndicateName}";
            var nationalIdUrl = $"{baseUrl}/uploads/doctors/{nationalIdName}";

            // Pass vezeetaLink to the service
            var result = await servicesManger.AuthenticationServices.DoctorRegisterAsync(dto, syndicateUrl, nationalIdUrl, vezeetaLink);

            return Ok(result);
        }

        [Authorize(Roles = "Parent")]
        [HttpPut("profile")]
        public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var updatedUser = await servicesManger.AuthenticationServices.UpdateParentProfileAsync(email, dto);
            return Ok(updatedUser);
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var userProfile = await servicesManger.AuthenticationServices.GetCurrentUserAsync(email);
            return Ok(userProfile);
        }

        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            await servicesManger.AuthenticationServices.ChangePasswordAsync(email, dto);
            return Ok(new { message = "Password changed successfully." });
        }

        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // In a stateless JWT authentication system, logout is primarily handled on the client side
            // by deleting the token. The server can optionally invalidate the token using a blacklist.
            return Ok(new { message = "Logged out successfully. Please remove the token from the client." });
        }
    }
}
