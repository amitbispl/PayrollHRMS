using AutoMapper;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using MediatR;

namespace HRMS.Application.Features.Employees.Commands
{
    public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, EmployeeDto>
    {
        private readonly IEmployeeRepository _repo;
        private readonly IMapper _mapper;

        public CreateEmployeeCommandHandler(IEmployeeRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<EmployeeDto> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var emp = _mapper.Map<Employee>(request.Employee);
            await _repo.AddAsync(emp);
            return _mapper.Map<EmployeeDto>(emp);
        }
    }
}
