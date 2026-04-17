using System.Text.Json.Serialization;

namespace SPEAK.Shared.DTO_s.IdentityDto
{
    public class UserDto
    {
        // ── Common fields (returned for all roles) ──────────────────────────
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string Role { get; set; } = "Parent";
        public bool IsProfileComplete { get; set; }

        // ── Doctor-only fields (null → omitted from JSON) ───────────────────
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DoctorStatus { get; set; }

        // ── Parent-only fields (null → omitted from JSON) ───────────────────
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ChildName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? ChildBirthDate { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ChildAge { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ChildGender { get; set; }   // 0 = Male, 1 = Female

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? AvatarId { get; set; }
    }
}
