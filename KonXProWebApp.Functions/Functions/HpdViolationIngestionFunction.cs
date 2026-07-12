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
        _logger.LogInformation("HpdViolationIngestionFunction started at {Time}", DateTime.UtcNow);

        int totalInserted = 0, totalUpdated = 0, totalSkipped = 0;
        DateTime? lastTimestamp = null;
        string status = "Success";
        string errorMessage = null;

        try
        {
            // Pull violations created/updated in the last 30 days
            var since = DateTime.UtcNow.AddDays(-30);

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
        }
        catch (Exception ex)
        {
            status = "Failed";
            errorMessage = ex.Message;
            _logger.LogError(ex, "Ingestion failed");
        }

        await _ingestionService.LogIngestionRun(
            totalInserted, totalUpdated, totalSkipped,
            "HPDViolation_" + status, errorMessage, lastTimestamp ?? DateTime.UtcNow);
    }
}
