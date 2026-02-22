using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SPEAK.Domain.Models.Identity;

namespace SPEAK.Domain.Models
{
    public class DiagnosticRecord
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public string Severity { get; set; } = string.Empty; // e.g., "Moderate"
        public string LabelCountsJson { get; set; } = "{}"; // Storing top disfluencies as JSON string
        public string FullResultJson { get; set; } = string.Empty; // Storing the complete response for future reference
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
