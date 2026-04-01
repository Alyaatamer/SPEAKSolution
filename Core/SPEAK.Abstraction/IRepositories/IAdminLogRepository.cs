using System.Collections.Generic;
using System.Threading.Tasks;
using SPEAK.Domain.Models;
using SPEAK.Shared.DTO_s.AdminDto;

namespace SPEAK.Abstraction.IRepositories
{
    public interface IAdminLogRepository
    {
        Task AddLogAsync(AdminLog log);
        Task<List<AdminLogDto>> GetAllLogsAsync();
    }
}
