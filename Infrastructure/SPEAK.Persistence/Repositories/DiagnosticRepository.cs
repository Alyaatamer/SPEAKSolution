using Microsoft.EntityFrameworkCore;
using SPEAK.Abstraction.IRepositories;
using SPEAK.Domain.Models;
using SPEAK.Persistence.Contexts;
using System.Linq;
using System.Threading.Tasks;

namespace SPEAK.Persistence.Repositories
{
    public class DiagnosticRepository : IDiagnosticRepository
    {
        private readonly UserIdentityDbContext _context;

        public DiagnosticRepository(UserIdentityDbContext context)
        {
            _context = context;
        }

        public async Task AddDiagnosticRecordAsync(DiagnosticRecord record)
        {
            await _context.DiagnosticRecords.AddAsync(record);
            await _context.SaveChangesAsync();
        }

        public async Task<DiagnosticRecord?> GetLatestDiagnosticRecordAsync(string userId)
        {
            return await _context.DiagnosticRecords
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
