using HRMS.Application.DTOs;
using MediatR;

namespace HRMS.Application.Features.Employees.Commands
{
    public class CreateEmployeeCommand : IRequest<EmployeeDto>
    {
        public EmployeeDto Employee { get; set; } = new EmployeeDto();
    }
}
