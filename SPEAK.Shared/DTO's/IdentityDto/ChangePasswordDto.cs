using System.ComponentModel.DataAnnotations;

namespace SPEAK.Shared.DTO_s.IdentityDto
{
    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = null!;

        [Required, MinLength(6)]
        public string NewPassword { get; set; } = null!;

        [Required, Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmNewPassword { get; set; } = null!;
    }
}
