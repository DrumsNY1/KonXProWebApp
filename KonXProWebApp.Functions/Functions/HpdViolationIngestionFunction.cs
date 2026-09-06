using KonXProWebApp.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KonXProWebApp.Functions.Functions;

public class HpdViolationIngestionFunction
{
    private readonly SocrataClient _socrataClient;
    private readonly IngestionService _ingestionService;
    private readonly ILogger<HpdViolationIngestionFunction> _logger;

    public HpdViolationIngestionFunction(
        SocrataClient socrataClient,
        IngestionService ingestionService,
        ILogger<HpdViolationIngestionFunction> logger)
    {
        _socrataClient = socrataClient;
        _ingestionService = ingestionService;
        _logger = logger;
    }

    [Function("HpdViolationIngestionFunction")]
    public async Task Run([TimerTrigger("0 30 10 * * *")] TimerInfo timerInfo)
    {
        await RunInternal();
    }

    [Function("HpdViolationIngestionHttp")]
    public async Task RunHttp([HttpTrigger(AuthorizationLevel.Function, "get", "post")] Microsoft.AspNetCore.Http.HttpRequest req)
    {
        await RunInternal();
    }

    private async Task RunInternal()
    {
        _logger.LogInformation("HpdViolationIngestionFunction started at {Time}", DateTime.UtcNow);

        int totalInserted = 0, totalUpdated = 0, totalSkipped = 0;
        DateTime? lastTimestamp = null;
        string status = "HPDViolation_Success";
        string errorMessage = null;

        try
        {
            // Use per-type watermark so this function tracks independently from permits.
            // Fall back to 45-day window on first run or after a long gap.
            var lastRun = await _ingestionService.GetLastIngestionTimestampByType("HPDViolation");
            var fallback = DateTime.UtcNow.AddDays(-45);
            var since = (lastRun.HasValue && lastRun.Value > fallback) ? lastRun.Value : fallback;

            _logger.LogInformation("HPD delta load since: {Since}", since.ToString("o"));

            var batch = new List<Models.SocrataHpdViolationRecord>();
            const int batchSize = 500;

            await foreach (var record in _socrataClient.GetHpdViolationsSince(since))
            {
                batch.Add(record);

                if (!string.IsNullOrEmpty(record.InspectionDate) &&
                    DateTime.TryParse(record.InspectionDate, out var recordDate))
                {
                    if (!lastTimestamp.HasValue || recordDate > lastTimestamp.Value)
                        lastTimestamp = recordDate;
                }

                if (batch.Count >= batchSize)
                {
                    var (ins, upd, skip) = await _ingestionService.UpsertHpdViolations(batch);
                    totalInserted += ins;
                    totalUpdated += upd;
                    totalSkipped += skip;
                    _logger.LogInformation("Batch: +{Ins} inserted, ~{Upd} updated, -{Skip} skipped", ins, upd, skip);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                var (ins, upd, skip) = await _ingestionService.UpsertHpdViolations(batch);
                totalInserted += ins;
                totalUpdated += upd;
                totalSkipped += skip;
            }

            _logger.LogInformation(
                "HPD ingestion complete: {Inserted} inserted, {Updated} updated, {Skipped} skipped",
                totalInserted, totalUpdated, totalSkipped);
        }
        catch (Exception ex)
        {
            status = "HPDViolation_Failed";
            errorMessage = ex.Message;
            _logger.LogError(ex, "HPD violation ingestion failed");
        }

        if (lastTimestamp == null && status == "HPDViolation_Success")
        {
            _logger.LogInformation("No new HPD violations found. Watermark not advanced.");
        }

        await _ingestionService.LogIngestionRun(
            totalInserted, totalUpdated, totalSkipped,
            status, errorMessage, lastTimestamp);
    }
}
