using System.Collections.Generic;
using System.Threading.Tasks;
using SPEAK.Domain.Models;

namespace SPEAK.Abstraction.IRepositories
{
    public interface IAdminLogRepository
    {
        Task AddLogAsync(AdminLog log);
        Task<List<AdminLog>> GetAllLogsAsync();
    }
}
