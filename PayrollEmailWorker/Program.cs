using QuestPDF.Infrastructure;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService() // Enable running as Windows Service
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        // Bind connection string from configuration
        string connectionString = context.Configuration.GetConnectionString("DefaultConnection");
        services.AddSingleton(connectionString);

        //// Bind Email settings
        //var emailSettings = context.Configuration.GetSection("Email").Get<EmailSettings>();
        //services.AddSingleton(emailSettings);

        // Register the Worker
        services.AddHostedService<PayslipWorker>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
    })
    .Build();


await host.RunAsync();
