using System;

namespace SPEAK.Shared.DTO_s.AdminDto
{
    public class AdminLogDto
    {
        public string Id { get; set; } = null!;
        public string AdminId { get; set; } = null!;
        public string AdminName { get; set; } = null!;
        public string AdminEmail { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string TargetUserId { get; set; } = null!;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
