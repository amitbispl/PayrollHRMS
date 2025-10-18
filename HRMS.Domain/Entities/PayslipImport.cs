using System;

namespace HRMS.Domain.Entities
{
    public class PayslipImport
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
        public decimal Basic { get; set; }
        public decimal HRA { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
