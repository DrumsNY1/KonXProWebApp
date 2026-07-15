using System.Reflection;
using Bunit;
using KonXProWebApp.Components.Pages.PermitIntel;
using KonXProWebApp.Data;
using KonXProWebApp.Models;
using KonXProWebApp.Models.PermitIntel;
using KonXProWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Radzen;
using Xunit;

namespace KonXProWebApp.Tests.Components;

/// <summary>
/// bUnit tests for the Subscribe page (Components/Pages/PermitIntel/Subscribe.razor).
/// SecurityService.User has a private setter (it's normally populated over the wire by
/// ApplicationAuthenticationStateProvider), so authenticated-user scenarios set it via reflection —
/// there's no supported constructor/test seam for it otherwise.
/// </summary>
public class SubscribeComponentTests : TestContext
{
    private readonly db_9f8bee_konxdevContext _context;

    public SubscribeComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var options = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new db_9f8bee_konxdevContext(options);

        Services.AddRadzenComponents();
        Services.AddSingleton(_context);
        Services.AddSingleton(sp => new PermitIntelService(_context, sp.GetRequiredService<NavigationManager>()));

        var stripeConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Stripe:SecretKey"] = "sk_test_fake" })
            .Build();
        Services.AddSingleton(sp =>
            new StripeService(stripeConfig, _context, new Mock<ILogger<StripeService>>().Object));

        Services.AddSingleton(new Mock<IHttpClientFactory>().Object);
        Services.AddSingleton(sp =>
            new SecurityService(sp.GetRequiredService<NavigationManager>(), sp.GetRequiredService<IHttpClientFactory>()));

        Services.AddSingleton(sp => new db_9f8bee_konxdevService(_context, sp.GetRequiredService<NavigationManager>()));
    }

    private static void SetCurrentUser(SecurityService security, string userId)
    {
        var userProperty = typeof(SecurityService).GetProperty(nameof(SecurityService.User))!;
        userProperty.SetValue(security, new ApplicationUser { Id = userId, Name = userId, Email = $"{userId}@test.com" });
    }

    [Fact]
    public void Subscribe_RendersAllSixTierCards()
    {
        var cut = RenderComponent<Subscribe>();

        foreach (var tierName in new[] { "Starter", "Pro", "Business", "Agency", "ComplianceAlerts", "LandlordCompliance" })
        {
            Assert.Contains(tierName, cut.Markup);
        }
    }

    [Fact]
    public void Subscribe_UnauthenticatedUser_ClickingStartTrial_RedirectsToLogin()
    {
        var cut = RenderComponent<Subscribe>();
        var navMan = Services.GetRequiredService<NavigationManager>();

        var starterButton = cut.FindAll("button")
            .First(b => b.TextContent.Contains("Start Free Trial"));
        starterButton.Click();

        Assert.EndsWith("/Login", navMan.Uri.TrimEnd('/'));
    }

    [Fact]
    public void Subscribe_ActiveTierMatchesSubscription_ShowsCurrentPlanButton()
    {
        _context.Subscriptions.Add(new Subscription
        {
            UserId = "sub_user_1",
            Tier = "Pro",
            Status = "Active",
            StartDate = DateTime.UtcNow.AddDays(-3)
        });
        _context.SaveChanges();

        var security = Services.GetRequiredService<SecurityService>();
        SetCurrentUser(security, "sub_user_1");

        var cut = RenderComponent<Subscribe>();

        var currentPlanButtons = cut.FindAll("button")
            .Where(b => b.TextContent.Contains("Current Plan"))
            .ToList();

        Assert.Single(currentPlanButtons);
    }

    [Fact]
    public void Subscribe_ActiveSubscription_ShowsManageBillingCard()
    {
        _context.Subscriptions.Add(new Subscription
        {
            UserId = "sub_user_2",
            Tier = "Business",
            Status = "Trialing",
            StartDate = DateTime.UtcNow.AddDays(-1),
            TrialEndDate = DateTime.UtcNow.AddDays(13)
        });
        _context.SaveChanges();

        var security = Services.GetRequiredService<SecurityService>();
        SetCurrentUser(security, "sub_user_2");

        var cut = RenderComponent<Subscribe>();

        Assert.Contains("Current Plan: Business", cut.Markup);
        Assert.Contains("Manage Billing", cut.Markup);
    }
}
