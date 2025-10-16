using HRMS.Application.DTOs;
using MediatR;

namespace HRMS.Application.Features.Employees.Commands
{
    public class UpdateEmployeeCommand : IRequest<bool>
    {
        public EmployeeDto Employee { get; set; } = new EmployeeDto();
    }
}
