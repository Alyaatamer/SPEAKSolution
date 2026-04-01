using SPEAK.Shared.DTO_s.AdminDto;

namespace SPEAK.Dashboard.ViewModels
{
    public class AdminLogViewModel
    {
        public List<AdminLogDto> Logs { get; set; } = new();
        public string? SearchQuery { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
