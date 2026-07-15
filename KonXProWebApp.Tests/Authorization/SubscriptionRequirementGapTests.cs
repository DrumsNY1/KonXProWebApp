using KonXProWebApp.Authorization;
using KonXProWebApp.Data;
using KonXProWebApp.Models.PermitIntel;
using KonXProWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;

namespace KonXProWebApp.Tests.Authorization;

/// <summary>
/// Covers gaps not exercised by the original SubscriptionRequirementTests: PastDue subscriptions,
/// the top-tier (Agency) user succeeding against every lower policy, and unauthenticated principals.
/// </summary>
public class SubscriptionRequirementGapTests
{
    [Fact]
    public async Task Handler_PastDueUser_FailsRequirement()
    {
        // PastDue is not "Active" or "Trialing", so GetActiveSubscription returns null and the
        // handler never calls context.Succeed.
        var (handler, context) = await SetupHandler("u1", "Pro", "PastDue", "Starter");

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Theory]
    [InlineData("Starter")]
    [InlineData("Pro")]
    [InlineData("Business")]
    [InlineData("Agency")]
    public async Task Handler_AgencyUser_SucceedsForAllTierRequirements(string required)
    {
        var (handler, context) = await SetupHandler("u1", "Agency", "Active", required);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded, $"Agency should pass the {required} requirement");
    }

    [Fact]
    public async Task Handler_UnauthenticatedUser_Fails()
    {
        var services = new ServiceCollection();
        var dbOptions = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new db_9f8bee_konxdevContext(dbOptions);
        var mockNav = new Mock<NavigationManager>();
        services.AddScoped(_ => db);
        services.AddScoped(_ => new PermitIntelService(db, mockNav.Object));
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        var handler = new SubscriptionAuthorizationHandler(scopeFactory);
        var anonUser = new ClaimsPrincipal(new ClaimsIdentity()); // no NameIdentifier claim
        var requirement = new SubscriptionRequirement("Starter");
        var context = new AuthorizationHandlerContext(new[] { requirement }, anonUser, null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_ProUser_FailsAgencyRequirement()
    {
        var (handler, context) = await SetupHandler("u1", "Pro", "Active", "Agency");

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private async Task<(SubscriptionAuthorizationHandler Handler, AuthorizationHandlerContext Context)> SetupHandler(
        string userId, string tier, string status, string requiredTier)
    {
        var services = new ServiceCollection();

        var dbOptions = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new db_9f8bee_konxdevContext(dbOptions);

        dbContext.Subscriptions.Add(new Subscription
        {
            UserId = userId,
            Tier = tier,
            Status = status,
            StartDate = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var mockNav = new Mock<NavigationManager>();
        services.AddScoped(_ => dbContext);
        services.AddScoped(_ => new PermitIntelService(dbContext, mockNav.Object));

        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var handler = new SubscriptionAuthorizationHandler(scopeFactory);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "TestAuth"));

        var requirement = new SubscriptionRequirement(requiredTier);
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        return (handler, context);
    }
}
