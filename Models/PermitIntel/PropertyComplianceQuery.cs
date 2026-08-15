namespace KonXProWebApp.Models.PermitIntel;

public class PropertyComplianceQuery
{
    public string SearchText { get; set; } = string.Empty;
    public string Borough { get; set; } = string.Empty;
    public string Source { get; set; } = "All"; // All, DOB, HPD
    public string SeverityClass { get; set; } = "All"; // All, A, B, C
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 25;
}
