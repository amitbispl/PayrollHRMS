using System;

namespace HRMS.Domain.Entities
{
    public class PayslipImportMaster
    {
        public int ImportId { get; set; }
        public int CompanyId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string MonthYear { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string Status { get; set; } = "Scheduled";
        public string? Message { get; set; }
        public bool IsProcessed { get; set; }
    }
    public class PayslipImportDetails
    {
        public int ImportId { get; set; }
        public string EmpCode { get; set; }
        public string EmpName { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public decimal Basic { get; set; }
        public decimal HRA { get; set; }
        public decimal OtherAllowances { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetPay { get; set; }
        public string Email { get; set; }
    }
}
