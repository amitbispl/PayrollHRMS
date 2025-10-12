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
        //await SendPayslipsManuallyAsync(); 
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

            var employeeGroups = employees.GroupBy(e => e.EmpCode).OrderBy(g => g.Key).ToList();


            string folderPath = Path.Combine("Payslips", DateTime.Now.ToString("yyyy-MM"));
            Directory.CreateDirectory(folderPath);

            string templatePath = Path.Combine(AppContext.BaseDirectory, "HTML", "payslip-template1.html");
            string htmlTemplate = File.ReadAllText(templatePath);

            var tempFiles = new List<string>();

            // Generate PDFs in parallel
            //await Parallel.ForEachAsync(employeeGroups, new ParallelOptions
            //{
            //    MaxDegreeOfParallelism = Environment.ProcessorCount,
            //    CancellationToken = stoppingToken
            //}, async (empGroup, ct) =>
            //{
            //    try
            //    {
            //        var emp = empGroup.First();
            //        string filePath = Path.Combine(folderPath, $"Payslip_{emp.EmpCode}_{DateTime.Now:yyyyMMdd}.pdf");

            //        string html = BuildPayslipHtml(empGroup, htmlTemplate);

            //        var doc = new HtmlToPdfDocument()
            //        {
            //            GlobalSettings = new GlobalSettings
            //            {
            //                ColorMode = ColorMode.Color,
            //                Orientation = Orientation.Portrait,
            //                PaperSize = PaperKind.A4,
            //                Out = filePath
            //            },
            //            Objects = { new ObjectSettings { HtmlContent = html, WebSettings = { DefaultEncoding = "utf-8" } } }
            //        };

            //        _converter.Convert(doc);

            //        lock (tempFiles) tempFiles.Add(filePath);

            //        _logger.LogInformation("Payslip generated: {filePath}", filePath);

            //        // Send individual email to employee
            //        //await SendEmailWithAttachmentAsync(emp, filePath);
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError(ex, "Error generating payslip for EmployeeCode: {EmpCode}", empGroup.Key);
            //    }
            //});

            //if (tempFiles.Any())
            //{
            //    string mergedFile = Path.Combine(folderPath, $"Payslips_Merged_{DateTime.Now:yyyyMMdd}.pdf");
            //    MergePdfs(tempFiles, mergedFile);

            //    _logger.LogInformation("Merged PDF created: {mergedFile}", mergedFile);

            //    //await SendMergedEmailToHRAsync(mergedFile);
            //}
            foreach (var empGroup in employees.GroupBy(e => e.EmpCode).OrderBy(g => g.Key))
            {
                var emp = empGroup.First();
                string filePath = Path.Combine(folderPath, $"Payslip_{emp.EmpCode}_{DateTime.Now:yyyyMMdd}.pdf");

                string html = BuildPayslipHtml(empGroup, htmlTemplate);

                var doc = new HtmlToPdfDocument
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

                tempFiles.Add(filePath);

                _logger.LogInformation("Payslip generated: {filePath}", filePath);
                //        // Send individual email to employee
                //        //await SendEmailWithAttachmentAsync(emp, filePath);
            }
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
        var previousMonth = DateTime.Now.AddMonths(-1);

        // Separate earnings & deductions
        var earnings = empHeads.Where(x => x.HeadType == "Earning")
                               .Select(x => new { x.HeadName, x.Amount })
                               .ToList();

        var deductions = empHeads.Where(x => x.HeadType == "Deduction")
                                 .Select(x => new { x.HeadName, x.Amount })
                                 .ToList();

        // Individual Gross components
        decimal basic = emp.BASIC;     // directly from PayslipDto
        decimal hra = emp.HRA;         // directly from PayslipDto
        decimal flexiPay = emp.FlexiPay; // directly from PayslipDto

        // Total Gross (optional)
        decimal grossRate = basic + hra + flexiPay;

        // Other Deduction Total
        decimal otherDeductionTotal = empHeads
        .Where(x => x.OtherDeductionTotal.HasValue && x.OtherDeductionTotal.Value > 0)
        .Select(x => x.OtherDeductionTotal.Value)
        .FirstOrDefault();

        decimal totalEarning = earnings.Sum(x => x.Amount);
        decimal totalDeduction = deductions.Sum(x => x.Amount);
        totalDeduction = totalDeduction + otherDeductionTotal;
        decimal netPay = totalEarning - totalDeduction;

        string netPayInWords = Helper.NumberToWords(netPay);
        string salaryPeriod = Helper.GetSalaryPeriod(previousMonth);

        var salaryRows = new StringBuilder();
        int maxRows = Math.Max(earnings.Count, deductions.Count);

        for (int i = 0; i < maxRows; i++)
        {
            var earn = i < earnings.Count ? earnings[i] : new { HeadName = "", Amount = 0m };
            var ded = i < deductions.Count ? deductions[i] : new { HeadName = "", Amount = 0m };

            string grossVal = earn.HeadName switch
            {
                "BASIC" => basic.ToString("C"),
                "HRA" => hra.ToString("C"),
                "Flexipay" => flexiPay.ToString("C"),
                _ => ""
            };

            salaryRows.AppendLine($@"
<tr>
    <td>{earn.HeadName}</td>
    <td>{grossVal}</td>
    <td>{(earn.Amount > 0 ? earn.Amount.ToString("C") : "")}</td>
    <td>{ded.HeadName}</td>
    <td style='text-align:right'>{(ded.Amount > 0 ? ded.Amount.ToString("C") : "")}</td>
</tr>");
        }

        // ✅ Add OtherDeductionTotal row after ALL deductions are printed
        if (otherDeductionTotal > 0)
        {
            salaryRows.AppendLine($@"
<tr>
    <td></td>
    <td></td>
    <td></td>
    <td><b>Notice Deductions</b></td>
    <td style='text-align:right'><b>{otherDeductionTotal:C}</b></td>
</tr>");
        }



        // Totals
        salaryRows.AppendLine($@"
<tr class=""total-row"">
    <th>Total Earnings</th>
    <td>{grossRate:C}</td>
    <td>{totalEarning:C}</td>
    <th>Total Deductions</th>
    <td>{totalDeduction:C}</td>
</tr>
<tr class=""net-pay-row"">
    <td colspan=""5"">Net Amount Payable Rs. {netPay:C} /=</td>
</tr>
<tr>
    <td colspan=""5"">(Rs. {netPayInWords} through Bank for the month {previousMonth:MMMM yyyy}.)</td>
</tr>");

        // Replace placeholders in HTML template
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
            .Replace("{{GrossRate}}", grossRate.ToString("C"))
            .Replace("{{DeductionAmount}}", emp.DeductionAmount.ToString("C"))
            .Replace("{{NetPayInWords}}", $"Rs. {netPayInWords} through Bank for {DateTime.Now:MMMM yyyy}.")
            .Replace("{{OpeningPL}}", emp.OpeningPL.ToString("F2"))
            .Replace("{{PLEarn}}", emp.PLEarn.ToString("F2"))
            .Replace("{{PLAvail}}", emp.PLAvail.ToString())
            .Replace("{{PLClosing}}", emp.PLClosing.ToString("F2"))
            .Replace("{{SalaryPeriod}}", salaryPeriod)
            .Replace("{{LeaveTaken}}", emp.PLAvail.ToString())
            .Replace("{{BankName}}", emp.BankName)
            .Replace("{{BankAccount}}", emp.BankAccount)
            .Replace("{{IFSCCode}}", emp.IFSCCode)
            .Replace("{{Aadhar}}", emp.AADHAR)
            .Replace("{{Email}}", emp.Email)
            .Replace("{{Date}}", DateTime.Now.ToString("dd-MMM-yyyy"))
            .Replace("{{Place}}", "Udaipur");
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
            EnableSsl = bool.Parse(smtpSection["EnableSsl"]),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };
        var previousMonth = DateTime.Now.AddMonths(-1);
        using var mail = new MailMessage
        {
            From = new MailAddress(smtpSection["From"], "HR Department"),
            Subject = $"Payslip for {previousMonth:MMMM yyyy}",
            Body = $"Dear {emp.EmpName},\n\nPlease find attached your payslip for {previousMonth:MMMM yyyy}.\n\nRegards,\nHR Department",
            IsBodyHtml = false
        };

        mail.To.Add("blalbohra@gmail.com");
        mail.CC.Add("aks.bispl@gmail.com");
        mail.CC.Add("blalbohra@gmail.com");
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            mail.Attachments.Add(new Attachment(filePath));
        }

        try
        {
            await client.SendMailAsync(mail);
            _logger.LogInformation("Payslip sent to {Email}", emp.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payslip to {Email}", emp.Email);
            throw; // or handle gracefully
        }
    }

    public async Task SendPayslipsManuallyAsync()
    {
        string mergedFile = Path.Combine("Payslips", DateTime.Now.ToString("yyyy-MM"),
                                         $"Payslips_Merged_{DateTime.Now:yyyyMMdd}.pdf");

        if (File.Exists(mergedFile))
        {
            await SendMergedEmailToHRAsync(mergedFile);
            _logger.LogInformation("Merged payslip email sent successfully: {file}", mergedFile);
        }
        else
        {
            _logger.LogWarning("No merged payslip file found: {file}", mergedFile);
        }
    }
    private async Task SendMergedEmailToHRAsync(string mergedFile)
    {
        var smtpSection = _configuration.GetSection("Email");

        using var client = new SmtpClient(smtpSection["Host"], int.Parse(smtpSection["Port"]))
        {
            Credentials = new NetworkCredential(smtpSection["User"], smtpSection["Password"]),
            EnableSsl = bool.Parse(smtpSection["EnableSsl"]),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };
        var previousMonth = DateTime.Now.AddMonths(-1);

        using var mail = new MailMessage
        {

            From = new MailAddress(smtpSection["From"], "Payroll System"),
            Subject = $"All Payslips for {previousMonth:MMMM yyyy}",
            Body = $"Dear HR,\n\nPlease find attached all employee payslips for {previousMonth:MMMM yyyy}.\n\nRegards,\nPayroll System",
            IsBodyHtml = false
        };

        mail.To.Add("blalbohra@gmail.com");
        mail.CC.Add("aks.bispl@gmail.com");
        //mail.CC.Add("blalbohra@gmail.com");

        if (!string.IsNullOrEmpty(mergedFile) && File.Exists(mergedFile))
        {
            mail.Attachments.Add(new Attachment(mergedFile));
        }
        else
        {
            _logger.LogWarning("Merged file not found: {File}", mergedFile);
            return;
        }

        try
        {
            await client.SendMailAsync(mail);
            _logger.LogInformation("Merged payslip email sent to HR.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send merged payslip email to HR.");
            throw;
        }
    }
    //    public async Task SendCompressedEmailAsync(string originalFilePath, string recipientEmail)
    //{
    //    // Step 1: Compress PDF
    //    string compressedFilePath = Path.Combine(Path.GetDirectoryName(originalFilePath),
    //        Path.GetFileNameWithoutExtension(originalFilePath) + "_Compressed.pdf");

    //    using (var reader = new PdfReader(originalFilePath))
    //    {
    //        using (var fs = new FileStream(compressedFilePath, FileMode.Create, FileAccess.Write))
    //        {
    //            using (var stamper = new PdfStamper(reader, fs, PdfWriter.VERSION_1_5))
    //            {
    //                stamper.Writer.CompressionLevel = PdfStream.BEST_COMPRESSION;
    //                for (int i = 1; i <= reader.NumberOfPages; i++)
    //                {
    //                    var page = stamper.GetUnderContent(i);
    //                }
    //            }
    //        }
    //    }

    //    // Check new size
    //    FileInfo fi = new FileInfo(compressedFilePath);
    //    double sizeMB = fi.Length / 1024.0 / 1024.0;
    //    Console.WriteLine($"Compressed PDF size: {sizeMB:F2} MB");

    //    // Step 2: Send email
    //    MailMessage mail = new MailMessage();
    //    mail.From = new MailAddress("you@example.com");
    //    mail.To.Add(recipientEmail);
    //    mail.Subject = "Payslip";
    //    mail.Body = "Please find your compressed payslip attached.";

    //    mail.Attachments.Add(new Attachment(compressedFilePath));

    //    using (SmtpClient smtp = new SmtpClient("smtp.example.com", 587))
    //    {
    //        smtp.Credentials = new System.Net.NetworkCredential("you@example.com", "password");
    //        smtp.EnableSsl = true;

    //        await smtp.SendMailAsync(mail);
    //    }

    //    Console.WriteLine("Email sent successfully!");
    //}


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
