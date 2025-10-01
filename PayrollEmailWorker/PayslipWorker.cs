using Dapper;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.Data.SqlClient;
using PayrollEmailWorker.Models;

public class PayslipWorker : BackgroundService
{
    private readonly ILogger<PayslipWorker> _logger;
    private readonly string _connectionString;

    public PayslipWorker(ILogger<PayslipWorker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection");
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

        using var writer = new PdfWriter(filePath);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);

        // Company & Header
        document.Add(new Paragraph("Access2Resources Pvt. Ltd.").SetFontSize(16));
        document.Add(new Paragraph($"Payslip for {DateTime.Now:MMMM yyyy}")
            .SetFontSize(12)
            .SetMarginBottom(20));

        // Employee Details
        document.Add(new Paragraph($"Employee Name: {emp.EmpName}"));
        document.Add(new Paragraph($"Department: {emp.Department}"));
        document.Add(new Paragraph($"Designation: {emp.Designation}"));
        document.Add(new Paragraph($"Employee Code: {emp.EmpCode}"));
        document.Add(new Paragraph($"Grade: {emp.Grade}"));
        document.Add(new Paragraph($"Join Date: {(emp.JoinDate.HasValue ? emp.JoinDate.Value.ToString("dd-MMM-yyyy") : "-")}"));
        document.Add(new Paragraph("-------------------------------------------------"));

        // Salary Table (Dynamic Heads)
        Table table = new Table(2);
        foreach (var head in empHeads)
        {
            table.AddCell(head.HeadName);
            table.AddCell(head.Amount.ToString("C"));
        }

        table.AddCell("Gross Rate");
        table.AddCell(emp.GrossRate.ToString("C"));

        table.AddCell("Deduction Amount");
        table.AddCell(emp.DeductionAmount.ToString("C"));

        table.AddCell("Net Pay");
        table.AddCell(emp.NetPay.ToString("C"));

        document.Add(table);

        // PL Details
        document.Add(new Paragraph($"\nPL Opening: {emp.OpeningPL}"));
        document.Add(new Paragraph($"PL Earned: {emp.PLEarn}"));
        document.Add(new Paragraph($"PL Avail: {emp.PLAvail}"));
        document.Add(new Paragraph($"PL Closing: {emp.PLClosing}"));

        // Disclaimer
        document.Add(new Paragraph("\nThis is a computer-generated payslip and does not require signature.")
            .SetFontSize(9));

        _logger.LogInformation("Payslip generated: {filePath}", filePath);
    }
}
