using System.ComponentModel.DataAnnotations;

namespace SPEAK.Shared.DTO_s.IdentityDto
{
    public class ResendOtpDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
    }
}
