using System.ComponentModel.DataAnnotations;

namespace SPEAK.Dashboard.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        public string? ErrorMessage { get; set; }
    }
}
