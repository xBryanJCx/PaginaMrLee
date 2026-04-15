using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using MrLee.Web.Models;

namespace MrLee.Web.Services;

public sealed class EmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> options, ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public bool IsConfigured()
        => !string.IsNullOrWhiteSpace(_settings.Host)
        && _settings.Port > 0
        && !string.IsNullOrWhiteSpace(_settings.Username)
        && !string.IsNullOrWhiteSpace(_settings.Password)
        && !string.IsNullOrWhiteSpace(_settings.FromEmail);

    public async Task SendAsync(string to, string subject, string htmlBody, string? textBody = null)
    {
        if (!IsConfigured())
            throw new InvalidOperationException("El SMTP no está configurado. Debe completar Smtp:Password en appsettings o variables de entorno.");

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        if (!string.IsNullOrWhiteSpace(textBody))
        {
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain"));
        }

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        _logger.LogInformation("Enviando correo SMTP a {To} mediante {Host}:{Port}", to, _settings.Host, _settings.Port);
        await client.SendMailAsync(message);
    }
}
