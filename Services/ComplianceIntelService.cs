using KonXProWebApp.Data;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;
using Microsoft.EntityFrameworkCore;

namespace KonXProWebApp.Services;

public class ComplianceIntelService
{
    private readonly db_9f8bee_konxdevContext _context;

    public ComplianceIntelService(db_9f8bee_konxdevContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<DobViolation> Results, int TotalCount)> SearchDobViolations(PropertyComplianceQuery query)
    {
        var items = _context.DobViolations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var search = query.SearchText.Trim();
            items = items.Where(v =>
                (v.HouseNumber != null && v.HouseNumber.Contains(search)) ||
                (v.Street != null && v.Street.Contains(search)) ||
                (v.Bin != null && v.Bin.Contains(search)) ||
                (v.ViolationNumber != null && v.ViolationNumber.Contains(search)) ||
                (v.Description != null && v.Description.Contains(search)));
        }

        var totalCount = await items.CountAsync();
        var results = await items
            .OrderByDescending(v => v.IssueDate)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync();

        return (results, totalCount);
    }

    public async Task<(IEnumerable<HpdViolation> Results, int TotalCount)> SearchHpdViolations(PropertyComplianceQuery query)
    {
        var items = _context.HpdViolations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var search = query.SearchText.Trim();
            items = items.Where(v =>
                (v.HouseNumber != null && v.HouseNumber.Contains(search)) ||
                (v.StreetName != null && v.StreetName.Contains(search)) ||
                (v.Bin != null && v.Bin.Contains(search)) ||
                (v.ViolationId != null && v.ViolationId.Contains(search)) ||
                (v.NovDescription != null && v.NovDescription.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Borough))
        {
            items = items.Where(v => v.Boro != null && v.Boro.Contains(query.Borough));
        }

        if (!string.IsNullOrWhiteSpace(query.SeverityClass) && query.SeverityClass != "All")
        {
            items = items.Where(v => v.Class == query.SeverityClass);
        }

        var totalCount = await items.CountAsync();
        var results = await items
            .OrderByDescending(v => v.InspectionDate)
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync();

        return (results, totalCount);
    }

    public static PropertyComplianceScore EvaluatePropertyCompliance(
        string bin,
        int dobViolationsCount,
        List<string> hpdClasses)
    {
        var score = new PropertyComplianceScore
        {
            Bin = bin,
            DobViolationsCount = dobViolationsCount,
            HpdClassACount = hpdClasses?.Count(c => c == "A") ?? 0,
            HpdClassBCount = hpdClasses?.Count(c => c == "B") ?? 0,
            HpdClassCCount = hpdClasses?.Count(c => c == "C") ?? 0
        };

        int penalty = 0;

        if (dobViolationsCount > 0)
        {
            penalty += dobViolationsCount * 10;
            score.ComplianceFlags.Add($"{dobViolationsCount} DOB Building Violation(s)");
        }

        if (score.HpdClassCCount > 0)
        {
            penalty += score.HpdClassCCount * 20;
            score.ComplianceFlags.Add($"🚨 {score.HpdClassCCount} Class C Immediately Hazardous Violation(s)");
        }

        if (score.HpdClassBCount > 0)
        {
            penalty += score.HpdClassBCount * 8;
            score.ComplianceFlags.Add($"⚠️ {score.HpdClassBCount} Class B Hazardous Violation(s)");
        }

        if (score.HpdClassACount > 0)
        {
            penalty += score.HpdClassACount * 3;
            score.ComplianceFlags.Add($"📋 {score.HpdClassACount} Class A Non-Hazardous Violation(s)");
        }

        score.ComplianceScore = Math.Clamp(100 - penalty, 1, 100);

        score.ComplianceLevel = score.ComplianceScore switch
        {
            < 50 => "High Compliance Risk",
            < 80 => "Moderate Risk",
            _ => "Compliant"
        };

        return score;
    }
}
