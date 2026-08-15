using System.Collections.Generic;

namespace KonXProWebApp.Models.PermitIntel;

public class PermitAnalyticsSummary
{
    public int TotalFilingsCount { get; set; }
    public decimal TotalJobCost { get; set; }
    public decimal AverageJobCost { get; set; }
    public double HotLeadsPercentage { get; set; }

    public List<BoroughMetric> BoroughMetrics { get; set; } = new();
    public List<JobTypeMetric> JobTypeMetrics { get; set; } = new();
    public List<ScoreTierMetric> ScoreMetrics { get; set; } = new();
}

public class BoroughMetric
{
    public string Borough { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalCost { get; set; }
}

public class JobTypeMetric
{
    public string JobType { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalCost { get; set; }
}

public class ScoreTierMetric
{
    public string Tier { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Count { get; set; }
}
