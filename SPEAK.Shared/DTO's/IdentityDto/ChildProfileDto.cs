using SPEAK.Domain.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Shared.DTO_s.IdentityDto
{
    public class ChildProfileDto
    {
        public string Email { get; set; } = "";
        public string ChildName { get; set; } = "";
        public DateTime? ChildBirthDate { get; set; }
        public Gender ChildGender { get; set; }
    }
}
