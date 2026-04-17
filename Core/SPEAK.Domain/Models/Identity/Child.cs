using SPEAK.Domain.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Domain.Models.Identity
{
    public class Child
    {
        public string ChildName { get; set; } = null!;
        public DateTime? ChildBirthDate { get; set; }
        public Gender ChildGender { get; set; }
    }
}
