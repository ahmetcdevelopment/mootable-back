using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mootable.Application.Interfaces;
using System.Net;
using System.Net.Mail;

namespace Mootable.Infrastructure.Services;

/// <summary>
/// Email service implementation using SMTP
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly SmtpClient _smtpClient;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Configure SMTP client
        var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
        var smtpUser = _configuration["EmailSettings:SmtpUser"] ?? "";
        var smtpPassword = _configuration["EmailSettings:SmtpPassword"] ?? "";
        var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

        _fromEmail = _configuration["EmailSettings:FromEmail"] ?? "noreply@mootable.com";
        _fromName = _configuration["EmailSettings:FromName"] ?? "Mootable";

        _smtpClient = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPassword),
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
    {
        try
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            mailMessage.To.Add(to);

            // In development, log the email instead of sending
            if (_configuration["Environment"] == "Development" &&
                string.IsNullOrEmpty(_configuration["EmailSettings:SmtpUser"]))
            {
                _logger.LogInformation("Development Email - To: {To}, Subject: {Subject}", to, subject);
                _logger.LogDebug("Email Body: {Body}", body);
                return;
            }

            await _smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw new ApplicationException($"Failed to send email to {to}", ex);
        }
    }

    public async Task SendEmailWithAttachmentsAsync(
        string to,
        string subject,
        string body,
        List<string> attachments,
        bool isHtml = false)
    {
        try
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            mailMessage.To.Add(to);

            // Add attachments
            foreach (var attachmentPath in attachments)
            {
                if (File.Exists(attachmentPath))
                {
                    var attachment = new Attachment(attachmentPath);
                    mailMessage.Attachments.Add(attachment);
                }
                else
                {
                    _logger.LogWarning("Attachment file not found: {Path}", attachmentPath);
                }
            }

            // In development, log the email instead of sending
            if (_configuration["Environment"] == "Development" &&
                string.IsNullOrEmpty(_configuration["EmailSettings:SmtpUser"]))
            {
                _logger.LogInformation("Development Email - To: {To}, Subject: {Subject}, Attachments: {Count}",
                    to, subject, attachments.Count);
                return;
            }

            await _smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email with {Count} attachments sent successfully to {To}",
                attachments.Count, to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachments to {To}", to);
            throw new ApplicationException($"Failed to send email with attachments to {to}", ex);
        }
    }

    public async Task SendBulkEmailAsync(
        List<string> recipients,
        string subject,
        string body,
        bool isHtml = false)
    {
        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(5); // Limit concurrent sends to 5

        foreach (var recipient in recipients)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await SendEmailAsync(recipient, subject, body, isHtml);
                    await Task.Delay(100); // Small delay between sends
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        _logger.LogInformation("Bulk email sent to {Count} recipients", recipients.Count);
    }

    public void Dispose()
    {
        _smtpClient?.Dispose();
    }
}