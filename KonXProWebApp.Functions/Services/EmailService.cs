using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KonXProWebApp.Functions.Services;

/// <summary>
/// Sends email notifications via SMTP at mail.konxpro.com.
/// </summary>
public class EmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _smtpHost = configuration["SmtpHost"] ?? "mail.konxpro.com";
        _smtpPort = int.TryParse(configuration["SmtpPort"], out var port) ? port : 587;
        _smtpUser = configuration["SmtpUser"] ?? "alerts@konxpro.com";
        _smtpPassword = configuration["SmtpPassword"] ?? "";
        _fromEmail = configuration["SmtpFromEmail"] ?? "alerts@konxpro.com";
        _fromName = configuration["SmtpFromName"] ?? "NYC Permit Intel";
        _logger = logger;
    }

    /// <summary>
    /// Sends an HTML email to the specified recipient.
    /// </summary>
    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUser, _smtpPassword),
                EnableSsl = true,
                Timeout = 30000
            };

            var message = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {To}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", toEmail, subject);
            throw;
        }
    }

    /// <summary>
    /// Builds the daily alert digest HTML email body.
    /// </summary>
    public string BuildAlertDigestHtml(string userName, List<AlertPermitMatch> matches)
    {
        var rows = string.Join("\n", matches.Select(m => {
            var stars = new string('\u2B50', Math.Clamp(m.LeadScore, 1, 5));
            var tierBg = m.LeadScore >= 4 ? "#EF4444" : (m.LeadScore == 3 ? "#F59E0B" : "#2563EB");
            var tierText = m.ScoreTier?.ToUpper() ?? (m.LeadScore >= 4 ? "HOT" : "STANDARD");
            var factorHtml = !string.IsNullOrEmpty(m.FactorSummary)
                ? $"<br/><span style=\"color: #64748b; font-size: 11px;\">{m.FactorSummary}</span>"
                : "";

            return $@"
            <tr style=""border-bottom: 1px solid #eee;"">
                <td style=""padding: 12px 8px; white-space: nowrap;"">
                    <span style=""color: #F59E0B; font-weight: bold; font-size: 13px;"">{stars}</span><br/>
                    <span style=""display: inline-block; background-color: {tierBg}; color: #ffffff; padding: 2px 6px; border-radius: 4px; font-size: 10px; font-weight: bold; margin-top: 4px;"">{tierText}</span>
                </td>
                <td style=""padding: 12px 8px;""><strong>{m.Address}</strong><br/><span style=""color: #888; font-size: 12px;"">{m.Borough}</span>{factorHtml}</td>
                <td style=""padding: 12px 8px; font-weight: 600;"">{m.JobType}</td>
                <td style=""padding: 12px 8px; color: #059669; font-weight: 600;"">{m.EstCost}</td>
                <td style=""padding: 12px 8px; font-size: 12px;"">{m.Status}</td>
                <td style=""padding: 12px 8px; text-align: center;""><a href=""https://konxpro.com/permit-intel/detail/{m.PermitId}"" style=""display: inline-block; background-color: #2563EB; color: #ffffff; padding: 6px 12px; border-radius: 4px; text-decoration: none; font-size: 12px; font-weight: bold;"">View →</a></td>
            </tr>";
        }));

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background-color: #f5f5f5;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 680px; margin: 0 auto; background-color: #ffffff;"">
        <!-- Header -->
        <tr>
            <td style=""background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%); padding: 32px 24px; text-align: center;"">
                <h1 style=""color: #ffffff; margin: 0; font-size: 24px;"">🔥 NYC Permit Intel</h1>
                <p style=""color: #94a3b8; margin: 8px 0 0; font-size: 14px;"">Daily AI Lead Quality Alert Digest</p>
            </td>
        </tr>

        <!-- Greeting -->
        <tr>
            <td style=""padding: 24px;"">
                <p style=""font-size: 16px; color: #333; margin-top: 0;"">Hi {userName},</p>
                <p style=""font-size: 14px; color: #666; margin-bottom: 0;"">We identified <strong>{matches.Count}</strong> high-priority permit lead(s) matching your custom alert preferences:</p>
            </td>
        </tr>

        <!-- Results Table -->
        <tr>
            <td style=""padding: 0 24px;"">
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;"">
                    <tr style=""background-color: #f8fafc;"">
                        <th style=""padding: 10px 8px; text-align: left; font-size: 12px; color: #64748b;"">Score</th>
                        <th style=""padding: 10px 8px; text-align: left; font-size: 12px; color: #64748b;"">Address</th>
                        <th style=""padding: 10px 8px; text-align: left; font-size: 12px; color: #64748b;"">Type</th>
                        <th style=""padding: 10px 8px; text-align: left; font-size: 12px; color: #64748b;"">Est. Cost</th>
                        <th style=""padding: 10px 8px; text-align: left; font-size: 12px; color: #64748b;"">Status</th>
                        <th style=""padding: 10px 8px; text-align: center; font-size: 12px; color: #64748b;"">Action</th>
                    </tr>
                    {rows}
                </table>
            </td>
        </tr>

        <!-- CTA -->
        <tr>
            <td style=""padding: 28px 24px; text-align: center;"">
                <a href=""https://konxpro.com/permit-intel/search"" style=""display: inline-block; padding: 14px 36px; background-color: #2563EB; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 15px;"">Explore All Live Permit Leads →</a>
            </td>
        </tr>

        <!-- Footer -->
        <tr>
            <td style=""padding: 24px; background-color: #fafafa; text-align: center; border-top: 1px solid #eee;"">
                <p style=""font-size: 12px; color: #999; margin: 0;"">You're receiving this because you enabled alerts on <a href=""https://konxpro.com/permit-intel/alerts"" style=""color: #2563EB;"">NYC Permit Intel</a>.</p>
                <p style=""font-size: 12px; color: #999; margin: 4px 0 0;"">© 2026 KonX Pro. All rights reserved.</p>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}

/// <summary>
/// Represents a permit that matched a user's alert criteria.
/// </summary>
public class AlertPermitMatch
{
    public int PermitId { get; set; }
    public string Address { get; set; }
    public string Borough { get; set; }
    public string JobType { get; set; }
    public string EstCost { get; set; }
    public string Status { get; set; }
    public int LeadScore { get; set; }
    public string ScoreTier { get; set; } = "Standard";
    public string FactorSummary { get; set; }
}
