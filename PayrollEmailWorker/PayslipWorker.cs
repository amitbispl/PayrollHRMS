using Dapper;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PayrollEmailWorker.helper;
using PayrollEmailWorker.Models;
using System.Net;
using System.Net.Mail;
using System.Text;

public class PayslipWorker : BackgroundService
{
    private readonly ILogger<PayslipWorker> _logger;
    private readonly string _connectionString;
    private readonly IConverter _converter;
    private readonly IConfiguration _configuration;

    public PayslipWorker(ILogger<PayslipWorker> logger, IConfiguration configuration, IConverter converter)
    {
        _logger = logger;
        _configuration = configuration;
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

            var employeeGroups = employees.GroupBy(e => e.EmpCode).ToList();

            string folderPath = Path.Combine("Payslips", DateTime.Now.ToString("yyyy-MM"));
            Directory.CreateDirectory(folderPath);

            string templatePath = Path.Combine(AppContext.BaseDirectory, "HTML", "payslip-template1.html");
            string htmlTemplate = File.ReadAllText(templatePath);

            var tempFiles = new List<string>();

            // Generate PDFs in parallel
            await Parallel.ForEachAsync(employeeGroups, new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = stoppingToken
            }, async (empGroup, ct) =>
            {
                try
                {
                    var emp = empGroup.First();
                    string filePath = Path.Combine(folderPath, $"Payslip_{emp.EmpCode}_{DateTime.Now:yyyyMMdd}.pdf");

                    string html = BuildPayslipHtml(empGroup, htmlTemplate);

                    var doc = new HtmlToPdfDocument()
                    {
                        GlobalSettings = new GlobalSettings
                        {
                            ColorMode = ColorMode.Color,
                            Orientation = Orientation.Portrait,
                            PaperSize = PaperKind.A4,
                            Out = filePath
                        },
                        Objects = { new ObjectSettings { HtmlContent = html, WebSettings = { DefaultEncoding = "utf-8" } } }
                    };

                    _converter.Convert(doc);

                    lock (tempFiles) tempFiles.Add(filePath);

                    _logger.LogInformation("Payslip generated: {filePath}", filePath);

                    // Send individual email to employee
                    //await SendEmailWithAttachmentAsync(emp, filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating payslip for EmployeeCode: {EmpCode}", empGroup.Key);
                }
            });

            if (tempFiles.Any())
            {
                string mergedFile = Path.Combine(folderPath, $"Payslips_Merged_{DateTime.Now:yyyyMMdd}.pdf");
                MergePdfs(tempFiles, mergedFile);

                _logger.LogInformation("Merged PDF created: {mergedFile}", mergedFile);

                //await SendMergedEmailToHRAsync(mergedFile);
            }

