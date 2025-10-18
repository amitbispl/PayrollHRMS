using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.Application.DTOs
{
    public class PayslipImportDto
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
