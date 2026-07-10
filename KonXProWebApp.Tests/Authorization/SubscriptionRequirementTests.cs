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

public class SubscriptionRequirementTests
{
    [Theory]
    [InlineData("Starter", 1)]
    [InlineData("Pro", 2)]
    [InlineData("Business", 3)]
    [InlineData("Agency", 4)]
    [InlineData("Unknown", 0)]
    public void GetTierLevel_ReturnsCorrectLevel(string tier, int expected)
    {
        Assert.Equal(expected, SubscriptionRequirement.GetTierLevel(tier));
    }

    [Fact]
    public async Task Handler_ActiveProUser_SucceedsForStarterRequirement()
    {
        // Arrange
        var (handler, context) = await SetupHandler("user123", "Pro", "Active", "Starter");

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_StarterUser_FailsForProRequirement()
    {
        var (handler, context) = await SetupHandler("user123", "Starter", "Active", "Pro");

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_NoSubscription_Fails()
    {
        var services = new ServiceCollection();

        var dbOptions = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new db_9f8bee_konxdevContext(dbOptions);

        var mockNav = new Mock<NavigationManager>();
        services.AddScoped(_ => dbContext);
        services.AddScoped(_ => new PermitIntelService(dbContext, mockNav.Object));

        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var handler = new SubscriptionAuthorizationHandler(scopeFactory);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user_no_sub")
        }, "TestAuth"));

        var requirement = new SubscriptionRequirement("Starter");
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_TrialingUser_Succeeds()
    {
        var (handler, context) = await SetupHandler("user123", "Business", "Trialing", "Business");

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_CanceledUser_Fails()
    {
        var (handler, context) = await SetupHandler("user123", "Pro", "Canceled", "Starter");

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
