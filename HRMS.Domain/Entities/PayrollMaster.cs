using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.Domain.Entities
{
    public class PayrollMaster
    {
        [Key]
        public int PayrollId { get; set; }
        public int EmployeeId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal DaysWorked { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal PFEmployee { get; set; }
        public decimal PFEmployer { get; set; }
        public decimal ESIEmployee { get; set; }
        public decimal ESIEmployer { get; set; }
        public decimal OtherDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public bool IsFinalized { get; set; } = false;

        public Employee? Employee { get; set; }
        public PFContribution? PFContribution { get; set; }
        public ESIContribution? ESIContribution { get; set; }
    }
}
