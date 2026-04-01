using SPEAK.Shared.DTO_s.AdminDto;

namespace SPEAK.Dashboard.ViewModels
{
    public class DoctorVerificationViewModel
    {
        public List<PendingDoctorDto> Doctors { get; set; } = new();
        public string? StatusFilter { get; set; } = "All";
    }
}
