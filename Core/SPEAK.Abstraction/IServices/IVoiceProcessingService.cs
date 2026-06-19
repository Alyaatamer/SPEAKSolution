using Microsoft.AspNetCore.Http;
using System;
using SPEAK.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPEAK.Shared.DTO_s;

namespace SPEAK.Abstraction.IServices
{
    public interface IVoiceProcessingService
    {
        Task<(int WordCount, string MergedFilePath)> ProcessVoiceSessionAsync(string userId, List<IFormFile> files, string type, string webRootPath);
        Task<string> CalculateSSIAsync(string userId, SSIDetectionRequestDto request, string webRootPath);
        Task CleanupMergedFileAsync(string userId, string type, string webRootPath);
        Task<DiagnosticRecord?> GetLatestDiagnosisAsync(string userId);
    }
}
