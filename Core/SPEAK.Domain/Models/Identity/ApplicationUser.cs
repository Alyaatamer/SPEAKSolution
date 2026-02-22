using Microsoft.AspNetCore.Identity;
using SPEAK.Domain.Models.Enums;

namespace SPEAK.Domain.Models.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = null!;
        public int ChildAge { get; set; } // kept for backward compatibility

        // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public bool IsDisabled { get; set; } = false;

        // Navigation Properties
        public DoctorProfile? DoctorProfile { get; set; }
        public ParentProfile? ParentProfile { get; set; }
    }
}
