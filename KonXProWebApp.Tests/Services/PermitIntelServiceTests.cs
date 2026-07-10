using KonXProWebApp.Data;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;
using KonXProWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace KonXProWebApp.Tests.Services;

public class PermitIntelServiceTests : IDisposable
{
    private readonly db_9f8bee_konxdevContext _context;
    private readonly PermitIntelService _service;

    public PermitIntelServiceTests()
    {
        var options = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new db_9f8bee_konxdevContext(options);
        var mockNav = new Mock<NavigationManager>();
        _service = new PermitIntelService(_context, mockNav.Object);
    }

    public void Dispose() => _context.Dispose();

    // ── Lead Scoring Tests ──

    [Fact]
    public void ScorePermit_MinimalFiling_ReturnsOne()
    {
        var filing = new DobjobFiling { JobType = "A3" };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(1, score);
    }

    [Fact]
    public void ScorePermit_HighCostMajorAlteration_ScoresHigher()
    {
        var filing = new DobjobFiling
        {
            JobType = "A1",
            InitialCost = 75_000m,
            Plumbing = "X",
            Mechanical = "X"
        };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.True(score >= 4, $"Expected score >= 4, got {score}");
    }

    [Fact]
    public void ScorePermit_NewBuildingWithExpansion_ScoresHigher()
    {
        var filing = new DobjobFiling
        {
            JobType = "NB",
            InitialCost = 100_000m,
            ExistingDwellingUnits = "2",
            ProposedDwellingUnits = "10"
        };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.True(score >= 4, $"Expected score >= 4, got {score}");
    }

    [Fact]
    public void ScorePermit_MaxScore_ClampedAtFive()
    {
        var filing = new DobjobFiling
        {
            JobType = "NB",
            InitialCost = 200_000m,
            Plumbing = "X",
            Mechanical = "X",
            Boiler = "X",
            Sprinkler = "X",
            ExistingDwellingUnits = "1",
            ProposedDwellingUnits = "50"
        };
        var score = PermitIntelService.ScorePermit(filing);
        Assert.Equal(5, score);
    }

    // ── Saved Leads Tests ──

    [Fact]
    public async Task SaveLead_NewLead_CreatesRecord()
    {
        // Arrange - need a DobjobFiling first
        var filing = new DobjobFiling { JobType = "A1", Borough = "MANHATTAN" };
        _context.DobjobFilings.Add(filing);
        await _context.SaveChangesAsync();

        // Act
        var lead = await _service.SaveLead("user123", filing.Id);

        // Assert
        Assert.NotNull(lead);
        Assert.Equal("user123", lead.UserId);
        Assert.Equal(filing.Id, lead.DobjobFilingId);
        Assert.Equal("New", lead.Status);
    }

