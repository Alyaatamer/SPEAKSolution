using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPEAK.Domain.Models.Enums;

namespace SPEAK.Domain.Models.Identity
{
    public class ParentProfile
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public string? ChildName { get; set; }
        public int ChildAge { get; set; }
        public Gender ChildGender { get; set; }
    }
}
