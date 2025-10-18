using HRMS.Application.Features.PayslipImport.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

public class ImportController : Controller
{
    private readonly IMediator _mediator;

    public ImportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    //[HttpGet]
    //public async Task<IActionResult> Index()
    //{
    //    var imports = await _mediator.Send(new GetAllPayslipImportsQuery());
    //    return View(imports);
    //}

    [HttpPost]
    public async Task<IActionResult> Import(IFormFile file, int companyId, DateTime scheduledDate)
    {
        if (file == null) return BadRequest("File is required");

        var uploadedBy = User.Identity?.Name ?? "Admin"; // or any default

        await _mediator.Send(new ImportPayslipsCommand(
            File: file,
            CompanyId: companyId,
            UploadedBy: uploadedBy,
            ScheduledDate: scheduledDate
        ));

        TempData["Message"] = "Payslip data imported successfully!";
        return RedirectToAction(nameof(Index));
    }

}
