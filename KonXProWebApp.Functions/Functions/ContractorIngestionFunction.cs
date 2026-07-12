using KonXProWebApp.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KonXProWebApp.Functions.Functions;

/// <summary>
/// Timer-triggered function that runs daily at 3:00 AM ET.
/// Polls the NYC Socrata API for new/updated Home Improvement Contractors
/// and upserts them into the SQL Server database.
/// </summary>
public class ContractorIngestionFunction
{
    private readonly SocrataClient _socrataClient;
    private readonly IngestionService _ingestionService;
    private readonly ILogger<ContractorIngestionFunction> _logger;

    public ContractorIngestionFunction(
        SocrataClient socrataClient,
        IngestionService ingestionService,
        ILogger<ContractorIngestionFunction> logger)
    {
        _socrataClient = socrataClient;
        _ingestionService = ingestionService;
        _logger = logger;
    }

    /// <summary>
    /// Runs daily at 3:00 AM.
    /// CRON: second minute hour day month weekday
    /// </summary>
    [Function("ContractorIngestionFunction")]
    public async Task Run(
        [TimerTrigger("0 0 3 * * *")] TimerInfo timerInfo)
    {
        await RunInternal();
    }

    [Function("ContractorIngestionHttp")]
    public async Task RunHttp(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] Microsoft.AspNetCore.Http.HttpRequest req)
    {
        await RunInternal();
    }

    private async Task RunInternal()
    {
        _logger.LogInformation("ContractorIngestionFunction started at {Time}", DateTime.UtcNow);

        int totalInserted = 0, totalUpdated = 0, totalSkipped = 0;
        string status = "Success";
        string errorMessage = null;
        DateTime? lastTimestamp = null; // Could track this if desired

        try
        {
            var since = await _ingestionService.GetLastIngestionTimestamp();
            // Optional: you might want a separate watermark for Contractors versus Permits. 
            // The existing `GetLastIngestionTimestamp()` returns the global last success. 
            // If they run at different times, this might need refinement, but since the API query 
            // pulls by `license_creation_date` it will work as long as Socrata guarantees ordering.
            // For safety and to ingest all initial data, you could set `since = null` temporarily 
            // or pass it in. For now, we will query since `null` if we want full sync, 
            // or since `since` if we trust the watermark.

            // Since this is the first time we're running it, let's sync everything.
            // To be efficient later, we can implement a specific watermark.
            _logger.LogInformation("Syncing Home Improvement Contractors.");

            var batch = new List<Models.SocrataContractorRecord>();
            const int batchSize = 500;

            await foreach (var record in _socrataClient.GetContractorsSince(since))
            {
                batch.Add(record);

                if (!string.IsNullOrEmpty(record.LicenseIssueDate) &&
                    DateTime.TryParse(record.LicenseIssueDate, out var recordDate))
                {
                    if (!lastTimestamp.HasValue || recordDate > lastTimestamp.Value)
                        lastTimestamp = recordDate;
                }

                if (batch.Count >= batchSize)
                {
                    var (ins, upd, skip) = await _ingestionService.UpsertContractors(batch);
                    totalInserted += ins;
                    totalUpdated += upd;
                    totalSkipped += skip;
                    _logger.LogInformation(
                        "Batch processed: +{Inserted} inserted, ~{Updated} updated, -{Skipped} skipped",
                        ins, upd, skip);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                var (ins, upd, skip) = await _ingestionService.UpsertContractors(batch);
                totalInserted += ins;
                totalUpdated += upd;
                totalSkipped += skip;
            }

            _logger.LogInformation(
                "Contractor Ingestion complete: {Inserted} inserted, {Updated} updated, {Skipped} skipped",
                totalInserted, totalUpdated, totalSkipped);
        }
        catch (Exception ex)
        {
            status = "Failed";
            errorMessage = ex.Message;
            _logger.LogError(ex, "Contractor Ingestion failed");
        }

        await _ingestionService.LogIngestionRun(
            totalInserted, totalUpdated, totalSkipped,
            status, errorMessage, lastTimestamp ?? DateTime.UtcNow);

        _logger.LogInformation("ContractorIngestionFunction completed at {Time}. Status: {Status}",
            DateTime.UtcNow, status);
    }
}
