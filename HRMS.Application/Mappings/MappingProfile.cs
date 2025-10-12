using AutoMapper;
using HRMS.Domain.Entities;
using HRMS.Application.DTOs;

namespace HRMS.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Employee, EmployeeDto>()
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department!.Name))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
                .ReverseMap();

            CreateMap<Department, DepartmentDto>().ReverseMap();
        }
    }
}
