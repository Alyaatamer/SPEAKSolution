using SPEAK.Domain.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Shared.DTO_s.IdentityDto
{
    public class RegisterDto
    {
        [Required]
        public string FullName { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required, MinLength(6)]
        public string Password { get; set; } = null!;

        [Required, Compare("Password", ErrorMessage = "Password do not match")]
        public string ConfirmPassword { get; set; } = null!;

        [Phone]
        public string? PhoneNumber { get; set; } 

        public string? ChildName { get; set; }   

        [Required]
        public DateTime? ChildBirthDate { get; set; }

        [Required]
        public Gender ChildGender { get; set; }

    }
}
