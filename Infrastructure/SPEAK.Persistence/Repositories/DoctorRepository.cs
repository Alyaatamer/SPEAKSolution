using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SPEAK.Abstraction.IRepositories;
using SPEAK.Domain.Models;
using SPEAK.Domain.Models.Enums;
using SPEAK.Domain.Models.Identity;
using SPEAK.Persistence.Contexts;
using SPEAK.Shared.DTO_s.AdminDto;

namespace SPEAK.Persistence.Repositories
{
    public class DoctorRepository : IDoctorRepository
    {
        private readonly UserIdentityDbContext _context;

        public DoctorRepository(UserIdentityDbContext context)
        {
            _context = context;
        }

        public async Task<List<PendingDoctorDto>> GetDoctorsByStatusAsync(DoctorStatus? status = null)
        {
            var query = _context.DoctorProfiles.Include(d => d.User).AsQueryable();

            if (status.HasValue)
                query = query.Where(d => d.Status == status.Value);

            return await query.Select(d => new PendingDoctorDto
            {
                UserId               = d.UserId,
                DisplayName          = d.User!.DisplayName,
                Email                = d.User.Email!,
                PhoneNumber          = d.User.PhoneNumber,
                SyndicateCardImageUrl = d.SyndicateCardImageUrl,
                NationalIdImageUrl   = d.NationalIdImageUrl,
                DoctorStatus         = d.Status.ToString(),
                ApprovedAt           = d.ApprovedAt,
                ApprovedByAdminId    = d.ApprovedByAdminId
            }).ToListAsync();
        }

        public async Task<DoctorProfile?> GetDoctorProfileByUserIdAsync(string userId)
        {
            return await _context.DoctorProfiles
                .FirstOrDefaultAsync(d => d.UserId == userId);
        }

        public async Task UpdateDoctorProfileAsync(DoctorProfile profile)
        {
            _context.DoctorProfiles.Update(profile);
            await _context.SaveChangesAsync();
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId, bool ignoreFilters = false)
        {
            if (ignoreFilters)
                return await _context.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == userId);

            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task UpdateUserAsync(ApplicationUser user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task AddDoctorProfileAsync(DoctorProfile profile)
        {
            await _context.DoctorProfiles.AddAsync(profile);
            await _context.SaveChangesAsync();
        }

        public async Task AddParentProfileAsync(ParentProfile profile)
        {
            await _context.ParentProfiles.AddAsync(profile);
            await _context.SaveChangesAsync();
        }
    }
}
