using Microsoft.Playwright;
using Xunit;

namespace KonXProWebApp.E2E.Tests.Journeys;

[Collection("E2E")]
public class AnonymousAccessJourneyTests
{
    private readonly PlaywrightFixture _fixture;

    public AnonymousAccessJourneyTests(PlaywrightFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SubscribePage_ShowsAllPricingTiers()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/permit-intel/subscribe");

        // StripeService.GetTierInfo() returns 6 tiers: Starter, Pro, Business, Agency,
        // ComplianceAlerts, LandlordCompliance.
        await page.GetByText("Starter").First.WaitForAsync();
        await page.GetByText("Choose Your Plan").WaitForAsync();

        var pageText = await page.ContentAsync();
        foreach (var tierName in new[] { "Starter", "Pro", "Business", "Agency" })
        {
            Assert.Contains(tierName, pageText);
        }
    }

    [Fact]
    public async Task PermitSearch_Unauthenticated_RedirectsAwayFromProtectedPage()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/permit-intel/search");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // [Authorize] on PermitSearch should bounce an anonymous visitor away from the search page
        // itself (to a login or unauthorized page), not render the protected grid.
        Assert.DoesNotContain("/permit-intel/search", page.Url);
    }

    [Fact]
    public async Task MyLeads_Unauthenticated_RedirectsAwayFromProtectedPage()
    {
        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/permit-intel/leads");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.DoesNotContain("/permit-intel/leads", page.Url);
    }
}
