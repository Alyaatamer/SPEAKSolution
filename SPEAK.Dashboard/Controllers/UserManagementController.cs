using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SPEAK.Abstraction.IServices;
using SPEAK.Dashboard.ViewModels;
using SPEAK.Domain.Models.Identity;
using System.Security.Claims;

namespace SPEAK.Dashboard.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserManagementController(IAdminService adminService, UserManager<ApplicationUser> userManager)
        {
            _adminService = adminService;
            _userManager  = userManager;
        }


        public async Task<IActionResult> Index()
        {

            var allUsers = _userManager.Users.ToList();


            var items = new List<UserManagementItemViewModel>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                items.Add(new UserManagementItemViewModel
                {
                    UserId    = user.Id ?? "",
                    UserName  = user.DisplayName ?? user.Email ?? "",
                    Email     = user.Email ?? "",
                    Role      = roles.FirstOrDefault() ?? "Parent",
                    Status    = user.IsDisabled ? "Disabled" : "Active",
                    CreatedAt = DateTime.UtcNow
                });
            }

            return View(new UserManagementViewModel { Users = items });
        }


        [HttpPost]
        public async Task<IActionResult> Disable(string userId)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            await _adminService.DisableUserAsync(userId, adminId);
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> Enable(string userId)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            await _adminService.EnableUserAsync(userId, adminId);
            return RedirectToAction(nameof(Index));
        }
    }
}
