using HRMS.Application.Interfaces.Repositories;
using HRMS.Domain.Entities;
using MediatR;
using OfficeOpenXml;

namespace HRMS.Application.Features.PayslipImport.Commands
{
    public class ImportPayslipsCommandHandler : IRequestHandler<ImportPayslipsCommand, bool>
    {
        private readonly IPayslipImportRepository _importRepo;

        public ImportPayslipsCommandHandler(IPayslipImportRepository importRepo)
        {
            _importRepo = importRepo;
        }

        public async Task<bool> Handle(ImportPayslipsCommand request, CancellationToken cancellationToken)
        {
            if (request.File == null || request.File.Length == 0)
                return false;

            using var package = new ExcelPackage(request.File.OpenReadStream());
            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension.Rows;

            // Create Master record
            var master = new PayslipImportMaster
            {
                CompanyId = request.CompanyId,
                FileName = request.File.FileName,
                MonthYear = worksheet.Cells[2, 1].Text,
                UploadedBy = request.UploadedBy,
                ScheduledDate = request.ScheduledDate
            };

            int importId = await _importRepo.CreateImportMasterAsync(master);

            // Create Details records
            var details = Enumerable.Range(2, rowCount - 1)
                .Select(row => new PayslipImportDetails
                {
                    ImportId = importId,
                    EmpCode = worksheet.Cells[row, 1].Text,
                    EmpName = worksheet.Cells[row, 2].Text,
                    Department = worksheet.Cells[row, 3].Text,
                    Designation = worksheet.Cells[row, 4].Text,
                    Basic = worksheet.Cells[row, 5].GetValue<decimal>(),
                    HRA = worksheet.Cells[row, 6].GetValue<decimal>(),
                    OtherAllowances = worksheet.Cells[row, 7].GetValue<decimal>(),
                    Deductions = worksheet.Cells[row, 8].GetValue<decimal>(),
                    NetPay = worksheet.Cells[row, 9].GetValue<decimal>(),
                    Email = worksheet.Cells[row, 10].Text
                })
                .Where(x => !string.IsNullOrEmpty(x.EmpCode))
                .ToList();

            await _importRepo.AddPayslipDetailsAsync(details);

            return true;
        }
    }
}
