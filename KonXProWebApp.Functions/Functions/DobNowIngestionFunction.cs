using KonXProWebApp.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KonXProWebApp.Functions.Functions;

/// <summary>
/// Timer-triggered function that runs daily at 6:30 AM ET.
/// Polls the NYC Socrata API for DOB NOW: Build – Job Application Filings
/// and upserts them into the SQL Server database.
/// </summary>
public class DobNowIngestionFunction
{
    private readonly SocrataClient _socrataClient;
    private readonly IngestionService _ingestionService;
    private readonly ILogger<DobNowIngestionFunction> _logger;

    public DobNowIngestionFunction(
        SocrataClient socrataClient,
        IngestionService ingestionService,
        ILogger<DobNowIngestionFunction> logger)
    {
        _socrataClient = socrataClient;
        _ingestionService = ingestionService;
        _logger = logger;
    }

    /// <summary>
    /// Runs daily at 6:30 AM Eastern Time (10:30 UTC).
    /// CRON: second minute hour day month weekday
    /// </summary>
    [Function("DobNowIngestionFunction")]
    public async Task Run(
        [TimerTrigger("0 30 10 * * *")] TimerInfo timerInfo)
    {
        await RunInternal();
    }

    [Function("DobNowIngestionHttp")]
    public async Task RunHttp(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] Microsoft.AspNetCore.Http.HttpRequest req)
    {
        await RunInternal();
    }

    private async Task RunInternal()
    {
        _logger.LogInformation("DobNowIngestionFunction started at {Time}", DateTime.UtcNow);

        int totalInserted = 0, totalUpdated = 0, totalSkipped = 0;
        DateTime? lastTimestamp = null;
        string status = "DobNow_Success";
        string errorMessage = null;

        try
        {
            // DOB NOW filings: look back 90 days for the initial load,
            // then use the watermark for incremental loads
            var since = await _ingestionService.GetLastIngestionTimestamp();
            var floor = DateTime.UtcNow.AddDays(-90);
            if (!since.HasValue || since.Value < floor)
            {
                since = floor;
            }
            _logger.LogInformation("DOB NOW delta load since: {Since}", since?.ToString("o") ?? "(first run)");

            var batch = new List<KonXProWebApp.Functions.Models.SocrataDobNowRecord>();
            const int batchSize = 500;
            int totalFiltered = 0;

            // ── Lead-quality filters ──────────────────────────────────────
            var filingDateCutoff = DateTime.UtcNow.AddDays(-90);
            var fullyPermittedCutoff = DateTime.UtcNow.AddYears(-1);

            await foreach (var record in _socrataClient.GetDobNowFilingsSince(since))
            {
                // 1. Skip signed-off/completed permits
                if (record.FilingStatus is not null &&
                    (record.FilingStatus.Contains("Signed", StringComparison.OrdinalIgnoreCase) ||
                     record.FilingStatus.Equals("Withdrawn", StringComparison.OrdinalIgnoreCase)))
                {
                    totalFiltered++;
                    continue;
                }

                // 2. Skip permits fully issued over 1 year ago
                if (!string.IsNullOrEmpty(record.FirstPermitDate) &&
                    DateTime.TryParse(record.FirstPermitDate, out var permittedDate) &&
                    permittedDate < fullyPermittedCutoff)
                {
                    totalFiltered++;
                    continue;
                }

                // 3. Skip permits filed more than 90 days ago
                if (!string.IsNullOrEmpty(record.FilingDate) &&
                    DateTime.TryParse(record.FilingDate, out var filingDate) &&
                    filingDate < filingDateCutoff)
                {
                    totalFiltered++;
                    continue;
                }

                batch.Add(record);

                // Track the latest filing_date as our new watermark
                if (!string.IsNullOrEmpty(record.FilingDate) &&
                    DateTime.TryParse(record.FilingDate, out var recordDate))
                {
                    if (!lastTimestamp.HasValue || recordDate > lastTimestamp.Value)
                        lastTimestamp = recordDate;
                }

                // Process in batches of 500
                if (batch.Count >= batchSize)
                {
                    var (ins, upd, skip) = await _ingestionService.UpsertDobNowFilings(batch);
                    totalInserted += ins;
                    totalUpdated += upd;
                    totalSkipped += skip;
                    _logger.LogInformation(
                        "DOB NOW batch processed: +{Inserted} inserted, ~{Updated} updated, -{Skipped} skipped (filtered: {Filtered})",
                        ins, upd, skip, totalFiltered);
                    batch.Clear();
                }
            }

            // Process remaining batch
            if (batch.Count > 0)
            {
                var (ins, upd, skip) = await _ingestionService.UpsertDobNowFilings(batch);
                totalInserted += ins;
                totalUpdated += upd;
                totalSkipped += skip;
            }

            _logger.LogInformation(
                "DOB NOW ingestion complete: +{Inserted} inserted, ~{Updated} updated, -{Skipped} skipped, {Filtered} filtered",
                totalInserted, totalUpdated, totalSkipped, totalFiltered);
        }
        catch (Exception ex)
        {
            status = "DobNow_Failed";
            errorMessage = ex.Message;
            _logger.LogError(ex, "DOB NOW ingestion failed");
        }

        await _ingestionService.LogIngestionRun(
            totalInserted, totalUpdated, totalSkipped,
            status, errorMessage, lastTimestamp);
    }
}
