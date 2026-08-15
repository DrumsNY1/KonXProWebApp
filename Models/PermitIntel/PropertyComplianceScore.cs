using System.Collections.Generic;

namespace KonXProWebApp.Models.PermitIntel;

public class PropertyComplianceScore
{
    public string Bin { get; set; } = string.Empty;
    public int ComplianceScore { get; set; } = 100; // 100 = perfect, 1 = severe violations
    public string ComplianceLevel { get; set; } = "Compliant"; // Compliant, Moderate Risk, High Compliance Risk
    public int DobViolationsCount { get; set; }
    public int HpdClassACount { get; set; }
    public int HpdClassBCount { get; set; }
    public int HpdClassCCount { get; set; }
    public List<string> ComplianceFlags { get; set; } = new();
}