            _logger.LogInformation("Payslip generation completed at: {time}", DateTimeOffset.Now);

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private string BuildPayslipHtml(IEnumerable<PayslipDto> empHeads, string htmlTemplate)
    {
        var emp = empHeads.First();
        var salaryRows = new StringBuilder();
        decimal totalEarning = 0, totalDeduction = 0;
        bool isFirstRow = true;

        foreach (var head in empHeads)
        {
            totalEarning += head.Amount;
            totalDeduction += head.DeductionAmount;
            salaryRows.AppendLine($@"
<tr>
    <td>{head.HeadName}</td>
    <td>{head.Amount:C}</td>
    <td>{(isFirstRow ? "Deductions" : "")}</td>
    <td>{head.DeductionAmount:C}</td>
</tr>");
            isFirstRow = false;
        }

        decimal netPay = totalEarning - totalDeduction;
        string netPayInWords = Helper.NumberToWords(netPay);
        string salaryPeriod = Helper.GetSalaryPeriod(DateTime.Now);

        return htmlTemplate
            .Replace("{{Month}}", DateTime.Now.ToString("MMMM yyyy"))
            .Replace("{{EmpName}}", emp.EmpName)
            .Replace("{{Department}}", emp.Department ?? "-")
            .Replace("{{Designation}}", emp.Designation ?? "-")
            .Replace("{{EmpCode}}", emp.EmpCode)
            .Replace("{{EmailId}}", emp.Email)
            .Replace("{{Grade}}", emp.Grade ?? "-")
            .Replace("{{UAN}}", emp.UAN ?? "-")
            .Replace("{{ESIC}}", emp.ESIC ?? "-")
            .Replace("{{TotalMonthDays}}", emp.TotalMonthDays.ToString())
            .Replace("{{TotalPaidDays}}", emp.TotalPaidDays.ToString())
            .Replace("{{JoinDate}}", emp.JoinDate?.ToString("dd-MMM-yyyy") ?? "-")
            .Replace("{{SalaryRows}}", salaryRows.ToString())
            .Replace("{{TotalEarnings}}", totalEarning.ToString("C"))
            .Replace("{{TotalDeductions}}", totalDeduction.ToString("C"))
            .Replace("{{NetSalary}}", netPay.ToString("C"))
            .Replace("{{GrossRate}}", emp.GrossRate.ToString("C"))
            .Replace("{{DeductionAmount}}", emp.DeductionAmount.ToString("C"))
            .Replace("{{NetPayInWords}}", $"Rs. {netPayInWords} through Bank for {DateTime.Now:MMMM yyyy}.")
            .Replace("{{OpeningPL}}", emp.OpeningPL.ToString())
            .Replace("{{PLEarn}}", emp.PLEarn.ToString())
            .Replace("{{PLAvail}}", emp.PLAvail.ToString())
            .Replace("{{PLClosing}}", emp.PLClosing.ToString())
            .Replace("{{SalaryPeriod}}", salaryPeriod)
            .Replace("{{LeaveTaken}}", emp.PLAvail.ToString());
    }

    private void MergePdfs(List<string> files, string outputFile)
    {
        using var pdfDoc = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfWriter(outputFile));
        foreach (var file in files)
        {
            using var reader = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(file));
            reader.CopyPagesTo(1, reader.GetNumberOfPages(), pdfDoc);
        }
    }

    private async Task SendEmailWithAttachmentAsync(PayslipDto emp, string filePath)
    {
        var smtpSection = _configuration.GetSection("Email");

        using var client = new SmtpClient(smtpSection["Host"], int.Parse(smtpSection["Port"]))
        {
            Credentials = new NetworkCredential(smtpSection["User"], smtpSection["Password"]),
            EnableSsl = bool.Parse(smtpSection["EnableSsl"])
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(smtpSection["From"]),
            Subject = $"Payslip for {DateTime.Now:MMMM yyyy}",
            Body = $"Dear {emp.EmpName},\n\nPlease find attached your payslip for {DateTime.Now:MMMM yyyy}.\n\nRegards,\nHR Department"
        };

        mail.To.Add(emp.Email);
        mail.CC.Add("aks.bispl@gmail.com");
        mail.Attachments.Add(new Attachment(filePath));

        await client.SendMailAsync(mail);
        _logger.LogInformation("Payslip sent to {Email}", emp.Email);
    }

    private async Task SendMergedEmailToHRAsync(string mergedFile)
    {
        var smtpSection = _configuration.GetSection("Email");

        using var client = new SmtpClient(smtpSection["Host"], int.Parse(smtpSection["Port"]))
        {
            Credentials = new NetworkCredential(smtpSection["User"], smtpSection["Password"]),
            EnableSsl = bool.Parse(smtpSection["EnableSsl"])
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(smtpSection["From"]),
            Subject = $"All Payslips for {DateTime.Now:MMMM yyyy}",
            Body = $"Dear HR,\n\nPlease find attached all employee payslips for {DateTime.Now:MMMM yyyy}.\n\nRegards,\nPayroll System"
        };

        mail.To.Add("hr@company.com"); // HR Email
        mail.Attachments.Add(new Attachment(mergedFile));

        await client.SendMailAsync(mail);
        _logger.LogInformation("Merged payslip email sent to HR.");
    }


    private void GeneratePayslipAndSendEmail(IEnumerable<PayslipDto> empHeads)
    {
        if (!empHeads.Any()) return;

        var emp = empHeads.First(); // Common employee info

        // Create folder for PDFs
        string folderPath = Path.Combine("Payslips", DateTime.Now.ToString("yyyy-MM"));
        Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, $"Payslip_{emp.EmpCode}_{DateTime.Now:yyyyMMdd}.pdf");

        // Load HTML template
        string templatePath = Path.Combine(AppContext.BaseDirectory, "HTML", "payslip-template1.html");
        string html = File.ReadAllText(templatePath);

        // Build salary rows dynamically and calculate totals
        var salaryRows = new StringBuilder();
        decimal totalEarning = 0;
        decimal totalDeduction = 0;
        bool isFirstRow = true;

        foreach (var head in empHeads)
        {
            totalEarning += head.Amount; // For earnings
            totalDeduction += head.DeductionAmount; // For deductions

            salaryRows.AppendLine($@"
<tr>
    <td>{head.HeadName}</td>
    <td>{head.Amount:C}</td>
    <td>{(isFirstRow ? "Deductions Title" : "")}</td>
    <td>{head.DeductionAmount:C}</td>
</tr>");
            isFirstRow = false;
        }


        decimal netPay = totalEarning - totalDeduction;
        string netPayInWords = Helper.NumberToWords(netPay);
        string salaryPeriod = Helper.GetSalaryPeriod(DateTime.Now);
        // Replace placeholders in HTML
        html = html.Replace("{{Month}}", DateTime.Now.ToString("MMMM yyyy"))
                   .Replace("{{EmpName}}", emp.EmpName)
                   .Replace("{{Department}}", emp.Department ?? "-")
                   .Replace("{{Designation}}", emp.Designation ?? "-")
                   .Replace("{{EmpCode}}", emp.EmpCode)
                   .Replace("{{EmailId}}", emp.Email)
                   .Replace("{{Grade}}", emp.Grade ?? "-")
                   .Replace("{{UAN}}", emp.UAN ?? "-")
                   .Replace("{{ESIC}}", emp.ESIC ?? "-")
                   .Replace("{{TotalMonthDays}}", emp.TotalMonthDays.ToString())
                   .Replace("{{TotalPaidDays}}", emp.TotalPaidDays.ToString())
                   .Replace("{{JoinDate}}", emp.JoinDate?.ToString("dd-MMM-yyyy") ?? "-")
                   .Replace("{{SalaryRows}}", salaryRows.ToString())
                   .Replace("{{TotalEarnings}}", totalEarning.ToString("C"))
                   .Replace("{{TotalDeductions}}", totalDeduction.ToString("C"))
                   .Replace("{{NetSalary}}", netPay.ToString("C"))
                   .Replace("{{GrossRate}}", emp.GrossRate.ToString("C"))
                   .Replace("{{DeductionAmount}}", emp.DeductionAmount.ToString("C"))
                   .Replace("{{NetPayInWords}}", $"Rs. {netPayInWords} through Bank for the month of {DateTime.Now:MMMM, yyyy}.")
                   .Replace("{{OpeningPL}}", emp.OpeningPL.ToString())
                   .Replace("{{PLEarn}}", emp.PLEarn.ToString())
                   .Replace("{{PLAvail}}", emp.PLAvail.ToString())
                   .Replace("{{PLClosing}}", emp.PLClosing.ToString()
                   .Replace("{{SalaryPeriod}}", salaryPeriod)
                   .Replace("{{LeaveTaken}}", emp.PLAvail.ToString()));

        // Generate PDF using DinkToPdf
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

        // Send email with PDF
       // SendEmailWithAttachment(emp, filePath);
    }
    private async Task GenerateAndMergePayslipsAsync(IEnumerable<IGrouping<string, PayslipDto>> employeeGroups, string htmlTemplate, string folderPath)
    {
        var tempFiles = new List<string>();

        // Generate PDFs in parallel
        await Parallel.ForEachAsync(employeeGroups, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (empGroup, ct) =>
        {
            try
            {
                var emp = empGroup.First();
                string filePath = Path.Combine(folderPath, $"Payslip_{emp.EmpCode}_{DateTime.Now:yyyyMMdd}.pdf");

                string html = BuildPayslipHtml(empGroup, htmlTemplate);

                var doc = new HtmlToPdfDocument()
                {
                    GlobalSettings = new GlobalSettings
                    {
                        ColorMode = ColorMode.Color,
                        Orientation = Orientation.Portrait,
                        PaperSize = PaperKind.A4,
                        Out = filePath
                    },
                    Objects = { new ObjectSettings { HtmlContent = html, WebSettings = { DefaultEncoding = "utf-8" } } }
                };

                _converter.Convert(doc);

                lock (tempFiles) tempFiles.Add(filePath);
                _logger.LogInformation("Payslip generated: {filePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payslip for EmployeeCode: {EmpCode}", empGroup.Key);
            }
        });

        if (!tempFiles.Any()) return;

        // Merge PDFs
        string mergedFile = Path.Combine(folderPath, $"Payslips_Merged_{DateTime.Now:yyyyMMdd}.pdf");
        MergePdfs(tempFiles, mergedFile);

        _logger.LogInformation("Merged PDF created: {mergedFile}", mergedFile);

        // Send merged email to HR
        await SendMergedEmailToHRAsync(mergedFile);

        // Optionally: delete individual temp PDFs
        foreach (var f in tempFiles)
            File.Delete(f);
    }

    
    


    private void SendEmailWithAttachment(PayslipDto emp, string filePath)
    {
        var smtpSection = _configuration.GetSection("Email");

        using var client = new SmtpClient(smtpSection["Host"], int.Parse(smtpSection["Port"]))
        {
            Credentials = new NetworkCredential(smtpSection["User"], smtpSection["Password"]),
            EnableSsl = bool.Parse(smtpSection["EnableSsl"])
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(smtpSection["From"]),
            Subject = $"Payslip for {DateTime.Now:MMMM yyyy}",
            Body = $"Dear {emp.EmpName},\n\nPlease find attached your payslip for {DateTime.Now:MMMM yyyy}.\n\nRegards,\nHR Department"
        };

        mail.To.Add(emp.Email);
        mail.CC.Add("aks.bispl@gmail.com"); // Add CC
        mail.Attachments.Add(new Attachment(filePath));

        client.Send(mail);

        _logger.LogInformation("Payslip sent to {Email}", emp.Email);
    }
}
