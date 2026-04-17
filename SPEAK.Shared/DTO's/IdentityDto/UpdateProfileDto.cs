using System;

namespace SPEAK.Shared.DTO_s.IdentityDto
{
    public class UpdateProfileDto
    {
        public int AvatarId { get; set; }
        public string? ChildName { get; set; }
        public DateTime? ChildBirthDate { get; set; }
        public int ChildGender { get; set; } 
    }
}
