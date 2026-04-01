using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPEAK.Abstraction.IServices;
using SPEAK.Dashboard.ViewModels;
using System.Security.Claims;

namespace SPEAK.Dashboard.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DoctorVerificationController : Controller
    {
        private readonly IAdminService _adminService;

        public DoctorVerificationController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        public async Task<IActionResult> Index(string? statusFilter = "All")
        {
            var allDoctors = await _adminService.GetAllDoctorsAsync();

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                allDoctors = allDoctors
                    .Where(d => d.DoctorStatus.Equals(statusFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var vm = new DoctorVerificationViewModel
            {
                Doctors      = allDoctors,
                StatusFilter = statusFilter
            };

            return View(vm);
        }

        
        [HttpPost]
        public async Task<IActionResult> Approve(string doctorUserId)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            await _adminService.ApproveDoctorAsync(doctorUserId, adminId);
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> Reject(string doctorUserId)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            await _adminService.RejectDoctorAsync(doctorUserId, adminId);
            return RedirectToAction(nameof(Index));
        }
    }
}
