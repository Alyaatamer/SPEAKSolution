using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SPEAK.Abstraction.IServices;
using SPEAK.Dashboard.ViewModels;
using SPEAK.Domain.Models.Identity;

namespace SPEAK.Dashboard.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(IAdminService adminService, UserManager<ApplicationUser> userManager)
        {
            _adminService = adminService;
            _userManager  = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var allDoctors = await _adminService.GetAllDoctorsAsync();
            var recentLogs = await _adminService.GetAdminLogsAsync();

            var allUsers = _userManager.Users.ToList();

            var vm = new DashboardViewModel
            {
                TotalUsers      = allUsers.Count,
                ApprovedDoctors = allDoctors.Count(d => d.DoctorStatus == "Approved"),
                PendingDoctors  = allDoctors.Count(d => d.DoctorStatus == "Pending"),
                TotalChildren   = 0,  

                RecentDoctors = allDoctors
                    .OrderByDescending(d => d.ApprovedAt)
                    .Take(3)
                    .ToList(),

                RecentAdminActions = recentLogs
                    .OrderByDescending(l => l.Timestamp)
                    .Take(3)
                    .ToList()
            };

            return View(vm);
        }
    }
}
