using KonXProWebApp.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KonXProWebApp.Functions.Functions;

/// <summary>
/// Timer-triggered function that runs daily at 11:00 AM UTC (7 AM ET).
/// Polls NYC 311 Service Requests (erm2-nwe9) for building-related complaints
/// (HEAT/HOT WATER, PLUMBING, WATER LEAK, General Construction/Plumbing)
/// and upserts them into the ServiceRequests311 table.
/// Only ingests records with a BBL (building identifier) to ensure
/// they can be matched to properties in our portfolio.
/// </summary>
public class ServiceRequest311IngestionFunction
{
    private readonly SocrataClient _socrataClient;
    private readonly IngestionService _ingestionService;
    private readonly ILogger<ServiceRequest311IngestionFunction> _logger;

    public ServiceRequest311IngestionFunction(
        SocrataClient socrataClient,
        IngestionService ingestionService,
        ILogger<ServiceRequest311IngestionFunction> logger)
    {
        _socrataClient = socrataClient;
        _ingestionService = ingestionService;
        _logger = logger;
    }

    /// <summary>
    /// Runs daily at 11:00 AM UTC (7:00 AM ET).
    /// CRON: second minute hour day month weekday
    /// </summary>
    [Function("ServiceRequest311IngestionFunction")]
    public async Task Run([TimerTrigger("0 0 11 * * *")] TimerInfo timerInfo)
    {
        await RunInternal();
    }

    [Function("ServiceRequest311IngestionHttp")]
    public async Task RunHttp([HttpTrigger(AuthorizationLevel.Function, "get", "post")] Microsoft.AspNetCore.Http.HttpRequest req)
    {
        await RunInternal();
    }

    private async Task RunInternal()
    {
        _logger.LogInformation("ServiceRequest311IngestionFunction started at {Time}", DateTime.UtcNow);

        int totalInserted = 0, totalUpdated = 0, totalSkipped = 0;
        DateTime? lastTimestamp = null;
        string status = "ServiceRequest311_Success";
        string errorMessage = null;

        try
        {
            // Use per-type watermark so 311 tracks independently from permits and violations.
            // Fall back to 45-day window on first run or after a long gap.
            var lastRun = await _ingestionService.GetLastIngestionTimestampByType("ServiceRequest311");
            var fallback = DateTime.UtcNow.AddDays(-45);
            var since = (lastRun.HasValue && lastRun.Value > fallback) ? lastRun.Value : fallback;

            _logger.LogInformation("311 delta load since: {Since}", since.ToString("o"));

            var batch = new List<Models.SocrataServiceRequest311>();
            const int batchSize = 500;

            await foreach (var record in _socrataClient.GetBuilding311ComplaintsSince(since))
            {
                batch.Add(record);

                if (record.CreatedDate.HasValue)
                {
                    if (!lastTimestamp.HasValue || record.CreatedDate.Value > lastTimestamp.Value)
                        lastTimestamp = record.CreatedDate.Value;
                }

                if (batch.Count >= batchSize)
                {
                    var (ins, upd, skip) = await _ingestionService.UpsertServiceRequests311(batch);
                    totalInserted += ins;
                    totalUpdated += upd;
                    totalSkipped += skip;
                    _logger.LogInformation("Batch: +{Ins} inserted, ~{Upd} updated, -{Skip} skipped", ins, upd, skip);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                var (ins, upd, skip) = await _ingestionService.UpsertServiceRequests311(batch);
                totalInserted += ins;
                totalUpdated += upd;
                totalSkipped += skip;
            }

            _logger.LogInformation(
                "311 ingestion complete: {Inserted} inserted, {Updated} updated, {Skipped} skipped",
                totalInserted, totalUpdated, totalSkipped);
        }
        catch (Exception ex)
        {
            status = "ServiceRequest311_Failed";
            errorMessage = ex.Message;
            _logger.LogError(ex, "311 service request ingestion failed");
        }

        if (lastTimestamp == null && status == "ServiceRequest311_Success")
        {
            _logger.LogInformation("No new 311 complaints found. Watermark not advanced.");
        }

        await _ingestionService.LogIngestionRun(
            totalInserted, totalUpdated, totalSkipped,
            status, errorMessage, lastTimestamp);
    }
}
