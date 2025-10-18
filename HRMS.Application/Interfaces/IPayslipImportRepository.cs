using HRMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HRMS.Application.Interfaces.Repositories
{
    public interface IPayslipImportRepository
    {
        Task AddRangeAsync(IEnumerable<PayslipImport> payslips, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
        Task<int> CreateImportMasterAsync(PayslipImportMaster master);
        Task AddPayslipDetailsAsync(IEnumerable<PayslipImportDetails> details);
    }
}
