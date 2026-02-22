using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPEAK.Abstraction.IServices;
using SPEAK.Shared.DTO_s.AdminDto;
using System.Security.Claims;

namespace SPEAK.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>Get all doctors with Pending status</summary>
        [HttpGet("pending-doctors")]
        public async Task<IActionResult> GetPendingDoctors()
        {
            var doctors = await _adminService.GetPendingDoctorsAsync();
            return Ok(doctors);
        }

        /// <summary>Get all doctors regardless of status</summary>
        [HttpGet("all-doctors")]
        public async Task<IActionResult> GetAllDoctors()
        {
            var doctors = await _adminService.GetAllDoctorsAsync();
            return Ok(doctors);
        }

        /// <summary>Approve a doctor registration</summary>
        [HttpPost("approve-doctor")]
        public async Task<IActionResult> ApproveDoctor([FromBody] DoctorActionDto dto)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            await _adminService.ApproveDoctorAsync(dto.DoctorUserId, adminId);
            return Ok(new { message = "Doctor approved successfully." });
        }

        /// <summary>Reject a doctor registration</summary>
        [HttpPost("reject-doctor")]
        public async Task<IActionResult> RejectDoctor([FromBody] DoctorActionDto dto)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            await _adminService.RejectDoctorAsync(dto.DoctorUserId, adminId);
            return Ok(new { message = "Doctor rejected." });
        }

        /// <summary>Disable a user account (soft delete)</summary>
        [HttpPost("disable-user/{userId}")]
        public async Task<IActionResult> DisableUser(string userId)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            await _adminService.DisableUserAsync(userId, adminId);
            return Ok(new { message = "User disabled." });
        }

        /// <summary>Re-enable a previously disabled user</summary>
        [HttpPost("enable-user/{userId}")]
        public async Task<IActionResult> EnableUser(string userId)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            await _adminService.EnableUserAsync(userId, adminId);
            return Ok(new { message = "User enabled." });
        }

        /// <summary>Get all admin action logs</summary>
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs()
        {
            var logs = await _adminService.GetAdminLogsAsync();
            return Ok(logs);
        }
    }
}
