using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces
{
    public interface IPayslipJobRepository
    {
        Task<int> CreateAsync(PayslipJobDto job);
        Task<IEnumerable<PayslipJobDto>> GetAllAsync();
        Task UpdateStatusAsync(int importId, string status, string? message = null);
    }
}
