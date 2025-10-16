using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.Domain.Entities
{
    public class PFContribution
    {
        [Key]
        public int PFId { get; set; }
        public int PayrollId { get; set; }
        public decimal EmployeeShare { get; set; }
        public decimal EmployerShare { get; set; }
        public decimal PensionShare { get; set; }
        public decimal EDLIShare { get; set; }
        public decimal AdminCharges { get; set; }
        public decimal Total { get; set; }

        public PayrollMaster? Payroll { get; set; }
    }
}
