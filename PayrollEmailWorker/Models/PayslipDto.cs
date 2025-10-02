using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayrollEmailWorker.Models
{
    public class PayslipDto
    {
        public string EmpName { get; set; }
        public string EmpCode { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public string Grade { get; set; }
        public DateTime? JoinDate { get; set; }
        public string BankName { get; set; }
        public string BankAccount { get; set; }
        public string IFSCCode { get; set; }
        public string PFNo { get; set; }
        public string ESIC { get; set; }
        public string UAN { get; set; }
        public decimal TotalMonthDays { get; set; }
        public decimal TotalPaidDays { get; set; }
        public decimal OpeningPL { get; set; }
        public decimal PLEarn { get; set; }
        public decimal PLAvail { get; set; }
        public decimal PLClosing { get; set; }
        public decimal GrossRate { get; set; }
        public decimal DeductionAmount { get; set; }
        public decimal NetPay { get; set; }
        public string Email { get; set; }

        // Dynamic salary heads
        public string HeadName { get; set; }
        public decimal Amount { get; set; }
        public decimal EarningsAmount { get; set; }
        public decimal DeductionsAmount { get; set; }
        public bool IsFirstRow { get; set; } = false; // Only first row renders deductions block
        public int DeductionRowSpan { get; set; } = 1; // rowspan for deductions column
        public string DeductionsTitle { get; set; } = "E.P.F. ESIC\nHostel TDS\nOther Deductions";
    }



}
