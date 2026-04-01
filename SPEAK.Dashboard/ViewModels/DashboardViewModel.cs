using SPEAK.Shared.DTO_s.AdminDto;

namespace SPEAK.Dashboard.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ApprovedDoctors { get; set; }
        public int PendingDoctors { get; set; }
        public int TotalChildren { get; set; }

        public List<PendingDoctorDto> RecentDoctors { get; set; } = new();
        public List<AdminLogDto> RecentAdminActions { get; set; } = new();
    }
}
