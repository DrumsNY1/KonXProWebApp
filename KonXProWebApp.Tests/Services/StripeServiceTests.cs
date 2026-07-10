using KonXProWebApp.Data;
using KonXProWebApp.Models.PermitIntel;
using KonXProWebApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KonXProWebApp.Tests.Services;

public class StripeServiceTests : IDisposable
{
    private readonly db_9f8bee_konxdevContext _context;
    private readonly StripeService _service;

    public StripeServiceTests()
    {
        var options = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new db_9f8bee_konxdevContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:SecretKey"] = "sk_test_fake",
                ["Stripe:Prices:Starter"] = "price_starter",
                ["Stripe:Prices:Pro"] = "price_pro",
                ["Stripe:Prices:Business"] = "price_business",
                ["Stripe:Prices:Agency"] = "price_agency"
            })
            .Build();

        var mockLogger = new Mock<ILogger<StripeService>>();
        _service = new StripeService(config, _context, mockLogger.Object);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public void GetTierInfo_ReturnsFourTiers()
    {
        var tiers = StripeService.GetTierInfo();
        Assert.Equal(4, tiers.Count);
        Assert.Contains(tiers, t => t.Name == "Starter" && t.Price == 29);
        Assert.Contains(tiers, t => t.Name == "Pro" && t.Price == 79);
        Assert.Contains(tiers, t => t.Name == "Business" && t.Price == 149);
        Assert.Contains(tiers, t => t.Name == "Agency" && t.Price == 299);
    }

    [Fact]
    public void GetTierInfo_ProIsPopular()
    {
        var tiers = StripeService.GetTierInfo();
        var popular = tiers.Where(t => t.IsPopular).ToList();
        Assert.Single(popular);
        Assert.Equal("Pro", popular[0].Name);
    }

    [Fact]
    public void GetTierInfo_AllHaveTrialFeature()
    {
        var tiers = StripeService.GetTierInfo();
        Assert.All(tiers, t => Assert.Contains(t.Features, f => f.Contains("14-day free trial")));
    }

    [Fact]
    public async Task HandleCheckoutCompleted_CreatesSubscription()
    {
        var session = new Stripe.Checkout.Session
        {
            Id = "cs_test_123",
            CustomerId = "cus_test_123",
            SubscriptionId = "sub_test_123",
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = "user123",
                ["tier"] = "Pro"
            }
        };

        await _service.HandleCheckoutCompleted(session);

        var sub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == "user123");
        Assert.NotNull(sub);
        Assert.Equal("Pro", sub.Tier);
        Assert.Equal("Trialing", sub.Status);
        Assert.Equal("cus_test_123", sub.StripeCustomerId);
        Assert.Equal("sub_test_123", sub.StripeSubscriptionId);
        Assert.NotNull(sub.TrialEndDate);
    }

    [Fact]
    public async Task HandleCheckoutCompleted_SupersedesExistingSubscription()
    {
        // Add existing active subscription
        _context.Subscriptions.Add(new Subscription
        {
            UserId = "user123",
            Tier = "Starter",
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(-30)
        });
        await _context.SaveChangesAsync();

        var session = new Stripe.Checkout.Session
        {
            Id = "cs_test_456",
            CustomerId = "cus_test_456",
            SubscriptionId = "sub_test_456",
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = "user123",
                ["tier"] = "Pro"
            }
        };

        await _service.HandleCheckoutCompleted(session);

        var subs = await _context.Subscriptions.Where(s => s.UserId == "user123").ToListAsync();
        Assert.Equal(2, subs.Count);
        Assert.Single(subs, s => s.Status == "Superseded");
        Assert.Single(subs, s => s.Status == "Trialing" && s.Tier == "Pro");
    }

    [Fact]
    public async Task HandleSubscriptionUpdated_CanceledStatus()
    {
        _context.Subscriptions.Add(new Subscription
        {
            UserId = "user123",
            Tier = "Pro",
            Status = "Active",
            StripeSubscriptionId = "sub_123",
            StartDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var stripeSubscription = new Stripe.Subscription
        {
            Id = "sub_123",
            Status = "canceled"
        };

        await _service.HandleSubscriptionUpdated(stripeSubscription);

        var sub = await _context.Subscriptions.FirstAsync(s => s.StripeSubscriptionId == "sub_123");
        Assert.Equal("Canceled", sub.Status);
        Assert.NotNull(sub.EndDate);
    }
}
