using KonXProWebApp.Data;
using KonXProWebApp.Models.PermitIntel;
using KonXProWebApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KonXProWebApp.Tests.Services;

/// <summary>
/// Covers gaps not exercised by the original StripeServiceTests: missing-metadata handling,
/// the full Stripe→local status mapping matrix, and unknown-tier/unknown-subscription edge cases.
/// </summary>
public class StripeServiceGapTests : IDisposable
{
    private readonly db_9f8bee_konxdevContext _context;
    private readonly StripeService _service;

    public StripeServiceGapTests()
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
    public async Task HandleCheckoutCompleted_MissingUserId_DoesNotCreateSubscription()
    {
        var session = new Stripe.Checkout.Session
        {
            Id = "cs_test_no_user",
            CustomerId = "cus_123",
            SubscriptionId = "sub_123",
            Metadata = new Dictionary<string, string>() // no userId key
        };

        await _service.HandleCheckoutCompleted(session);

        Assert.Equal(0, await _context.Subscriptions.CountAsync());
    }

    [Theory]
    [InlineData("active", "Active")]
    [InlineData("trialing", "Trialing")]
    [InlineData("past_due", "PastDue")]
    [InlineData("unpaid", "PastDue")]
    [InlineData("canceled", "Canceled")]
    public async Task HandleSubscriptionUpdated_StatusMappings_AreCorrect(
        string stripeStatus, string expectedLocalStatus)
    {
        _context.Subscriptions.Add(new Subscription
        {
            UserId = "u1",
            Tier = "Pro",
            Status = "Active",
            StripeSubscriptionId = $"sub_map_{stripeStatus}",
            StartDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var stripeSub = new Stripe.Subscription { Id = $"sub_map_{stripeStatus}", Status = stripeStatus };
        await _service.HandleSubscriptionUpdated(stripeSub);

        var local = await _context.Subscriptions.FirstAsync(s => s.StripeSubscriptionId == $"sub_map_{stripeStatus}");
        Assert.Equal(expectedLocalStatus, local.Status);
    }

    [Fact]
    public async Task HandleSubscriptionUpdated_UnrecognizedStripeStatus_PassesThroughVerbatim()
    {
        _context.Subscriptions.Add(new Subscription
        {
            UserId = "u1", Tier = "Pro", Status = "Active",
            StripeSubscriptionId = "sub_incomplete", StartDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var stripeSub = new Stripe.Subscription { Id = "sub_incomplete", Status = "incomplete_expired" };
        await _service.HandleSubscriptionUpdated(stripeSub);

        var local = await _context.Subscriptions.FirstAsync(s => s.StripeSubscriptionId == "sub_incomplete");
        Assert.Equal("incomplete_expired", local.Status);
    }

    [Fact]
    public async Task HandleSubscriptionUpdated_CanceledStatus_SetsEndDate()
    {
        _context.Subscriptions.Add(new Subscription
        {
            UserId = "u1", Tier = "Pro", Status = "Active",
            StripeSubscriptionId = "sub_cancel_gap", StartDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var stripeSub = new Stripe.Subscription { Id = "sub_cancel_gap", Status = "canceled" };
        await _service.HandleSubscriptionUpdated(stripeSub);

        var local = await _context.Subscriptions.FirstAsync(s => s.StripeSubscriptionId == "sub_cancel_gap");
        Assert.NotNull(local.EndDate);
    }

    [Fact]
    public async Task HandleSubscriptionUpdated_UnknownSubscriptionId_LogsWarningOnly()
    {
        var stripeSub = new Stripe.Subscription { Id = "sub_ghost", Status = "active" };

        var ex = await Record.ExceptionAsync(() => _service.HandleSubscriptionUpdated(stripeSub));

        Assert.Null(ex);
        Assert.Equal(0, await _context.Subscriptions.CountAsync());
    }

    [Fact]
    public async Task CreateCheckoutSession_UnknownTier_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateCheckoutSession("u1", "u1@test.com", "Diamond", "/success", "/cancel"));
    }
}
