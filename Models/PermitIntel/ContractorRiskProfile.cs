using System.Collections.Generic;

namespace KonXProWebApp.Models.PermitIntel;

public class ContractorRiskProfile
{
    public string RiskLevel { get; set; } = "Low Risk"; // Low Risk, Moderate Risk, High Risk
    public int RiskScore { get; set; } = 15; // 1 (safest) to 100 (highest risk)
    public bool IsLicenseActive { get; set; } = true;
    public bool IsLicenseExpired { get; set; } = false;
    public int TotalPermitFilingsCount { get; set; }
    public decimal TotalEstJobVolume { get; set; }
    public int DobViolationsCount { get; set; }
    public int HpdViolationsCount { get; set; }
    public List<string> RiskFactors { get; set; } = new();
    public List<string> TrustSignals { get; set; } = new();
}
