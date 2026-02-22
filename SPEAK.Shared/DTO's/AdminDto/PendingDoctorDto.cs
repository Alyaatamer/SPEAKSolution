using System;

namespace SPEAK.Shared.DTO_s.AdminDto
{
    public class PendingDoctorDto
    {
        public string UserId { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? SyndicateCardImageUrl { get; set; }
        public string? NationalIdImageUrl { get; set; }
        public string DoctorStatus { get; set; } = null!;
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedByAdminId { get; set; }
    }
}
