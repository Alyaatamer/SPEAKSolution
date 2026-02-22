using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SPEAK.Domain.Models.Identity;

namespace SPEAK.Domain.Models
{
    public class AdminLog
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string AdminId { get; set; } = string.Empty;

        [ForeignKey("AdminId")]
        public ApplicationUser? Admin { get; set; }

        public string Action { get; set; } = string.Empty;
        public string TargetUserId { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
