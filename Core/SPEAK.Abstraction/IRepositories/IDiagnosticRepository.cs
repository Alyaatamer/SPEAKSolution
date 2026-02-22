using System.Threading.Tasks;
using SPEAK.Domain.Models;

namespace SPEAK.Abstraction.IRepositories
{
    public interface IDiagnosticRepository
    {
        Task AddDiagnosticRecordAsync(DiagnosticRecord record);
        Task<DiagnosticRecord?> GetLatestDiagnosticRecordAsync(string userId);
    }
}
