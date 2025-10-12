using MailKit.Net.Smtp;
using MimeKit;

namespace PayrollEmailWorker;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, byte[]? attachment = null, string? fileName = null);
}

public class EmailService : IEmailService
{
    private readonly string _from;
    private readonly string _smtpServer;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;

    public EmailService(IConfiguration configuration)
    {
        _from = configuration["Email:From"];
        _smtpServer = configuration["Email:SmtpServer"];
        _port = int.Parse(configuration["Email:Port"]);
        _username = configuration["Email:Username"];
        _password = configuration["Email:Password"];
    }

    public async Task SendEmailAsync(string to, string subject, string body, byte[]? attachment = null, string? fileName = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Payroll System", _from));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = body };

        if (attachment != null && !string.IsNullOrEmpty(fileName))
        {
            builder.Attachments.Add(fileName, attachment);
        }

        message.Body = builder.ToMessageBody();

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(_smtpServer, _port, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_username, _password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
