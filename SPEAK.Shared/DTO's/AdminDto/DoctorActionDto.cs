using System.ComponentModel.DataAnnotations;

namespace SPEAK.Shared.DTO_s.AdminDto
{
    public class DoctorActionDto
    {
        [Required]
        public string DoctorUserId { get; set; } = null!;
    }
}
