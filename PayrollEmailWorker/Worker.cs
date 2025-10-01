using PayrollEmailWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly PdfService _pdfService;
    private DateTime _lastRunDate = DateTime.MinValue;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _pdfService = new PdfService();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var today = DateTime.Now.Date;

            if (today.Day == 1 && _lastRunDate != today)
            {
                _logger.LogInformation("Generating and sending payslips for {date}", today);

                var pdfBytes = _pdfService.GeneratePayslip("Amit Saini", 50000);

                // Create a scope to get IEmailService
                using (var scope = _serviceProvider.CreateScope())
                {
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    await emailService.SendEmailAsync(
                        "amit.bispl@gmail.com",
                        "Your Monthly Payslip",
                        "<h3>Hello Amit,</h3><p>Please find your payslip attached.</p>",
                        pdfBytes,
                        $"Payslip_{today:yyyy_MM}.pdf"
                    );
                }

                _lastRunDate = today;
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
