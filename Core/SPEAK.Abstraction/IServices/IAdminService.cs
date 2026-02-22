using System.Collections.Generic;
using System.Threading.Tasks;
using SPEAK.Domain.Models;
using SPEAK.Shared.DTO_s.AdminDto;

namespace SPEAK.Abstraction.IServices
{
    public interface IAdminService
    {
        Task<List<PendingDoctorDto>> GetPendingDoctorsAsync();
        Task<List<PendingDoctorDto>> GetAllDoctorsAsync();
        Task ApproveDoctorAsync(string doctorUserId, string adminId);
        Task RejectDoctorAsync(string doctorUserId, string adminId);
        Task DisableUserAsync(string userId, string adminId);
        Task EnableUserAsync(string userId, string adminId);
        Task<List<AdminLog>> GetAdminLogsAsync();
    }
}
