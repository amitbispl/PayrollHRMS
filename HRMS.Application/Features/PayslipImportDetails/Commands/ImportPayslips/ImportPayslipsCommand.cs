using MediatR;
using Microsoft.AspNetCore.Http;

namespace HRMS.Application.Features.PayslipImport.Commands
{
    public record ImportPayslipsCommand(IFormFile File, int CompanyId, string UploadedBy, DateTime ScheduledDate) : IRequest<bool>;
}
