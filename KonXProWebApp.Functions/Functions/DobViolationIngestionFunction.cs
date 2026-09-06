using KonXProWebApp.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KonXProWebApp.Functions.Functions;

public class DobViolationIngestionFunction
{
    private readonly SocrataClient _socrataClient;
    private readonly IngestionService _ingestionService;
    private readonly ILogger<DobViolationIngestionFunction> _logger;

    public DobViolationIngestionFunction(
        SocrataClient socrataClient,
        IngestionService ingestionService,
        ILogger<DobViolationIngestionFunction> logger)
    {
        _socrataClient = socrataClient;
        _ingestionService = ingestionService;
        _logger = logger;
    }

    [Function("DobViolationIngestionFunction")]
    public async Task Run([TimerTrigger("0 15 10 * * *")] TimerInfo timerInfo)
    {
        await RunInternal();
    }

    [Function("DobViolationIngestionHttp")]
    public async Task RunHttp([HttpTrigger(AuthorizationLevel.Function, "get", "post")] Microsoft.AspNetCore.Http.HttpRequest req)
    {
        await RunInternal();
    }

    private async Task RunInternal()
    {
        _logger.LogInformation("DobViolationIngestionFunction started at {Time}", DateTime.UtcNow);

        int totalInserted = 0, totalUpdated = 0, totalSkipped = 0;
        DateTime? lastTimestamp = null;
        string status = "DOBViolation_Success";
        string errorMessage = null;

        try
        {
            // Use per-type watermark so DOB Violations track independently.
            // Fall back to 45-day window on first run or after a long gap.
            var lastRun = await _ingestionService.GetLastIngestionTimestampByType("DOBViolation");
            var fallback = DateTime.UtcNow.AddDays(-45);
            var since = (lastRun.HasValue && lastRun.Value > fallback) ? lastRun.Value : fallback;

            _logger.LogInformation("DOB Violation delta load since: {Since}", since.ToString("o"));

            var batch = new List<Models.SocrataDobViolationRecord>();
            const int batchSize = 500;

            await foreach (var record in _socrataClient.GetDobViolationsSince(since))
            {
                batch.Add(record);

                var parsedDate = IngestionService.ParseDobDate(record.IssueDate);
                if (parsedDate.HasValue)
                {
                    if (!lastTimestamp.HasValue || parsedDate.Value > lastTimestamp.Value)
                        lastTimestamp = parsedDate.Value;
                }

                if (batch.Count >= batchSize)
                {
                    var (ins, upd, skip) = await _ingestionService.UpsertDobViolations(batch);
                    totalInserted += ins;
                    totalUpdated += upd;
                    totalSkipped += skip;
                    _logger.LogInformation("Batch: +{Ins} inserted, ~{Upd} updated, -{Skip} skipped", ins, upd, skip);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                var (ins, upd, skip) = await _ingestionService.UpsertDobViolations(batch);
                totalInserted += ins;
                totalUpdated += upd;
                totalSkipped += skip;
            }

            _logger.LogInformation(
                "DOB Violation ingestion complete: {Inserted} inserted, {Updated} updated, {Skipped} skipped",
                totalInserted, totalUpdated, totalSkipped);
        }
        catch (Exception ex)
        {
            status = "DOBViolation_Failed";
            errorMessage = ex.Message;
            _logger.LogError(ex, "DOB violation ingestion failed");
        }

        if (lastTimestamp == null && status == "DOBViolation_Success")
        {
            _logger.LogInformation("No new DOB violations found. Watermark not advanced.");
        }

        await _ingestionService.LogIngestionRun(
            totalInserted, totalUpdated, totalSkipped,
            status, errorMessage, lastTimestamp);
    }
}
