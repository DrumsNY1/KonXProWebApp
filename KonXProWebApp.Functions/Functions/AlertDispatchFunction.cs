using KonXProWebApp.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace KonXProWebApp.Functions.Functions;

/// <summary>
/// Timer-triggered function that runs daily at 7 AM ET (11:00 UTC).
/// Matches new permits against user alert preferences and dispatches notifications.
/// </summary>
public class AlertDispatchFunction
{
    private readonly EmailService _emailService;
    private readonly SmsService _smsService;
    private readonly string _connectionString;
    private readonly ILogger<AlertDispatchFunction> _logger;

    public AlertDispatchFunction(
        EmailService emailService,
        SmsService smsService,
        IConfiguration configuration,
        ILogger<AlertDispatchFunction> logger)
    {
        _emailService = emailService;
        _smsService = smsService;
        _connectionString = configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException("SqlConnectionString not configured");
        _logger = logger;
    }

    /// <summary>
    /// Runs daily at 7:00 AM Eastern Time (11:00 UTC), one hour after ingestion.
    /// </summary>
    [Function("AlertDispatchFunction")]
    public async Task Run(
        [TimerTrigger("0 0 11 * * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation("AlertDispatchFunction started at {Time}", DateTime.UtcNow);

        int emailsSent = 0, smsSent = 0, errors = 0;

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Get all active alert preferences with user email
            var alertUsers = await GetActiveAlertUsers(connection);
            _logger.LogInformation("Found {Count} users with active alerts", alertUsers.Count);

            foreach (var user in alertUsers)
            {
                try
                {
                    // Find permits matching this user's criteria from last 24 hours
                    var matches = await GetMatchingPermits(connection, user);

                    if (matches.Count == 0)
                    {
                        _logger.LogDebug("No matches for user {UserId}", user.UserId);
                        continue;
                    }

                    _logger.LogInformation("Found {Count} matches for user {UserId}",
                        matches.Count, user.UserId);

                    // Send email if channel is Email or Both
                    if (user.AlertChannel is "Email" or "Both" && !string.IsNullOrEmpty(user.Email))
                    {
                        var html = _emailService.BuildAlertDigestHtml(user.UserName, matches);
                        await _emailService.SendEmailAsync(
                            user.Email,
                            $"NYC Permit Intel: {matches.Count} New Permit(s) Match Your Alerts",
                            html);
                        emailsSent++;
                    }

                    // Send SMS if channel is SMS or Both (Pro+ only)
                    if (user.AlertChannel is "SMS" or "Both" && !string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        var smsText = SmsService.BuildAlertSms(
                            matches.Count, matches.First().Address);
                        await _smsService.SendSmsAsync(user.PhoneNumber, smsText);
                        smsSent++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to dispatch alerts for user {UserId}", user.UserId);
                    errors++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AlertDispatchFunction failed");
        }

        _logger.LogInformation(
            "AlertDispatchFunction completed. Emails: {Emails}, SMS: {Sms}, Errors: {Errors}",
            emailsSent, smsSent, errors);
    }

    private async Task<List<AlertUser>> GetActiveAlertUsers(SqlConnection connection)
    {
        const string sql = @"
            SELECT ap.UserId, ap.Boroughs, ap.JobTypes, ap.Trades,
                   ap.MinCost, ap.MaxCost, ap.AlertChannel, ap.AlertFrequency,
                   u.Email, u.UserName, u.PhoneNumber
            FROM AlertPreferences ap
            INNER JOIN AspNetUsers u ON ap.UserId = u.Id
            WHERE ap.IsActive = 1 AND ap.AlertFrequency = 'Daily'";

        var users = new List<AlertUser>();
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(new AlertUser
            {
                UserId = reader.GetString(0),
                Boroughs = reader.IsDBNull(1) ? null : reader.GetString(1),
                JobTypes = reader.IsDBNull(2) ? null : reader.GetString(2),
                Trades = reader.IsDBNull(3) ? null : reader.GetString(3),
                MinCost = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                MaxCost = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                AlertChannel = reader.GetString(6),
                AlertFrequency = reader.GetString(7),
                Email = reader.IsDBNull(8) ? null : reader.GetString(8),
                UserName = reader.IsDBNull(9) ? "User" : reader.GetString(9),
                PhoneNumber = reader.IsDBNull(10) ? null : reader.GetString(10)
            });
        }

        return users;
    }

    private async Task<List<AlertPermitMatch>> GetMatchingPermits(
        SqlConnection connection, AlertUser user)
    {
        // Build dynamic WHERE clause based on user's preferences
        var conditions = new List<string> { "d.LatestActionDate >= DATEADD(day, -1, GETUTCDATE())" };
        var parameters = new List<SqlParameter>();

        if (!string.IsNullOrEmpty(user.Boroughs))
        {
            var boroughs = user.Boroughs.Split(',');
            var boroughParams = boroughs.Select((b, i) => $"@borough{i}").ToList();
            conditions.Add($"d.Borough IN ({string.Join(",", boroughParams)})");
            for (int i = 0; i < boroughs.Length; i++)
                parameters.Add(new SqlParameter($"@borough{i}", boroughs[i].Trim()));
        }

        if (!string.IsNullOrEmpty(user.JobTypes))
        {
            var types = user.JobTypes.Split(',');
            var typeParams = types.Select((t, i) => $"@jobType{i}").ToList();
            conditions.Add($"d.JobType IN ({string.Join(",", typeParams)})");
            for (int i = 0; i < types.Length; i++)
                parameters.Add(new SqlParameter($"@jobType{i}", types[i].Trim()));
        }

        if (user.MinCost.HasValue)
        {
            conditions.Add("d.InitialCost >= @minCost");
            parameters.Add(new SqlParameter("@minCost", user.MinCost.Value));
        }

        if (user.MaxCost.HasValue)
        {
            conditions.Add("d.InitialCost <= @maxCost");
            parameters.Add(new SqlParameter("@maxCost", user.MaxCost.Value));
        }

        // Trade filters
        if (!string.IsNullOrEmpty(user.Trades))
        {
            var trades = user.Trades.Split(',');
            var tradeConditions = trades.Select(t => t.Trim() switch
            {
                "Plumbing" => "d.Plumbing = 'X'",
                "Mechanical" => "d.Mechanical = 'X'",
                "Boiler" => "d.Boiler = 'X'",
                "Sprinkler" => "d.Sprinkler = 'X'",
                "Fire Alarm" => "d.FireAlarm = 'X'",
                "Standpipe" => "d.Standpipe = 'X'",
                "Equipment" => "d.Equipment = 'X'",
                "Fire Suppression" => "d.FireSuppression = 'X'",
                "Curb Cut" => "d.CurbCut = 'X'",
                _ => null
            }).Where(c => c != null);

            if (tradeConditions.Any())
                conditions.Add($"({string.Join(" OR ", tradeConditions)})");
        }

        var sql = $@"
            SELECT TOP 50 d.Id, d.HouseNum, d.StreetName, d.Borough,
                   d.JobType, d.InitialCost, d.JobStatusDescrp, d.LeadScore
            FROM DOBJobFilings d
            WHERE {string.Join(" AND ", conditions)}
            ORDER BY d.LeadScore DESC, d.InitialCost DESC";

        var matches = new List<AlertPermitMatch>();
        await using var cmd = new SqlCommand(sql, connection);
        foreach (var p in parameters) cmd.Parameters.Add(p);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            matches.Add(new AlertPermitMatch
            {
                PermitId = reader.GetInt32(0),
                Address = $"{(reader.IsDBNull(1) ? "" : reader.GetString(1))} {(reader.IsDBNull(2) ? "" : reader.GetString(2))}".Trim(),
                Borough = reader.IsDBNull(3) ? "" : reader.GetString(3),
                JobType = reader.IsDBNull(4) ? "" : reader.GetString(4),
                EstCost = reader.IsDBNull(5) ? "N/A" : reader.GetDecimal(5).ToString("C0"),
                Status = reader.IsDBNull(6) ? "" : reader.GetString(6),
                LeadScore = reader.IsDBNull(7) ? 1 : reader.GetInt32(7)
            });
        }

        return matches;
    }

    private class AlertUser
    {
        public string UserId { get; set; }
        public string Boroughs { get; set; }
        public string JobTypes { get; set; }
        public string Trades { get; set; }
        public decimal? MinCost { get; set; }
        public decimal? MaxCost { get; set; }
        public string AlertChannel { get; set; }
        public string AlertFrequency { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
    }
}
