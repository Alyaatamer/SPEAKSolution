using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SPEAK.Domain.Models.Enums;

namespace SPEAK.Domain.Models.Identity
{
    public class DoctorProfile
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public string? SyndicateCardImageUrl { get; set; }
        public string? NationalIdImageUrl { get; set; }

        public DoctorStatus Status { get; set; } = DoctorStatus.Pending;

        public string? ApprovedByAdminId { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
