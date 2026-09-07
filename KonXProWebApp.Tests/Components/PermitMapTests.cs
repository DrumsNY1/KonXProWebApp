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

public class PermitMapTests : TestContext
{
    private const string UserId = "map_user_1";
    private readonly db_9f8bee_konxdevContext _context;

    public PermitMapTests()
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

    [Fact]
    public void PermitMap_LoadsWithSavedTradeAndBoroughCriteria()
    {
        // Arrange: Seed user's saved preferences (Brooklyn & Plumbing)
        _context.AlertPreferences.Add(new AlertPreference
        {
            UserId = UserId,
            Boroughs = "BROOKLYN",
            Trades = "Plumbing",
            JobTypes = "A1",
            MinCost = 50000m,
            AlertChannel = "Email",
            AlertFrequency = "Daily",
            IsActive = true
        });

        // Seed 1 matching Brooklyn Plumbing permit with GIS coords
        _context.DobjobFilings.Add(new DobjobFiling
        {
            Id = 1,
            JobNum = 1001,
            Borough = "BROOKLYN",
            JobType = "A1",
            Plumbing = "X",
            Mechanical = "X",
            InitialCost = 600000m,
            LeadScore = 4,
            Gislatitude = "40.6782",
            Gislongitude = "-73.9442",
            HouseNum = "100",
            StreetName = "Flatbush Ave"
        });

        // Seed 1 Manhattan Plumbing permit with GIS coords (different borough)
        _context.DobjobFilings.Add(new DobjobFiling
        {
            Id = 2,
            JobNum = 1002,
            Borough = "MANHATTAN",
            JobType = "A1",
            Plumbing = "X",
            InitialCost = 120000m,
            LeadScore = 4,
            Gislatitude = "40.7831",
            Gislongitude = "-73.9712",
            HouseNum = "200",
            StreetName = "Broadway"
        });

        // Seed 1 Brooklyn permit without Plumbing (different trade)
        _context.DobjobFilings.Add(new DobjobFiling
        {
            Id = 3,
            JobNum = 1003,
            Borough = "BROOKLYN",
            JobType = "A1",
            Mechanical = "X",
            InitialCost = 60000m,
            LeadScore = 4,
            Gislatitude = "40.6782",
            Gislongitude = "-73.9442",
            HouseNum = "300",
            StreetName = "Atlantic Ave"
        });

        _context.SaveChanges();

        // Act: Render PermitMap component
        var cut = RenderComponent<PermitMap>();

        // Assert: Wait for async OnInitializedAsync to complete and populate stats
        cut.WaitForAssertion(() =>
        {
            Assert.Null(cut.Instance.LastException);
            Assert.Equal("BROOKLYN", cut.Instance.SelectedBorough);
            Assert.Equal("Plumbing", cut.Instance.SelectedTrade);
            Assert.Equal(1, cut.Instance.MapPermitCount);

            var statValues = cut.FindAll(".stat-value");
            Assert.Equal(4, statValues.Count);
            Assert.Equal("1", statValues[0].TextContent.Trim()); // Pins Displayed
            Assert.Equal("1", statValues[1].TextContent.Trim()); // Hot Leads
            Assert.Equal("$600K", statValues[2].TextContent.Trim()); // Map Job Volume
            Assert.Equal("BROOKLYN", statValues[3].TextContent.Trim()); // Borough Focus

            var html = cut.Markup;
            Assert.Contains("BROOKLYN", html);
            Assert.Contains("Plumbing trade", html);
        });
    }

    [Fact]
    public void PermitMap_ExcludesFilingsWithoutGisCoordinates()
    {
        // Arrange: Seed permit with GIS and permit without GIS
        _context.DobjobFilings.Add(new DobjobFiling
        {
            Id = 10,
            JobNum = 2001,
            Borough = "QUEENS",
            JobType = "NB",
            InitialCost = 200000m,
            LeadScore = 4,
            Gislatitude = "40.7282",
            Gislongitude = "-73.7949",
            HouseNum = "10",
            StreetName = "Queens Blvd"
        });

        _context.DobjobFilings.Add(new DobjobFiling
        {
            Id = 11,
            JobNum = 2002,
            Borough = "QUEENS",
            JobType = "NB",
            InitialCost = 300000m,
            LeadScore = 4,
            Gislatitude = null, // Missing GIS
            Gislongitude = null,
            HouseNum = "20",
            StreetName = "Queens Blvd"
        });

        _context.SaveChanges();

        // Act
        var cut = RenderComponent<PermitMap>();

        // Assert: Only the record with GIS coordinates is counted as a map pin
        var statValues = cut.FindAll(".stat-value");
        Assert.Equal("1", statValues[0].TextContent.Trim());
    }

