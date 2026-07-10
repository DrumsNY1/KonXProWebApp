using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace KonXProWebApp.Functions.Services;

/// <summary>
/// Sends SMS notifications via Twilio REST API.
/// Only available for Pro+ tier subscribers.
/// </summary>
public class SmsService
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromNumber;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SmsService> _logger;
    private readonly bool _isConfigured;

    public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
    {
        _accountSid = configuration["TwilioAccountSid"] ?? "";
        _authToken = configuration["TwilioAuthToken"] ?? "";
        _fromNumber = configuration["TwilioFromNumber"] ?? "";
        _logger = logger;
        _isConfigured = !string.IsNullOrEmpty(_accountSid) && !string.IsNullOrEmpty(_authToken);

        if (_isConfigured)
        {
            _httpClient = new HttpClient();
            var credentials = Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes($"{_accountSid}:{_authToken}"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        }
    }

    /// <summary>
    /// Sends an SMS to the specified phone number.
    /// Returns true if sent successfully, false if SMS is not configured.
    /// </summary>
    public async Task<bool> SendSmsAsync(string toNumber, string message)
    {
        if (!_isConfigured)
        {
            _logger.LogWarning("Twilio SMS not configured. Skipping SMS to {To}", toNumber);
            return false;
        }

        try
        {
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{_accountSid}/Messages.json";
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("To", toNumber),
                new KeyValuePair<string, string>("From", _fromNumber),
                new KeyValuePair<string, string>("Body", message)
            });

            var response = await _httpClient.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS sent to {To}", toNumber);
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Twilio SMS failed: {StatusCode} {Error}", response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {To}", toNumber);
            return false;
        }
    }

    /// <summary>
    /// Builds a short SMS summary for a permit alert.
    /// </summary>
    public static string BuildAlertSms(int matchCount, string topAddress)
    {
        return $"NYC Permit Intel: {matchCount} new permit(s) match your alerts. Top: {topAddress}. View at konxpro.com/permit-intel/search";
    }
}
