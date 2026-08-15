using KonXProWebApp.Data;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;
using KonXProWebApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KonXProWebApp.Tests.Services;

public class ComplianceIntelServiceTests : IDisposable
{
    private readonly db_9f8bee_konxdevContext _context;
    private readonly ComplianceIntelService _service;

    public ComplianceIntelServiceTests()
    {
        var options = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new db_9f8bee_konxdevContext(options);
        _service = new ComplianceIntelService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task SearchDobViolations_TextSearch_ReturnsMatchingRecords()
    {
        _context.DobViolations.AddRange(
            new DobViolation { Boro = 1, Bin = "1000001", HouseNumber = "100", Street = "Broadway", ViolationNumber = "DOB-001", Description = "Failure to maintain", ViolationCategory = "V", ViolationType = "V", ViolationTypeCode = "V1", Block = "100", Lot = "1", Number = "1" },
            new DobViolation { Boro = 3, Bin = "3000002", HouseNumber = "500", Street = "Grand St", ViolationNumber = "DOB-002", Description = "Work without permit", ViolationCategory = "V", ViolationType = "V", ViolationTypeCode = "V2", Block = "200", Lot = "2", Number = "2" }
        );
        await _context.SaveChangesAsync();

        var query = new PropertyComplianceQuery { SearchText = "Broadway" };
        var (results, count) = await _service.SearchDobViolations(query);

        Assert.Equal(1, count);
        Assert.Equal("DOB-001", results.First().ViolationNumber);
    }

    [Fact]
    public async Task SearchHpdViolations_SeverityClassFilter_FiltersClassC()
    {
        _context.HpdViolations.AddRange(
            new HpdViolation { ViolationId = "HPD-1", HouseNumber = "100", StreetName = "Broadway", Boro = "MANHATTAN", Class = "A" },
            new HpdViolation { ViolationId = "HPD-2", HouseNumber = "100", StreetName = "Broadway", Boro = "MANHATTAN", Class = "C" },
            new HpdViolation { ViolationId = "HPD-3", HouseNumber = "200", StreetName = "Main St", Boro = "BROOKLYN", Class = "B" }
        );
        await _context.SaveChangesAsync();

        var query = new PropertyComplianceQuery { SeverityClass = "C" };
        var (results, count) = await _service.SearchHpdViolations(query);

        Assert.Equal(1, count);
        Assert.Equal("HPD-2", results.First().ViolationId);
    }

    [Fact]
    public void EvaluatePropertyCompliance_NoViolations_Returns100Compliant()
    {
        var score = ComplianceIntelService.EvaluatePropertyCompliance("1000001", 0, new List<string>());

        Assert.Equal(100, score.ComplianceScore);
        Assert.Equal("Compliant", score.ComplianceLevel);
        Assert.Empty(score.ComplianceFlags);
    }

    [Fact]
    public void EvaluatePropertyCompliance_SevereClassCViolations_ReturnsHighRisk()
    {
        var hpdClasses = new List<string> { "C", "C", "B" };
        var score = ComplianceIntelService.EvaluatePropertyCompliance("1000001", dobViolationsCount: 2, hpdClasses: hpdClasses);

        Assert.True(score.ComplianceScore < 50);
        Assert.Equal("High Compliance Risk", score.ComplianceLevel);
        Assert.Contains(score.ComplianceFlags, f => f.Contains("Immediately Hazardous"));
    }
}
