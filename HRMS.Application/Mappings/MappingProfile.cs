using AutoMapper;
using HRMS.Domain.Entities;
using HRMS.Application.DTOs;

namespace HRMS.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<Employee, EmployeeDto>();
        }
    }
}
