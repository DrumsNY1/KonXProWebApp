using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using KonXProWebApp.Functions.Models;
using Microsoft.Extensions.Logging;

namespace KonXProWebApp.Functions.Services;

/// <summary>
/// Typed HTTP client for the NYC Socrata Open Data API.
/// Handles pagination, rate limiting, and error recovery.
/// </summary>
public class SocrataClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SocrataClient> _logger;
    private const int PageSize = 5000;
    private const int MaxRetries = 3;

    public SocrataClient(HttpClient httpClient, ILogger<SocrataClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Fetches all permit records updated after the given timestamp.
    /// Uses dobrundate for reliable delta loads.
    /// Paginates through results in pages of 5000.
    /// </summary>
    public async IAsyncEnumerable<SocrataPermitRecord> GetPermitsSince(DateTime? since)
    {
        int offset = 0;
        bool hasMore = true;

        while (hasMore)
        {
            var query = BuildQuery(since, offset);
            _logger.LogInformation("Fetching Socrata page at offset {Offset}: {Query}", offset, query);

            List<SocrataPermitRecord> page = null;

            for (int retry = 0; retry < MaxRetries; retry++)
            {
                try
                {
                    var response = await _httpClient.GetAsync(query);

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, retry + 1));
                        _logger.LogWarning("Rate limited by Socrata. Retrying in {Delay}s...", delay.TotalSeconds);
                        await Task.Delay(delay);
                        continue;
                    }

                    response.EnsureSuccessStatusCode();
                    page = await response.Content.ReadFromJsonAsync<List<SocrataPermitRecord>>();
                    break;
                }
                catch (HttpRequestException ex) when (retry < MaxRetries - 1)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retry + 1));
                    _logger.LogWarning(ex, "Socrata request failed. Retrying in {Delay}s...", delay.TotalSeconds);
                    await Task.Delay(delay);
                }
            }

            if (page == null || page.Count == 0)
            {
                hasMore = false;
                yield break;
            }

            _logger.LogInformation("Fetched {Count} records at offset {Offset}", page.Count, offset);

            foreach (var record in page)
            {
                yield return record;
            }

            if (page.Count < PageSize)
            {
                hasMore = false;
            }
            else
            {
                offset += PageSize;
            }
        }
    }

    /// <summary>
    /// Gets total count of records matching the since filter.
    /// </summary>
    public async Task<int> GetRecordCount(DateTime? since)
    {
        var whereClause = since.HasValue
            ? $"dobrundate>'{since.Value:yyyy-MM-ddTHH:mm:ss.000}'"
            : null;

        var queryString = whereClause != null
            ? $"?$select=count(*)+as+total&$where={HttpUtility.UrlEncode(whereClause)}"
            : "?$select=count(*)+as+total";

        var response = await _httpClient.GetAsync(queryString);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<Dictionary<string, string>>>();
        if (result?.Count > 0 && result[0].TryGetValue("total", out var totalStr))
        {
            return int.TryParse(totalStr, out var total) ? total : 0;
        }

        return 0;
    }

    private string BuildQuery(DateTime? since, int offset)
    {
        var queryParams = new List<string>
        {
            $"$limit={PageSize}",
            $"$offset={offset}",
            "$order=dobrundate ASC"
        };

        if (since.HasValue)
        {
            var whereClause = $"dobrundate>'{since.Value:yyyy-MM-ddTHH:mm:ss.000}'";
            queryParams.Add($"$where={HttpUtility.UrlEncode(whereClause)}");
        }

        return "?" + string.Join("&", queryParams);
    }
}
