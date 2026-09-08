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
        var saveButton = cut.FindAll("button.action-btn").First(b => b.GetAttribute("aria-label") == "Save as lead" || b.InnerHtml.Contains("star"));
        saveButton.Click();

        var captured = notificationService.Messages.LastOrDefault();
        Assert.NotNull(captured);
        Assert.Equal(NotificationSeverity.Warning, captured!.Severity);
        Assert.Equal(0, _context.SavedLeads.Count());
    }

    [Fact]
    public void FilterSummaryBar_HiddenWhenNoFiltersActive()
    {
        var cut = RenderComponent<PermitSearch>();

        // No filters active → bar should not be in the DOM
        Assert.DoesNotContain("filter-summary-bar", cut.Markup);
        Assert.DoesNotContain("Clear All", cut.Markup);
    }

    [Fact]
    public void FilterSummaryBar_ShowsCountAndResultsWhenFiltersActive()
    {
        _context.DobjobFilings.Add(new DobjobFiling
        {
            JobNum = 700010,
            Borough = "BROOKLYN",
            HouseNum = "100",
            StreetName = "FILTER TEST ST",
            JobType = "A1",
            LatestActionDate = DateTime.UtcNow
        });
        _context.SaveChanges();

        var cut = RenderComponent<PermitSearch>();

        // Programmatically toggle a borough filter
        var instance = cut.Instance;
        var brooklynOption = instance.GetType()
            .GetField("boroughOptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(instance) as System.Collections.IList;
        dynamic option = brooklynOption![1]; // "BROOKLYN"
        option.Selected = true;

        // Re-render after filter change
        cut.Render();

        Assert.Contains("filter-summary-bar", cut.Markup);
        Assert.Contains("1 filter active", cut.Markup);
        Assert.Contains("BROOKLYN", cut.Markup);
        Assert.Contains("Clear All", cut.Markup);
    }

    [Fact]
    public void ClearAllFilters_ResetsAllOptionsAndHidesBar()
    {
        var cut = RenderComponent<PermitSearch>();

        // Select a few filters via reflection
        var instance = cut.Instance;
        var boroughField = instance.GetType()
            .GetField("boroughOptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var boroughs = boroughField.GetValue(instance) as System.Collections.IList;
        dynamic manhattan = boroughs![0];
        manhattan.Selected = true;

        var tradeField = instance.GetType()
            .GetField("tradeOptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var trades = tradeField.GetValue(instance) as System.Collections.IList;
        dynamic plumbing = trades![0];
        plumbing.Selected = true;

        cut.Render();

        // Bar should be visible with 2 filters
        Assert.Contains("filter-summary-bar", cut.Markup);
        Assert.Contains("2 filters active", cut.Markup);

        // Click Clear All
        var clearBtn = cut.Find("button.filter-clear-all");
        clearBtn.Click();

        // After clear, bar should disappear
        Assert.DoesNotContain("filter-summary-bar", cut.Markup);
    }

    [Fact]
    public void FilterChips_HaveAriaPressedAndAccessibleRoles()
    {
        var cut = RenderComponent<PermitSearch>();

        // Find borough filter chips
        var firstChip = cut.FindAll("button.filter-chip")
            .First(b => b.GetAttribute("aria-label")?.StartsWith("Filter by") == true);

        Assert.Equal("false", firstChip.GetAttribute("aria-pressed"));
        Assert.DoesNotContain("is-selected", firstChip.ClassList);

        // Click to toggle
        firstChip.Click();

        // Re-query after re-render: should now be pressed and have is-selected class
        var updatedChip = cut.FindAll("button.filter-chip")
            .First(b => b.GetAttribute("aria-label")?.StartsWith("Filter by") == true);
        Assert.Equal("true", updatedChip.GetAttribute("aria-pressed"));
        Assert.Contains("is-selected", updatedChip.ClassList);
    }

    [Fact]
    public void SortableHeaders_RenderAccessibleButtonsWithAriaSort()
    {
        _context.DobjobFilings.Add(new DobjobFiling
        {
            JobNum = 700020,
            Borough = "BROOKLYN",
            HouseNum = "10",
            StreetName = "SORT TEST ST",
            JobType = "A1",
            LatestActionDate = DateTime.UtcNow
        });
        _context.SaveChanges();

        var cut = RenderComponent<PermitSearch>();

        // Header sort buttons should exist with aria-sort="none" initially
        var sortButtons = cut.FindAll("button.grid-sort-header-btn");
        Assert.NotEmpty(sortButtons);

        var permitNumBtn = sortButtons.First(b => b.TextContent.Contains("Permit #"));
        Assert.Equal("none", permitNumBtn.GetAttribute("aria-sort"));

        // Click to sort ascending
        permitNumBtn.Click();
        permitNumBtn = cut.FindAll("button.grid-sort-header-btn").First(b => b.TextContent.Contains("Permit #"));
        Assert.Equal("ascending", permitNumBtn.GetAttribute("aria-sort"));
        Assert.Contains("sorted ascending", permitNumBtn.GetAttribute("aria-label"));

        // Click again to sort descending
        permitNumBtn.Click();
        permitNumBtn = cut.FindAll("button.grid-sort-header-btn").First(b => b.TextContent.Contains("Permit #"));
        Assert.Equal("descending", permitNumBtn.GetAttribute("aria-sort"));
        Assert.Contains("sorted descending", permitNumBtn.GetAttribute("aria-label"));
    }

    [Fact]
    public void ActionButtonsAndMobileCards_HaveAccessibleAttributes()
    {
        _context.DobjobFilings.Add(new DobjobFiling
        {
            JobNum = 700030,
            Borough = "MANHATTAN",
            HouseNum = "500",
            StreetName = "FIFTH AVE",
            JobDescription = "Full floor commercial interior renovation with structural framing.",
            JobType = "A1",
            LatestActionDate = DateTime.UtcNow
        });
        _context.SaveChanges();

        var cut = RenderComponent<PermitSearch>();

        // Check action buttons in desktop and mobile
        var saveBtns = cut.FindAll("button.action-btn").Where(b => b.GetAttribute("title") == "Save as lead").ToList();
        Assert.NotEmpty(saveBtns);
        Assert.All(saveBtns, b =>
        {
            Assert.Contains("Save as lead:", b.GetAttribute("aria-label"));
            Assert.Equal("false", b.GetAttribute("aria-pressed"));
        });

        var detailBtns = cut.FindAll("button.action-btn").Where(b => b.GetAttribute("title") == "View details").ToList();
        Assert.NotEmpty(detailBtns);
        Assert.All(detailBtns, b =>
        {
            Assert.Contains("View details for", b.GetAttribute("aria-label"));
            Assert.Contains("→", b.TextContent);
        });

        // Mobile cards should be keyboard accessible
        var mobileCards = cut.FindAll(".mobile-card");
        Assert.NotEmpty(mobileCards);
        var firstCard = mobileCards.First();
        Assert.Equal("0", firstCard.GetAttribute("tabindex"));
        Assert.Equal("button", firstCard.GetAttribute("role"));

        // Mobile description expander is an accessible button
        var descBtn = cut.Find("button.mobile-card-desc");
        Assert.NotNull(descBtn);
        Assert.Equal("false", descBtn.GetAttribute("aria-expanded"));
    }
}
