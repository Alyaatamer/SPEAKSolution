using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Shared.DTO_s.IdentityDto
{
    public class ForgetPasswordDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

    }
}
