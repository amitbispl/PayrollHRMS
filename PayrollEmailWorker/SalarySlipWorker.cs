public class SalerySlipWorker : BackgroundService
{
    private readonly PayslipGenerator _generator;

    public SalerySlipWorker(PayslipGenerator generator)
    {
        _generator = generator;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string templatePath = Path.Combine(AppContext.BaseDirectory, "HTML", "payslip-template.html");
        string outputPath = Path.Combine(AppContext.BaseDirectory, "Payslip.pdf");

        _generator.GeneratePayslip(templatePath, outputPath);

        return Task.CompletedTask;
    }
}
