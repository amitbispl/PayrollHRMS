using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.Domain.Entities
{
    public class Company
    {
        public int CompanyId { get; set; }
        public int ContractorId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? PFNumber { get; set; }
        public string? ESICNumber { get; set; }
        public string? TAN { get; set; }
        public string? PAN { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Contractor? Contractor { get; set; }
        public ICollection<Employee>? Employees { get; set; }
    }
}
