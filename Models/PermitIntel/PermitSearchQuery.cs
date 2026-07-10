using System;
using System.Collections.Generic;

namespace KonXProWebApp.Models.PermitIntel;

public class PermitSearchQuery
{
    public string SearchText { get; set; }

    public List<string> Boroughs { get; set; } = new();

    public List<string> JobTypes { get; set; } = new();

    public List<string> JobStatuses { get; set; } = new();

    public List<string> Trades { get; set; } = new();

    public decimal? MinCost { get; set; }

    public decimal? MaxCost { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public string BuildingType { get; set; }

    public int Skip { get; set; }

    public int Take { get; set; } = 25;

    public string OrderBy { get; set; } = "LatestActionDate desc";
}
