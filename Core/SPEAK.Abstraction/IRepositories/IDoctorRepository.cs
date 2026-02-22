using System.Collections.Generic;
using System.Threading.Tasks;
using SPEAK.Domain.Models;
using SPEAK.Domain.Models.Enums;
using SPEAK.Domain.Models.Identity;
using SPEAK.Shared.DTO_s.AdminDto;

namespace SPEAK.Abstraction.IRepositories
{
    public interface IDoctorRepository
    {
        // Admin queries
        Task<List<PendingDoctorDto>> GetDoctorsByStatusAsync(DoctorStatus? status = null);
        Task<DoctorProfile?> GetDoctorProfileByUserIdAsync(string userId);
        Task UpdateDoctorProfileAsync(DoctorProfile profile);

        // User management (for disable/enable)
        Task<ApplicationUser?> GetUserByIdAsync(string userId, bool ignoreFilters = false);
        Task UpdateUserAsync(ApplicationUser user);

        // Profile creation (used by AuthenticationServices)
        Task AddDoctorProfileAsync(DoctorProfile profile);
        Task AddParentProfileAsync(ParentProfile profile);
    }
}
