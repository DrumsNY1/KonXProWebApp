using KonXProWebApp.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KonXProWebApp.Functions.Functions;

/// <summary>
/// Timer-triggered function that runs daily at 6 AM ET.
/// Polls the NYC Socrata API for new/updated DOB permit filings
/// and upserts them into the SQL Server database.
/// </summary>
public class PermitIngestionFunction
{
    private readonly SocrataClient _socrataClient;
    private readonly IngestionService _ingestionService;
    private readonly ILogger<PermitIngestionFunction> _logger;

    public PermitIngestionFunction(
        SocrataClient socrataClient,
        IngestionService ingestionService,
        ILogger<PermitIngestionFunction> logger)
    {
        _socrataClient = socrataClient;
        _ingestionService = ingestionService;
        _logger = logger;
    }

    /// <summary>
    /// Runs daily at 6:00 AM Eastern Time (10:00 UTC).
    /// CRON: second minute hour day month weekday
    /// </summary>
    [Function("PermitIngestionFunction")]
    public async Task Run(
        [TimerTrigger("0 0 10 * * *")] TimerInfo timerInfo)
    {
        await RunInternal();
    }

    [Function("PermitIngestionHttp")]
    public async Task RunHttp(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] Microsoft.AspNetCore.Http.HttpRequest req)
    {
        await RunInternal();
    }

    private async Task RunInternal()
    {
        _logger.LogInformation("PermitIngestionFunction started at {Time}", DateTime.UtcNow);

        int totalInserted = 0, totalUpdated = 0, totalSkipped = 0;
        DateTime? lastTimestamp = null;
        string status = "Success";
        string errorMessage = null;

        try
        {
            // Get watermark from last successful run, but never look back further than 30 days
            var since = await _ingestionService.GetLastIngestionTimestamp();
            var floor = DateTime.UtcNow.AddDays(-30);
            if (!since.HasValue || since.Value < floor)
            {
                since = floor;
            }
            _logger.LogInformation("Delta load since: {Since}", since?.ToString("o") ?? "(first run)");

            // Stream records from Socrata
            var batch = new List<Models.SocrataPermitRecord>();
            const int batchSize = 500;

            await foreach (var record in _socrataClient.GetPermitsSince(since))
            {
                batch.Add(record);

                // Track the latest dobrundate as our new watermark
                if (!string.IsNullOrEmpty(record.DobRunDate) &&
                    DateTime.TryParse(record.DobRunDate, out var recordDate))
                {
                    if (!lastTimestamp.HasValue || recordDate > lastTimestamp.Value)
                        lastTimestamp = recordDate;
                }

                // Process in batches of 500
                if (batch.Count >= batchSize)
                {
                    var (ins, upd, skip) = await _ingestionService.UpsertPermits(batch);
                    totalInserted += ins;
                    totalUpdated += upd;
                    totalSkipped += skip;
                    _logger.LogInformation(
                        "Batch processed: +{Inserted} inserted, ~{Updated} updated, -{Skipped} skipped",
                        ins, upd, skip);
                    batch.Clear();
                }
            }

            // Process remaining records
            if (batch.Count > 0)
            {
                var (ins, upd, skip) = await _ingestionService.UpsertPermits(batch);
                totalInserted += ins;
                totalUpdated += upd;
                totalSkipped += skip;
            }

            _logger.LogInformation(
                "Ingestion complete: {Inserted} inserted, {Updated} updated, {Skipped} skipped",
                totalInserted, totalUpdated, totalSkipped);
        }
        catch (Exception ex)
        {
            status = "Failed";
            errorMessage = ex.Message;
            _logger.LogError(ex, "Ingestion failed");
        }

        // Log the run — only advance watermark if we actually ingested records
        if (lastTimestamp == null && status == "Success")
        {
            _logger.LogInformation("No new records found. Watermark not advanced.");
        }
        await _ingestionService.LogIngestionRun(
            totalInserted, totalUpdated, totalSkipped,
            status, errorMessage, lastTimestamp);

        _logger.LogInformation("PermitIngestionFunction completed at {Time}. Status: {Status}",
            DateTime.UtcNow, status);
    }
}
