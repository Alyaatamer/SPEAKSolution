using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SPEAK.Abstraction.IRepositories;
using SPEAK.Domain.Models;
using SPEAK.Persistence.Contexts;
using SPEAK.Shared.DTO_s.AdminDto;

namespace SPEAK.Persistence.Repositories
{
    public class AdminLogRepository : IAdminLogRepository
    {
        private readonly UserIdentityDbContext _context;

        public AdminLogRepository(UserIdentityDbContext context)
        {
            _context = context;
        }

        public async Task AddLogAsync(AdminLog log)
        {
            await _context.AdminLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AdminLogDto>> GetAllLogsAsync()
        {
            return await _context.AdminLogs
                .Include(l => l.Admin)          
                .OrderByDescending(l => l.Timestamp)
                .Select(l => new AdminLogDto
                {
                    Id            = l.Id,
                    AdminId       = l.AdminId,
                    AdminName     = l.Admin != null ? l.Admin.DisplayName : "Unknown",
                    AdminEmail    = l.Admin != null ? l.Admin.Email!     : "Unknown",
                    Action        = l.Action,
                    TargetUserId  = l.TargetUserId,
                    Details       = l.Details,
                    Timestamp     = l.Timestamp
                })
                .ToListAsync();
        }
    }
}
