using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using Xunit;

namespace KonXProWebApp.Tests.E2E;

/// <summary>
/// Playwright End-to-End browser UI test suite for NYC Permit Intel features.
/// </summary>
public class PermitIntelE2ETests : PageTest
{
    private const string BaseUrl = "https://localhost:5001"; // Or local dev server port

    [Fact(Skip = "Requires running web host server")]
    public async Task PermitSearch_NavigatesAndFiltersGrid()
    {
        await Page.GotoAsync($"{BaseUrl}/permit-intel/search");

        // Verify page header
        var headerText = await Page.TextContentAsync("h1");
        Assert.Contains("NYC DOB Permit Intelligence", headerText);

        // Fill search text
        await Page.FillAsync("input[placeholder*='Address']", "Broadway");
        await Page.ClickAsync("button:has-text('Search')");

        // Verify table or grid contains results
        await Expect(Page.Locator(".rz-datatable")).ToBeVisibleAsync();
    }

    [Fact(Skip = "Requires running web host server")]
    public async Task PermitMap_LoadsLeafletContainerAndPins()
    {
        await Page.GotoAsync($"{BaseUrl}/permit-intel/map");

        // Verify map container element
        await Expect(Page.Locator("#permit-map")).ToBeVisibleAsync();

        // Verify KPI stats row is rendered
        await Expect(Page.Locator(".stats-row")).ToBeVisibleAsync();
    }

    [Fact(Skip = "Requires running web host server")]
    public async Task ContractorDirectory_SearchesContractorByText()
    {
        await Page.GotoAsync($"{BaseUrl}/permit-intel/contractors");

        // Verify header
        var headerText = await Page.TextContentAsync("h1");
        Assert.Contains("NYC Contractor Directory", headerText);

        // Search for contractor
        await Page.FillAsync("input[placeholder*='Business name']", "Apex");
        await Page.ClickAsync("button:has-text('Search')");

        // Confirm data grid response
        await Expect(Page.Locator(".rz-datatable")).ToBeVisibleAsync();
    }

    [Fact(Skip = "Requires running web host server")]
    public async Task Subscription_DisplaysPricingCardsAndTrialCTA()
    {
        await Page.GotoAsync($"{BaseUrl}/subscription");

        // Verify active tier card and pricing cards
        await Expect(Page.Locator("h1:has-text('Subscription')")).ToBeVisibleAsync();
        await Expect(Page.Locator("h3:has-text('Pro')")).ToBeVisibleAsync();
        await Expect(Page.Locator("button:has-text('Start 14-Day Free Trial')").First).ToBeVisibleAsync();
    }
}