    [Fact]
    public async Task SearchPermits_MatchesAnyTradeWithOrLogic()
    {
        // Arrange: Seed 1 Plumbing only, 1 Mechanical only, 1 neither
        _context.DobjobFilings.Add(new DobjobFiling
        {
            Id = 20,
            JobNum = 3001,
            Borough = "MANHATTAN",
            Plumbing = "X",
            InitialCost = 50000m,
            Gislatitude = "40.7",
            Gislongitude = "-74.0"
        });
        _context.DobjobFilings.Add(new DobjobFiling
        {
            Id = 21,
            JobNum = 3002,
            Borough = "MANHATTAN",
            Mechanical = "X",
            InitialCost = 60000m,
            Gislatitude = "40.7",
            Gislongitude = "-74.0"
        });
        _context.DobjobFilings.Add(new DobjobFiling
        {
            Id = 22,
            JobNum = 3003,
            Borough = "MANHATTAN",
            Boiler = "X",
            InitialCost = 70000m,
            Gislatitude = "40.7",
            Gislongitude = "-74.0"
        });
        await _context.SaveChangesAsync();

        var service = Services.GetRequiredService<PermitIntelService>();

        // Act: Search for Plumbing OR Mechanical
        var (results, count) = await service.SearchPermits(new PermitSearchQuery
        {
            Trades = new() { "Plumbing", "Mechanical" },
            RequireGisCoordinates = true
        });

        // Assert: Should find both filings (2 items), not 0
        var list = results.ToList();
        Assert.Equal(2, count);
        Assert.Contains(list, f => f.Id == 20);
        Assert.Contains(list, f => f.Id == 21);
        Assert.DoesNotContain(list, f => f.Id == 22);
    }

    [Fact]
    public void PermitMap_ResetFilters_ClearsCriteriaAndReloads()
    {
        // Arrange: Seed user preference
        _context.AlertPreferences.Add(new AlertPreference
        {
            UserId = UserId,
            Boroughs = "BROOKLYN",
            Trades = "Plumbing",
            AlertChannel = "Email",
            AlertFrequency = "Daily",
            IsActive = true
        });

        // Seed 1 Brooklyn Plumbing permit
        _context.DobjobFilings.Add(new DobjobFiling
        {
            Id = 31,
            JobNum = 4001,
            Borough = "BROOKLYN",
            Plumbing = "X",
            Gislatitude = "40.67",
            Gislongitude = "-73.94"
        });

        // Seed 1 Manhattan Mechanical permit
        _context.DobjobFilings.Add(new DobjobFiling
        {
            Id = 32,
            JobNum = 4002,
            Borough = "MANHATTAN",
            Mechanical = "X",
            Gislatitude = "40.78",
            Gislongitude = "-73.97"
        });
        _context.SaveChanges();

        var cut = RenderComponent<PermitMap>();

        // Pre-condition: Initially filtered to 1 pin (Brooklyn Plumbing)
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("BROOKLYN", cut.Instance.SelectedBorough);
            Assert.Equal("Plumbing", cut.Instance.SelectedTrade);
            Assert.Equal(1, cut.Instance.MapPermitCount);
        });

        // Act: Click Reset button
        var resetButton = cut.FindAll("button").First(b => b.TextContent.Contains("Reset"));
        resetButton.Click();

        // Assert: Filters cleared and both permits now included (2 pins)
        cut.WaitForAssertion(() =>
        {
            Assert.Null(cut.Instance.SelectedBorough);
            Assert.Null(cut.Instance.SelectedTrade);
            Assert.Equal(2, cut.Instance.MapPermitCount);
        });
    }
}
