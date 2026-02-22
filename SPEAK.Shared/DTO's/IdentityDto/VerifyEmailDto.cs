using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Shared.DTO_s.IdentityDto
{
    public class VerifyEmailDto
    {
        public string Email { get; set; } = null!;
        public string Otp { get; set; } = null!;
    }
}
