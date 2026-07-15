using Bunit;
using KonXProWebApp.Components.Pages.PermitIntel;
using KonXProWebApp.Data;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Radzen;
using Xunit;

namespace KonXProWebApp.Tests.Components;

/// <summary>
/// bUnit tests for the permit search feed (Components/Pages/PermitIntel/PermitSearch.razor).
/// Uses a real PermitIntelService backed by EF InMemory rather than mocking it, since the service
/// is a concrete class with non-virtual members that Moq can't usefully override.
/// </summary>
public class PermitSearchComponentTests : TestContext
{
    private readonly db_9f8bee_konxdevContext _context;

    public PermitSearchComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var options = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new db_9f8bee_konxdevContext(options);

        Services.AddRadzenComponents();
        Services.AddSingleton(_context);
        Services.AddSingleton(sp => new PermitIntelService(_context, sp.GetRequiredService<NavigationManager>()));
        Services.AddSingleton(new Mock<IHttpClientFactory>().Object);
        Services.AddSingleton(sp =>
            new SecurityService(sp.GetRequiredService<NavigationManager>(), sp.GetRequiredService<IHttpClientFactory>()));
        Services.AddSingleton(sp => new db_9f8bee_konxdevService(_context, sp.GetRequiredService<NavigationManager>()));
    }

    [Fact]
    public void PermitSearch_InitialLoad_RendersSeededPermit()
    {
        _context.DobjobFilings.Add(new DobjobFiling
        {
            JobNum = 700001,
            Borough = "BROOKLYN",
            HouseNum = "42",
            StreetName = "UNIQUE TEST STREET",
            JobType = "A1",
            LatestActionDate = DateTime.UtcNow
        });
        _context.SaveChanges();

        var cut = RenderComponent<PermitSearch>();

        Assert.Contains("UNIQUE TEST STREET", cut.Markup);
        Assert.Contains("700001", cut.Markup);
    }

    [Fact]
    public void PermitSearch_NoPermits_RendersEmptyGridWithoutThrowing()
    {
        var cut = RenderComponent<PermitSearch>();

        Assert.Contains("Live Permit Feed", cut.Markup);
    }

    [Fact]
    public void PermitSearch_SaveAsLead_WithoutLogin_ShowsWarningNotification()
    {
        _context.DobjobFilings.Add(new DobjobFiling
        {
            JobNum = 700002,
            Borough = "QUEENS",
            HouseNum = "1",
            StreetName = "NO LOGIN AVE",
            JobType = "A1",
            LatestActionDate = DateTime.UtcNow
        });
        _context.SaveChanges();

        var notificationService = Services.GetRequiredService<NotificationService>();

        var cut = RenderComponent<PermitSearch>();

        // Security.User defaults to the "Anonymous" user (Id == null), so SaveAsLead should warn
        // instead of calling PermitIntelService.SaveLead.
        var saveButton = cut.FindAll("button.action-btn").First(b => b.TextContent.Contains("⭐"));
        saveButton.Click();

        var captured = notificationService.Messages.LastOrDefault();
        Assert.NotNull(captured);
        Assert.Equal(NotificationSeverity.Warning, captured!.Severity);
        Assert.Equal(0, _context.SavedLeads.Count());
    }
}
