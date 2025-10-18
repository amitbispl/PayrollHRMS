using HRMS.Application.Interfaces.Repositories;
using HRMS.Domain.Entities;
using HRMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HRMS.Infrastructure.Repositories
{
    public class PayslipImportRepository : IPayslipImportRepository
    {
        private readonly AppDbContext _context;

        public PayslipImportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<PayslipImport> payslips, CancellationToken cancellationToken)
        {
            await _context.PayslipImports.AddRangeAsync(payslips, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        public async Task<int> CreateImportMasterAsync(PayslipImportMaster master)
        {
            _context.PayslipImportMasters.Add(master);
            await _context.SaveChangesAsync();
            return master.ImportId; // EF Core sets the identity automatically
        }
        public async Task AddPayslipDetailsAsync(IEnumerable<PayslipImportDetails> details)
        {
            _context.PayslipImportDetails.AddRange(details);
            await _context.SaveChangesAsync();
        }
    }
}
