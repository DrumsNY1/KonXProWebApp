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

    public IAsyncEnumerable<SocrataPermitRecord> GetPermitsSince(DateTime? since)
    {
        // Cap at 50 pages (250K records) per run to stay within the 10-min function timeout.
        // The watermark only advances on records found, so subsequent runs pick up the rest.
        return GetRecordsSince<SocrataPermitRecord>("ic3t-wcy2.json", "dobrundate", since, null, maxPages: 50);
    }

    public IAsyncEnumerable<SocrataDobViolationRecord> GetDobViolationsSince(DateTime? since)
    {
        // Issue Date for DOB Violations
        return GetRecordsSince<SocrataDobViolationRecord>("cepu-5g8r.json", "issue_date", since, null);
    }

    public IAsyncEnumerable<SocrataHpdViolationRecord> GetHpdViolationsSince(DateTime? since)
    {
        return GetRecordsSince<SocrataHpdViolationRecord>("csn4-vhvf.json", "inspectiondate", since, null);
    }

    public IAsyncEnumerable<SocrataServiceRequest311> GetBuilding311ComplaintsSince(DateTime? since)
    {
        // Only get complaints that have a BBL (Building) and are relevant to building distress
        string extraWhere = "bbl IS NOT NULL AND complaint_type IN ('HEAT/HOT WATER', 'PLUMBING', 'WATER LEAK', 'General Construction/Plumbing')";
        return GetRecordsSince<SocrataServiceRequest311>("erm2-nwe9.json", "created_date", since, extraWhere);
    }

    public IAsyncEnumerable<SocrataContractorRecord> GetContractorsSince(DateTime? since)
    {
        string extraWhere = "business_category='Home Improvement Contractor'";
        return GetRecordsSince<SocrataContractorRecord>("w7w3-xahh.json", "license_creation_date", since, extraWhere);
    }

    private async IAsyncEnumerable<T> GetRecordsSince<T>(string resourceId, string dateColumn, DateTime? since, string extraWhere, int? maxPages = null)
    {
        int offset = 0;
        bool hasMore = true;
        int pageCount = 0;

        while (hasMore)
        {
            var query = BuildQuery(resourceId, dateColumn, since, offset, extraWhere);
            _logger.LogInformation("Fetching Socrata page at offset {Offset}: {Query}", offset, query);

            List<T> page = null;

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
                    page = await response.Content.ReadFromJsonAsync<List<T>>();
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
                pageCount++;
                if (maxPages.HasValue && pageCount >= maxPages.Value)
                {
                    _logger.LogInformation("Reached max pages limit ({MaxPages}). Will continue on next run.", maxPages.Value);
                    hasMore = false;
                }
            }
        }
    }

    private string BuildQuery(string resourceId, string dateColumn, DateTime? since, int offset, string extraWhere)
    {
        var queryParams = new List<string>
        {
            $"$limit={PageSize}",
            $"$offset={offset}",
            $"$order={dateColumn} ASC"
        };

        var whereClauses = new List<string>();

        if (since.HasValue)
        {
            whereClauses.Add($"{dateColumn}>'{since.Value:yyyy-MM-ddTHH:mm:ss.000}'");
        }

        if (!string.IsNullOrEmpty(extraWhere))
        {
            whereClauses.Add(extraWhere);
        }

        if (whereClauses.Count > 0)
        {
            var combinedWhere = string.Join(" AND ", whereClauses);
            queryParams.Add($"$where={HttpUtility.UrlEncode(combinedWhere)}");
        }

        return resourceId + "?" + string.Join("&", queryParams);
    }
}
