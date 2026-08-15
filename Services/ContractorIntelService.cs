using KonXProWebApp.Data;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace KonXProWebApp.Services;

public partial class ContractorIntelService
{
    private readonly db_9f8bee_konxdevContext context;

    public ContractorIntelService(db_9f8bee_konxdevContext context)
    {
        this.context = context;
    }

    public async Task<(IEnumerable<HomeImprovementContractor> Results, int TotalCount)> SearchContractors(ContractorSearchQuery query)
    {
        var items = context.HomeImprovementContractors.AsNoTracking().AsQueryable();

        // Text search across business name, DBA, license number, and borough
        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var search = query.SearchText.Trim();
            items = items.Where(i =>
                (i.BusinessName != null && i.BusinessName.Contains(search)) ||
                (i.DbaTradeName != null && i.DbaTradeName.Contains(search)) ||
                (i.LicenseNumber != null && i.LicenseNumber.Contains(search)) ||
                (i.AddressStreetName != null && i.AddressStreetName.Contains(search)) ||
                (i.Borough != null && i.Borough.Contains(search)));
        }

        // Borough filter
        if (query.Boroughs?.Any() == true)
        {
            items = items.Where(i => query.Boroughs.Contains(i.Borough));
        }

        // License status filter
        if (query.LicenseStatuses?.Any() == true)
        {
            items = items.Where(i => query.LicenseStatuses.Contains(i.LicenseStatus));
        }

        var totalCount = await items.CountAsync();

        // Ordering
        if (!string.IsNullOrWhiteSpace(query.OrderBy))
        {
            items = items.OrderBy(query.OrderBy);
        }

        var results = await items
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync();

        return (results, totalCount);
    }

    public async Task<HomeImprovementContractor> GetContractorById(int id)
    {
        return await context.HomeImprovementContractors
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<DobjobFiling>> GetContractorPermitFilings(string licenseNum, string businessName, int take = 50)
    {
        var query = context.DobjobFilings.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(licenseNum))
        {
            var cleanLicense = licenseNum.Trim();
            query = query.Where(f => f.ApplicantLicenseNum == cleanLicense || f.ApplicantLicenseNum.Contains(cleanLicense));
        }
        else if (!string.IsNullOrWhiteSpace(businessName))
        {
            var cleanName = businessName.Trim();
            query = query.Where(f => f.OwnersBusinessName.Contains(cleanName) || f.ApplicantsLastName.Contains(cleanName));
        }
        else
        {
            return new List<DobjobFiling>();
        }

        return await query
            .OrderByDescending(f => f.LatestActionDate)
            .Take(take)
            .ToListAsync();
    }

    public static ContractorRiskProfile EvaluateContractorRisk(
        HomeImprovementContractor contractor,
        int activePermitCount = 0,
        decimal totalJobVolume = 0m,
        int dobViolationsCount = 0,
        int hpdViolationsCount = 0)
    {
        var profile = new ContractorRiskProfile
        {
            TotalPermitFilingsCount = activePermitCount,
            TotalEstJobVolume = totalJobVolume,
            DobViolationsCount = dobViolationsCount,
            HpdViolationsCount = hpdViolationsCount
        };

        int score = 15; // Base score

        // Check License status
        if (contractor.LicenseStatus?.Equals("Active", StringComparison.OrdinalIgnoreCase) == true)
        {
            profile.IsLicenseActive = true;
            profile.TrustSignals.Add("Verified Active NYC DCA License");
        }
        else
        {
            profile.IsLicenseActive = false;
            profile.IsLicenseExpired = true;
            score += 35;
            profile.RiskFactors.Add($"License Status is '{contractor.LicenseStatus ?? "Inactive"}'");
        }

        // Check Expiration date
        if (contractor.LicenseExpirationDate.HasValue && contractor.LicenseExpirationDate.Value < DateTime.Today)
        {
            profile.IsLicenseExpired = true;
            score += 25;
            profile.RiskFactors.Add($"License expired on {contractor.LicenseExpirationDate.Value:MM/dd/yyyy}");
        }

        // Permit activity trust signal
        if (activePermitCount > 5)
        {
            score -= 10;
            profile.TrustSignals.Add($"High Permit Volume ({activePermitCount} recent filings)");
        }
        else if (activePermitCount > 0)
        {
            profile.TrustSignals.Add($"Active NYC Permit Filings ({activePermitCount} filings)");
        }

        // Job volume trust signal
        if (totalJobVolume >= 100_000m)
        {
            profile.TrustSignals.Add($"Track Record of High-Value Projects ({totalJobVolume:C0})");
        }

        // Violations impact
        if (dobViolationsCount >= 3)
        {
            score += 25;
            profile.RiskFactors.Add($"{dobViolationsCount} open DOB building violations");
        }
        else if (dobViolationsCount > 0)
        {
            score += 10;
            profile.RiskFactors.Add($"{dobViolationsCount} DOB violation on record");
        }

        if (hpdViolationsCount > 0)
        {
            score += 15;
            profile.RiskFactors.Add($"{hpdViolationsCount} HPD housing quality violations");
        }

        profile.RiskScore = Math.Clamp(score, 1, 100);

        profile.RiskLevel = profile.RiskScore switch
        {
            >= 50 => "High Risk",
            >= 30 => "Moderate Risk",
            _ => "Low Risk"
        };

        return profile;
    }
}
