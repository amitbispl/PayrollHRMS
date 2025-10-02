using DinkToPdf;
using DinkToPdf.Contracts;

public class PayslipGenerator
{
    private readonly IConverter _converter;

    public PayslipGenerator(IConverter converter)
    {
        _converter = converter;
    }

    public void GeneratePayslip(string templatePath, string outputPath)
    {
        // Load template
        string html = File.ReadAllText(templatePath);

        // Replace placeholders
        html = html.Replace("{{Month}}", "September 2025")
                   .Replace("{{EmployeeName}}", "John Doe")
                   .Replace("{{Department}}", "Finance")
                   .Replace("{{BasicSalary}}", "₹50,000")
                   .Replace("{{HRA}}", "₹10,000")
                   .Replace("{{Deductions}}", "₹5,000")
                   .Replace("{{NetPay}}", "₹55,000");

        // Configure PDF doc
        var doc = new HtmlToPdfDocument()
        {
            GlobalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Out = outputPath
            },
            Objects =
            {
                new ObjectSettings
                {
                    HtmlContent = html,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
        };

        _converter.Convert(doc);
    }
}
