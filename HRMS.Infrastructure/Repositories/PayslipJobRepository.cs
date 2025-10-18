using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Infrastructure.Repositories
{
    public class PayslipJobRepository : IPayslipJobRepository
    {
        private readonly AppDbContext _context;

        public PayslipJobRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateAsync(PayslipJobDto job)
        {
            var entity = new PayslipImportMaster
            {
                FileName = job.FileName,
                MonthYear = job.MonthYear,
                UploadedBy = job.UploadedBy,
                UploadedDate = DateTime.UtcNow,
                ScheduledDate = job.ScheduledDate,
                Status = job.Status,
                Message = job.Message,
                IsProcessed = false
            };

            await _context.PayslipImportMasters.AddAsync(entity);
            await _context.SaveChangesAsync();

            return entity.ImportId;
        }

        public async Task<IEnumerable<PayslipJobDto>> GetAllAsync()
        {
            return await _context.PayslipImportMasters
                .OrderByDescending(x => x.UploadedDate)
                .Select(x => new PayslipJobDto
                {
                    ImportId = x.ImportId,
                    FileName = x.FileName,
                    MonthYear = x.MonthYear,
                    UploadedBy = x.UploadedBy,
                    UploadedDate = x.UploadedDate,
                    ScheduledDate = x.ScheduledDate,
                    Status = x.Status,
                    Message = x.Message
                })
                .ToListAsync();
        }

        public async Task UpdateStatusAsync(int importId, string status, string? message = null)
        {
            var entity = await _context.PayslipImportMasters
                .FirstOrDefaultAsync(x => x.ImportId == importId);

            if (entity != null)
            {
                entity.Status = status;
                entity.Message = message;
                entity.ProcessedDate = DateTime.UtcNow;
                entity.IsProcessed = true;

                await _context.SaveChangesAsync();
            }
        }
    }
}
