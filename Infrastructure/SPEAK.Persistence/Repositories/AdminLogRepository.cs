using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SPEAK.Abstraction.IRepositories;
using SPEAK.Domain.Models;
using SPEAK.Persistence.Contexts;

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

        public async Task<List<AdminLog>> GetAllLogsAsync()
        {
            return await _context.AdminLogs
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }
    }
}
