using System.Net;
using System.Net.Mail;
using System.Text;
using System.IO;
using Örnek.Models;

namespace Örnek.Services;

public sealed class EmailService
{
    public void SendPdf(EmailSettings settings, string to, string subject, string body, byte[] pdfBytes, string attachmentName)
    {
        if (string.IsNullOrWhiteSpace(settings.SmtpHost))
            throw new InvalidOperationException("SMTP host ayarlı değil");

        if (string.IsNullOrWhiteSpace(settings.FromAddress))
            throw new InvalidOperationException("FromAddress ayarlı değil");

        using var message = new MailMessage();
        message.From = new MailAddress(settings.FromAddress, settings.FromName);
        message.To.Add(to);
        message.Subject = subject;
        message.Body = body;
        message.BodyEncoding = Encoding.UTF8;

        var attachmentStream = new MemoryStream(pdfBytes);
        var attachment = new Attachment(attachmentStream, attachmentName, "application/pdf");
        message.Attachments.Add(attachment);

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = settings.EnableSsl
        };

        client.UseDefaultCredentials = false;

        var user = string.IsNullOrWhiteSpace(settings.Username)
            ? settings.FromAddress
            : settings.Username;

        if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(settings.Password))
            client.Credentials = new NetworkCredential(user, settings.Password);

        client.Send(message);
    }
}
