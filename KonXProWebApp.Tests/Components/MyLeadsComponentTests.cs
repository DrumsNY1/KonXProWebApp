using Bunit;
using KonXProWebApp.Components.Pages.PermitIntel;
using KonXProWebApp.Data;
using KonXProWebApp.Models;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;
using KonXProWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Radzen;
using Xunit;

namespace KonXProWebApp.Tests.Components;

/// <summary>
/// bUnit tests for the saved-leads pipeline page (Components/Pages/PermitIntel/MyLeads.razor).
/// SecurityService.User has a private setter, so it's populated via reflection here — see
/// SubscribeComponentTests for the same pattern and rationale.
/// </summary>
public class MyLeadsComponentTests : TestContext
{
    private const string UserId = "leads_user_1";
    private readonly db_9f8bee_konxdevContext _context;

    public MyLeadsComponentTests()
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

        var security = Services.GetRequiredService<SecurityService>();
        typeof(SecurityService).GetProperty(nameof(SecurityService.User))!
            .SetValue(security, new ApplicationUser { Id = UserId, Name = UserId });
    }

    private void SeedLead(int jobNum, string status)
    {
        var filing = new DobjobFiling { JobNum = jobNum, Borough = "BROOKLYN", JobType = "A1", HouseNum = "1", StreetName = "Lead St" };
        _context.DobjobFilings.Add(filing);
        _context.SaveChanges();

        _context.SavedLeads.Add(new SavedLead
        {
            UserId = UserId,
            DobjobFilingId = filing.Id,
            Status = status,
            SavedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    [Fact]
    public void MyLeads_StatusButtons_ShowCorrectCounts()
    {
        SeedLead(800001, "New");
        SeedLead(800002, "New");
        SeedLead(800003, "Won");

        var cut = RenderComponent<MyLeads>();

        Assert.Contains("All (3)", cut.Markup);
        Assert.Contains("New (2)", cut.Markup);
        Assert.Contains("Won (1)", cut.Markup);
        Assert.Contains("Lost (0)", cut.Markup);
    }

    [Fact]
    public void MyLeads_FilterByStatus_ShowsOnlyMatchingLeads()
    {
        SeedLead(800004, "New");
        SeedLead(800005, "Won");
        SeedLead(800006, "New");

        var cut = RenderComponent<MyLeads>();

        var wonButton = cut.FindAll("button").First(b => b.TextContent.Contains("Won ("));
        wonButton.Click();

        // After filtering to "Won", the grid should show exactly one saved-lead row.
        var rows = cut.FindAll(".rz-data-row");
        Assert.Single(rows);
    }

    [Fact]
    public void MyLeads_NoLeads_RendersEmptyStateWithoutThrowing()
    {
        var cut = RenderComponent<MyLeads>();

        Assert.Contains("0 saved leads", cut.Markup);
    }
}
