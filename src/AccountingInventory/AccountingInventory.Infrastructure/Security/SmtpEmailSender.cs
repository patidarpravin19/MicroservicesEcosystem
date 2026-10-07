using System.Net;
using System.Net.Mail;
using AccountingInventory.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace AccountingInventory.Infrastructure.Security;

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var section = configuration.GetSection("Email");
        var host = section["SmtpHost"];
        var from = section["From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Email SMTP settings are not configured.");
        using var client = new SmtpClient(host, int.TryParse(section["SmtpPort"], out var port) ? port : 587)
        {
            EnableSsl = !bool.TryParse(section["EnableSsl"], out var ssl) || ssl,
            Credentials = string.IsNullOrWhiteSpace(section["Username"])
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(section["Username"], section["Password"]),
        };
        using var message = new MailMessage(from, recipient, subject, htmlBody) { IsBodyHtml = true };
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}
