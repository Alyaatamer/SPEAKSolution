using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SPEAK.Abstraction.IRepositories;
using SPEAK.Abstraction.IServices;
using SPEAK.Domain.Models;
using SPEAK.Domain.Models.Enums;
using SPEAK.Shared.DTO_s.AdminDto;

namespace SPEAK.Service.Services
{
    public class AdminService : IAdminService
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IAdminLogRepository _logRepository;

        public AdminService(
            IDoctorRepository doctorRepository,
            IAdminLogRepository logRepository)
        {
            _doctorRepository = doctorRepository;
            _logRepository = logRepository;
        }

        public async Task<List<PendingDoctorDto>> GetPendingDoctorsAsync()
        {
            return await _doctorRepository.GetDoctorsByStatusAsync(DoctorStatus.Pending);
        }

        public async Task<List<PendingDoctorDto>> GetAllDoctorsAsync()
        {
            return await _doctorRepository.GetDoctorsByStatusAsync();
        }

        public async Task ApproveDoctorAsync(string doctorUserId, string adminId)
        {
            var profile = await _doctorRepository.GetDoctorProfileByUserIdAsync(doctorUserId)
                ?? throw new Exception("Doctor profile not found.");

            profile.Status = DoctorStatus.Approved;
            profile.ApprovedByAdminId = adminId;
            profile.ApprovedAt = DateTime.UtcNow;
            await _doctorRepository.UpdateDoctorProfileAsync(profile);

            await _logRepository.AddLogAsync(new AdminLog
            {
                AdminId = adminId,
                Action = "Approve",
                TargetUserId = doctorUserId,
                Details = "Doctor account approved."
            });
        }

        public async Task RejectDoctorAsync(string doctorUserId, string adminId)
        {
            var profile = await _doctorRepository.GetDoctorProfileByUserIdAsync(doctorUserId)
                ?? throw new Exception("Doctor profile not found.");

            profile.Status = DoctorStatus.Rejected;
            await _doctorRepository.UpdateDoctorProfileAsync(profile);

            await _logRepository.AddLogAsync(new AdminLog
            {
                AdminId = adminId,
                Action = "Reject",
                TargetUserId = doctorUserId,
                Details = "Doctor account rejected."
            });
        }

        public async Task DisableUserAsync(string userId, string adminId)
        {
            var user = await _doctorRepository.GetUserByIdAsync(userId, ignoreFilters: true)
                ?? throw new Exception("User not found.");

            user.IsDisabled = true;
            await _doctorRepository.UpdateUserAsync(user);

            await _logRepository.AddLogAsync(new AdminLog
            {
                AdminId = adminId,
                Action = "Disable",
                TargetUserId = userId,
                Details = "User account disabled."
            });
        }

        public async Task EnableUserAsync(string userId, string adminId)
        {
            var user = await _doctorRepository.GetUserByIdAsync(userId, ignoreFilters: true)
                ?? throw new Exception("User not found.");

            user.IsDisabled = false;
            await _doctorRepository.UpdateUserAsync(user);

            await _logRepository.AddLogAsync(new AdminLog
            {
                AdminId = adminId,
                Action = "Enable",
                TargetUserId = userId,
                Details = "User account re-enabled."
            });
        }

        public async Task<List<AdminLog>> GetAdminLogsAsync()
        {
            return await _logRepository.GetAllLogsAsync();
        }
    }
}
