using KonXProWebApp.Data;
using KonXProWebApp.Models.PermitIntel;
using KonXProWebApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KonXProWebApp.Tests.Services;

public class StripeServiceTests : IDisposable
{
    private readonly db_9f8bee_konxdevContext _context;
    private readonly SubscriptionTierService _tierService;

    public StripeServiceTests()
    {
        var options = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new db_9f8bee_konxdevContext(options);
        _tierService = new SubscriptionTierService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public void GetTierInfo_ReturnsAllConfiguredTiers()
    {
        var tiers = StripeService.GetTierInfo();

        Assert.NotNull(tiers);
        Assert.True(tiers.Count >= 4);
        Assert.Contains(tiers, t => t.Name == "Starter" && t.Price == 29);
        Assert.Contains(tiers, t => t.Name == "Pro" && t.Price == 79);
        Assert.Contains(tiers, t => t.Name == "Business" && t.Price == 149);
        Assert.Contains(tiers, t => t.Name == "Agency" && t.Price == 299);
    }

    [Fact]
    public void TierSatisfiesRequirement_HierarchicalCheck_EvaluatesCorrectly()
    {
        Assert.True(SubscriptionTierService.TierSatisfiesRequirement("Pro", "Starter"));
        Assert.True(SubscriptionTierService.TierSatisfiesRequirement("Business", "Pro"));
        Assert.True(SubscriptionTierService.TierSatisfiesRequirement("Agency", "Pro"));
        Assert.False(SubscriptionTierService.TierSatisfiesRequirement("Free", "Starter"));
        Assert.False(SubscriptionTierService.TierSatisfiesRequirement("Starter", "Pro"));
    }

    [Fact]
    public async Task GetUserActiveTier_NoSubscription_DefaultsToFree()
    {
        var tier = await _tierService.GetUserActiveTier("non-existent-user");

        Assert.Equal("Free", tier);
    }

    [Fact]
    public async Task GetUserActiveTier_ActiveSubscription_ReturnsTierName()
    {
        _context.Subscriptions.Add(new Subscription
        {
            UserId = "user-123",
            Tier = "Pro",
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var tier = await _tierService.GetUserActiveTier("user-123");

        Assert.Equal("Pro", tier);
    }

    [Fact]
    public async Task HasFeatureAccess_FeatureRequirements_GatesCorrectly()
    {
        _context.Subscriptions.Add(new Subscription
        {
            UserId = "starter-user",
            Tier = "Starter",
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var canSearch = await _tierService.HasFeatureAccess("starter-user", "SearchPermits");
        var canExport = await _tierService.HasFeatureAccess("starter-user", "ExportData");
        var canMap = await _tierService.HasFeatureAccess("starter-user", "PermitMap");

        Assert.True(canSearch);
        Assert.False(canExport);
        Assert.False(canMap);
    }
}
