using System.Collections.Generic;

namespace KonXProWebApp.Models.PermitIntel;

public class ContractorSearchQuery
{
    public string SearchText { get; set; } = string.Empty;
    public List<string> Boroughs { get; set; } = new();
    public List<string> LicenseStatuses { get; set; } = new();
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 25;
    public string OrderBy { get; set; } = "BusinessName";
}
