using KonXProWebApp.Data;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;
using KonXProWebApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KonXProWebApp.Tests.Services;

public class ContractorIntelServiceTests : IDisposable
{
    private readonly db_9f8bee_konxdevContext _context;
    private readonly ContractorIntelService _service;

    public ContractorIntelServiceTests()
    {
        var options = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new db_9f8bee_konxdevContext(options);
        _service = new ContractorIntelService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task SearchContractors_TextSearch_ReturnsMatchingRecords()
    {
        _context.HomeImprovementContractors.AddRange(
            new HomeImprovementContractor { LicenseNumber = "123456", BusinessName = "Apex Builders LLC", Borough = "MANHATTAN", LicenseStatus = "Active" },
            new HomeImprovementContractor { LicenseNumber = "987654", BusinessName = "Empire Plumbing Corp", Borough = "BROOKLYN", LicenseStatus = "Active" }
        );
        await _context.SaveChangesAsync();

        var query = new ContractorSearchQuery { SearchText = "Apex" };
        var (results, count) = await _service.SearchContractors(query);

        Assert.Equal(1, count);
        Assert.Equal("123456", results.First().LicenseNumber);
    }

    [Fact]
    public async Task SearchContractors_BoroughAndStatusFilter_FiltersCorrectly()
    {
        _context.HomeImprovementContractors.AddRange(
            new HomeImprovementContractor { BusinessName = "Manhattan Contractor", Borough = "MANHATTAN", LicenseStatus = "Active" },
            new HomeImprovementContractor { BusinessName = "Brooklyn Active", Borough = "BROOKLYN", LicenseStatus = "Active" },
            new HomeImprovementContractor { BusinessName = "Brooklyn Expired", Borough = "BROOKLYN", LicenseStatus = "Expired" }
        );
        await _context.SaveChangesAsync();

        var query = new ContractorSearchQuery
        {
            Boroughs = new List<string> { "BROOKLYN" },
            LicenseStatuses = new List<string> { "Active" }
        };
        var (results, count) = await _service.SearchContractors(query);

        Assert.Equal(1, count);
        Assert.Equal("Brooklyn Active", results.First().BusinessName);
    }

    [Fact]
    public void EvaluateContractorRisk_ActiveLicenseNoViolations_ReturnsLowRisk()
    {
        var contractor = new HomeImprovementContractor
        {
            BusinessName = "Safe Builders",
            LicenseStatus = "Active",
            LicenseExpirationDate = DateTime.Today.AddYears(1)
        };

        var profile = ContractorIntelService.EvaluateContractorRisk(contractor, activePermitCount: 8, totalJobVolume: 250_000m);

        Assert.Equal("Low Risk", profile.RiskLevel);
        Assert.True(profile.RiskScore < 30);
        Assert.Contains(profile.TrustSignals, s => s.Contains("High Permit Volume"));
    }

    [Fact]
    public void EvaluateContractorRisk_ExpiredLicenseAndViolations_ReturnsHighRisk()
    {
        var contractor = new HomeImprovementContractor
        {
            BusinessName = "Risky Contracting Inc",
            LicenseStatus = "Expired",
            LicenseExpirationDate = DateTime.Today.AddDays(-30)
        };

        var profile = ContractorIntelService.EvaluateContractorRisk(contractor, activePermitCount: 1, dobViolationsCount: 4, hpdViolationsCount: 2);

        Assert.Equal("High Risk", profile.RiskLevel);
        Assert.True(profile.RiskScore >= 50);
        Assert.Contains(profile.RiskFactors, f => f.Contains("DOB building violations"));
    }
}
