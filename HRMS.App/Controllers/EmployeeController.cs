using HRMS.Application.DTOs;
using HRMS.Application.Features.Employees.Commands;
using HRMS.Application.Features.Employees.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRMS.Web.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IMediator _mediator;

        public EmployeeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // 🔹 List
        public async Task<IActionResult> Index()
        {
            var employees = await _mediator.Send(new GetAllEmployeesQuery());
            return View(employees);
        }

        // 🔹 Details
        public async Task<IActionResult> Details(int id)
        {
            var emp = await _mediator.Send(new GetEmployeeByIdQuery { EmployeeId = id });
            if (emp == null) return NotFound();
            return View(emp);
        }

        // 🔹 Create (GET)
        public IActionResult Create() => View(new EmployeeDto());

        // 🔹 Create (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeDto model)
        {
            if (!ModelState.IsValid) return View(model);
            await _mediator.Send(new CreateEmployeeCommand { Employee = model });
            return RedirectToAction(nameof(Index));
        }

        // 🔹 Edit (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var emp = await _mediator.Send(new GetEmployeeByIdQuery { EmployeeId = id });
            if (emp == null) return NotFound();
            return View(emp);
        }

        // 🔹 Edit (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmployeeDto model)
        {
            if (!ModelState.IsValid) return View(model);
            await _mediator.Send(new UpdateEmployeeCommand { Employee = model });
            return RedirectToAction(nameof(Index));
        }

        // 🔹 Delete (GET)
        public async Task<IActionResult> Delete(int id)
        {
            var emp = await _mediator.Send(new GetEmployeeByIdQuery { EmployeeId = id });
            if (emp == null) return NotFound();
            return View(emp);
        }

        // 🔹 Delete (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _mediator.Send(new DeleteEmployeeCommand { EmployeeId = id });
            return RedirectToAction(nameof(Index));
        }
    }
}
