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
        Task<(int WordCount, string MergedFilePath)> ProcessVoiceSessionAsync(List<IFormFile> files, string type, string contentRootPath);
        Task<string> CalculateSSIAsync(string userId, SSIDetectionRequestDto request, string contentRootPath);
        Task CleanupMergedFileAsync(string type, string contentRootPath);
        Task<DiagnosticRecord?> GetLatestDiagnosisAsync(string userId);
    }
}
