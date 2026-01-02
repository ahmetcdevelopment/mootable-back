namespace Mootable.Application.Interfaces;

/// <summary>
/// Email service interface
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send email asynchronously
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body</param>
    /// <param name="isHtml">Whether the body is HTML</param>
    /// <returns>Task</returns>
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = false);

    /// <summary>
    /// Send email with attachments asynchronously
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body</param>
    /// <param name="attachments">List of attachment file paths</param>
    /// <param name="isHtml">Whether the body is HTML</param>
    /// <returns>Task</returns>
    Task SendEmailWithAttachmentsAsync(
        string to,
        string subject,
        string body,
        List<string> attachments,
        bool isHtml = false);

    /// <summary>
    /// Send bulk emails asynchronously
    /// </summary>
    /// <param name="recipients">List of recipient email addresses</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body</param>
    /// <param name="isHtml">Whether the body is HTML</param>
    /// <returns>Task</returns>
    Task SendBulkEmailAsync(
        List<string> recipients,
        string subject,
        string body,
        bool isHtml = false);
}