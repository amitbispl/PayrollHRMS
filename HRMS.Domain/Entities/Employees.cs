namespace HRMS.Domain.Entities
{
    public class Employee
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public DateTime DateOfJoining { get; set; }
        public decimal Salary { get; set; }

        // Foreign key to Department
        public int DepartmentId { get; set; }
        public Department? Department { get; set; }

        // Derived property
        public string FullName => $"{FirstName} {LastName}";
    }
}
