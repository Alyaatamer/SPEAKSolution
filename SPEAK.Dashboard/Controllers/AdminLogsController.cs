using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPEAK.Abstraction.IServices;
using SPEAK.Dashboard.ViewModels;

namespace SPEAK.Dashboard.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminLogsController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminLogsController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // GET: /AdminLogs
        public async Task<IActionResult> Index(string? search, DateTime? dateFrom, DateTime? dateTo)
        {
            var logs = await _adminService.GetAdminLogsAsync();

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(search))
            {
                logs = logs
                    .Where(l => l.AdminName.Contains(search, StringComparison.OrdinalIgnoreCase)
                             || l.Action.Contains(search, StringComparison.OrdinalIgnoreCase)
                             || (l.Details != null && l.Details.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // Filter by date range
            if (dateFrom.HasValue)
                logs = logs.Where(l => l.Timestamp.Date >= dateFrom.Value.Date).ToList();

            if (dateTo.HasValue)
                logs = logs.Where(l => l.Timestamp.Date <= dateTo.Value.Date).ToList();

            var vm = new AdminLogViewModel
            {
                Logs        = logs.OrderByDescending(l => l.Timestamp).ToList(),
                SearchQuery = search,
                DateFrom    = dateFrom,
                DateTo      = dateTo
            };

            return View(vm);
        }
    }
}
