using System.ComponentModel.DataAnnotations;

namespace HRMS.Domain.Entities
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }
        public int CompanyId { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public decimal BasicSalary { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? EmpCode { get; set; }
        public string? EmpName { get; set; }
        public string? UAN { get; set; }
        public string? ESIC { get; set; }
        public int? DeptId { get; set; }
        public int? DesignationId { get; set; }
        public decimal HRA { get; set; }
        public decimal FlexiPay { get; set; }

        public Company? Company { get; set; }
    }
}
