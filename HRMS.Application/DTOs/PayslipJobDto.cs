namespace HRMS.Application.DTOs
{
    public class PayslipJobDto
    {
        public int ImportId { get; set; }
        public string FileName { get; set; }
        public string MonthYear { get; set; }
        public string UploadedBy { get; set; }
        public DateTime UploadedDate { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public string Status { get; set; }
        public string? Message { get; set; }
    }
}
