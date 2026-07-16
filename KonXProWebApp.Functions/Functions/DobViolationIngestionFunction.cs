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
        string status = "Success";
        string errorMessage = null;

        try
        {
            // Just doing a full sync for the last 30 days to keep it simple, or based on watermark
            // For now let's say we pull the last 30 days of issue_date
            var since = DateTime.UtcNow.AddDays(-30);

            var batch = new List<Models.SocrataDobViolationRecord>();
            const int batchSize = 500;

            await foreach (var record in _socrataClient.GetDobViolationsSince(since))
            {
                batch.Add(record);

                if (!string.IsNullOrEmpty(record.IssueDate) &&
                    DateTime.TryParse(record.IssueDate, out var recordDate))
                {
                    if (!lastTimestamp.HasValue || recordDate > lastTimestamp.Value)
                        lastTimestamp = recordDate;
                }

                if (batch.Count >= batchSize)
                {
                    var (ins, upd, skip) = await _ingestionService.UpsertDobViolations(batch);
                    totalInserted += ins;
                    totalUpdated += upd;
                    totalSkipped += skip;
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
        }
        catch (Exception ex)
        {
            status = "Failed";
            errorMessage = ex.Message;
            _logger.LogError(ex, "Ingestion failed");
        }

        if (lastTimestamp == null && status == "Success")
        {
            _logger.LogInformation("No new DOB violations found. Watermark not advanced.");
        }
        await _ingestionService.LogIngestionRun(
            totalInserted, totalUpdated, totalSkipped,
            "DOBViolation_" + status, errorMessage, lastTimestamp);
    }
}