    [Fact]
    public async Task SaveLead_Duplicate_ReturnsExisting()
    {
        var filing = new DobjobFiling { JobType = "A1" };
        _context.DobjobFilings.Add(filing);
        await _context.SaveChangesAsync();

        var first = await _service.SaveLead("user123", filing.Id);
        var second = await _service.SaveLead("user123", filing.Id);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await _context.SavedLeads.CountAsync());
    }

    [Fact]
    public async Task UpdateLeadStatus_ValidLead_UpdatesStatus()
    {
        var filing = new DobjobFiling { JobType = "A1" };
        _context.DobjobFilings.Add(filing);
        await _context.SaveChangesAsync();

        var lead = await _service.SaveLead("user123", filing.Id);
        await _service.UpdateLeadStatus(lead.Id, "Contacted", "Called owner");

        var updated = await _context.SavedLeads.FindAsync(lead.Id);
        Assert.Equal("Contacted", updated.Status);
        Assert.Equal("Called owner", updated.Notes);
    }

    [Fact]
    public async Task DeleteLead_ValidLead_RemovesRecord()
    {
        var filing = new DobjobFiling { JobType = "A1" };
        _context.DobjobFilings.Add(filing);
        await _context.SaveChangesAsync();

        var lead = await _service.SaveLead("user123", filing.Id);
        await _service.DeleteLead(lead.Id);

        Assert.Equal(0, await _context.SavedLeads.CountAsync());
    }

    [Fact]
    public async Task GetSavedLeadCount_ReturnsCorrectCount()
    {
        var filing1 = new DobjobFiling { JobType = "A1" };
        var filing2 = new DobjobFiling { JobType = "NB" };
        _context.DobjobFilings.AddRange(filing1, filing2);
        await _context.SaveChangesAsync();

        await _service.SaveLead("user123", filing1.Id);
        await _service.SaveLead("user123", filing2.Id);
        await _service.SaveLead("user456", filing1.Id);

        var count = await _service.GetSavedLeadCount("user123");
        Assert.Equal(2, count);
    }

    // ── Alert Preference Tests ──

    [Fact]
    public async Task SaveAlertPreference_New_CreatesRecord()
    {
        var pref = new AlertPreference
        {
            UserId = "user123",
            Boroughs = "BROOKLYN,QUEENS",
            AlertChannel = "Email",
            AlertFrequency = "Daily",
            IsActive = true
        };

        await _service.SaveAlertPreference(pref);

        var saved = await _context.AlertPreferences.FirstOrDefaultAsync(a => a.UserId == "user123");
        Assert.NotNull(saved);
        Assert.Equal("BROOKLYN,QUEENS", saved.Boroughs);
    }

    [Fact]
    public async Task SaveAlertPreference_Existing_UpdatesRecord()
    {
        var pref = new AlertPreference
        {
            UserId = "user123",
            Boroughs = "BROOKLYN",
            AlertChannel = "Email",
            AlertFrequency = "Daily",
            IsActive = true
        };
        await _service.SaveAlertPreference(pref);

        // Update
        pref.Boroughs = "MANHATTAN,BRONX";
        pref.AlertChannel = "Both";
        await _service.SaveAlertPreference(pref);

        var count = await _context.AlertPreferences.CountAsync(a => a.UserId == "user123");
        Assert.Equal(1, count);

        var saved = await _context.AlertPreferences.FirstAsync(a => a.UserId == "user123");
        Assert.Equal("MANHATTAN,BRONX", saved.Boroughs);
        Assert.Equal("Both", saved.AlertChannel);
    }

    // ── Subscription Tests ──

    [Fact]
    public async Task GetActiveSubscription_ReturnsActiveOnly()
    {
        _context.Subscriptions.AddRange(
            new Subscription { UserId = "user123", Tier = "Starter", Status = "Canceled", StartDate = DateTime.UtcNow.AddDays(-60) },
            new Subscription { UserId = "user123", Tier = "Pro", Status = "Active", StartDate = DateTime.UtcNow.AddDays(-10) }
        );
        await _context.SaveChangesAsync();

        var sub = await _service.GetActiveSubscription("user123");
        Assert.NotNull(sub);
        Assert.Equal("Pro", sub.Tier);
        Assert.Equal("Active", sub.Status);
    }

    [Fact]
    public async Task GetActiveSubscription_NoActive_ReturnsNull()
    {
        _context.Subscriptions.Add(
            new Subscription { UserId = "user123", Tier = "Starter", Status = "Canceled", StartDate = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var sub = await _service.GetActiveSubscription("user123");
        Assert.Null(sub);
    }

    // ── Search Tests ──

    [Fact]
    public async Task SearchPermits_BoroughFilter_FiltersCorrectly()
    {
        _context.DobjobFilings.AddRange(
            new DobjobFiling { Borough = "BROOKLYN", JobType = "A1" },
            new DobjobFiling { Borough = "MANHATTAN", JobType = "A1" },
            new DobjobFiling { Borough = "BROOKLYN", JobType = "NB" }
        );
        await _context.SaveChangesAsync();

        var query = new PermitSearchQuery { Boroughs = new List<string> { "BROOKLYN" } };
        var (results, count) = await _service.SearchPermits(query);

        Assert.Equal(2, count);
        Assert.All(results, r => Assert.Equal("BROOKLYN", r.Borough));
    }

    [Fact]
    public async Task SearchPermits_CostRange_FiltersCorrectly()
    {
        _context.DobjobFilings.AddRange(
            new DobjobFiling { InitialCost = 5_000m, JobType = "A3" },
            new DobjobFiling { InitialCost = 50_000m, JobType = "A1" },
            new DobjobFiling { InitialCost = 200_000m, JobType = "NB" }
        );
        await _context.SaveChangesAsync();

        var query = new PermitSearchQuery { MinCost = 10_000m, MaxCost = 100_000m };
        var (results, count) = await _service.SearchPermits(query);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SearchPermits_Pagination_ReturnsCorrectPage()
    {
        for (int i = 0; i < 10; i++)
            _context.DobjobFilings.Add(new DobjobFiling { JobType = "A1", Borough = "BROOKLYN" });
        await _context.SaveChangesAsync();

        var query = new PermitSearchQuery { Skip = 3, Take = 5 };
        var (results, count) = await _service.SearchPermits(query);

        Assert.Equal(10, count);
        Assert.Equal(5, results.Count());
    }
}
