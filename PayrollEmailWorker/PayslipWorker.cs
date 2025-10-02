using Dapper;
using DinkToPdf;
using DinkToPdf.Contracts;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.Data.SqlClient;
using PayrollEmailWorker.Models;
using System.Text;

public class PayslipWorker : BackgroundService
{
    private readonly ILogger<PayslipWorker> _logger;
    private readonly string _connectionString;
    private readonly IConverter _converter;

    public PayslipWorker(ILogger<PayslipWorker> logger, IConfiguration configuration, IConverter converter)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _converter = converter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Payslip generation started at: {time}", DateTimeOffset.Now);

            IEnumerable<PayslipDto> employees;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                employees = await connection.QueryAsync<PayslipDto>(
                    "sp_GetAllPayslipData",
                    commandType: System.Data.CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payslip data");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            // Group by employee code to handle multiple salary heads
            var employeeGroups = employees.GroupBy(e => e.EmpCode);

            foreach (var empGroup in employeeGroups)
            {
                try
                {
                    GeneratePayslip(empGroup);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating payslip for EmployeeCode: {EmpCode}", empGroup.Key);
                }
            }

            _logger.LogInformation("Payslip generation completed at: {time}", DateTimeOffset.Now);

            // Delay between runs (set to 1 minute for demo; replace with scheduling in production)
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private void GeneratePayslip(IEnumerable<PayslipDto> empHeads)
    {
        if (!empHeads.Any()) return;

        var emp = empHeads.First(); // Common info

        string folderPath = Path.Combine("Payslips", DateTime.Now.ToString("yyyy-MM"));
        Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, $"Payslip_{emp.EmpCode}_{DateTime.Now:yyyyMMdd}.pdf");

        // Load HTML template
        string templatePath = Path.Combine(AppContext.BaseDirectory, "HTML", "payslip-template.html");
        string html = File.ReadAllText(templatePath);

        // Build salary rows dynamically
        var salaryRows = new StringBuilder();
        foreach (var head in empHeads)
        {
            salaryRows.AppendLine($"<tr><td>{head.HeadName}</td><td>{head.Amount:C}</td></tr>");
        }

        // Replace placeholders
        html = html.Replace("{{Month}}", DateTime.Now.ToString("MMMM yyyy"))
                   .Replace("{{EmpName}}", emp.EmpName)
                   .Replace("{{Department}}", emp.Department ?? "-")
                   .Replace("{{Designation}}", emp.Designation ?? "-")
                   .Replace("{{EmpCode}}", emp.EmpCode)
                   .Replace("{{Grade}}", emp.Grade ?? "-")
                   .Replace("{{JoinDate}}", emp.JoinDate.HasValue ? emp.JoinDate.Value.ToString("dd-MMM-yyyy") : "-")
                   .Replace("{{SalaryRows}}", salaryRows.ToString())
                   .Replace("{{GrossRate}}", emp.GrossRate.ToString("C"))
                   .Replace("{{DeductionAmount}}", emp.DeductionAmount.ToString("C"))
                   .Replace("{{NetPay}}", emp.NetPay.ToString("C"))
                   .Replace("{{OpeningPL}}", emp.OpeningPL.ToString())
                   .Replace("{{PLEarn}}", emp.PLEarn.ToString())
                   .Replace("{{PLAvail}}", emp.PLAvail.ToString())
                   .Replace("{{PLClosing}}", emp.PLClosing.ToString());

        // Generate PDF with DinkToPdf
        var doc = new HtmlToPdfDocument()
        {
            GlobalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Out = filePath
            },
            Objects = {
            new ObjectSettings
            {
                HtmlContent = html,
                WebSettings = { DefaultEncoding = "utf-8" }
            }
        }
        };

        _converter.Convert(doc);

        _logger.LogInformation("Payslip generated: {filePath}", filePath);
    }

}
