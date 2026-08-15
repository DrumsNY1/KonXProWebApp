using System.Collections.Generic;

namespace KonXProWebApp.Models.PermitIntel;

public class LeadScoreFactor
{
    public string Name { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General"; // Cost, Violation, Complaint, Trade, JobType, Expansion
    public string BadgeStyle { get; set; } = "info"; // danger, warning, info, success
}

public class LeadScoreBreakdown
{
    public int TotalScore { get; set; }
    public int RawScore { get; set; }
    public string Tier { get; set; } = "Standard"; // Hot, High Priority, Medium Priority, Standard
    public List<LeadScoreFactor> Factors { get; set; } = new();

    public int CostPoints { get; set; }
    public int JobTypePoints { get; set; }
    public int TradePoints { get; set; }
    public int ComplaintPoints { get; set; }
    public int DobViolationPoints { get; set; }
    public int HpdViolationPoints { get; set; }
    public int ExpansionPoints { get; set; }
}
