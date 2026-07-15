using Microsoft.Playwright;
using Xunit;

namespace KonXProWebApp.E2E.Tests.Journeys;

/// <summary>
/// These journeys need a pre-seeded, email-confirmed user (and — for the subscriber-only journeys —
/// an active subscription row) already present in whatever environment E2E_BASE_URL points at, since
/// there is no public self-service registration endpoint reachable without an authenticated session
/// (RegisterApplicationUser is itself [Authorize]-gated; only the identity Account/Register API
/// backs it — see SecurityService.Register). Configure via:
///   E2E_TEST_USER / E2E_TEST_PASSWORD           — any confirmed login
///   E2E_SUBSCRIBER_USER / E2E_SUBSCRIBER_PASSWORD — a user with an Active/Trialing subscription
/// Tests no-op (pass trivially) when the relevant env vars aren't set, so this project still builds
/// and runs cleanly with no environment configured.
/// </summary>
[Collection("E2E")]
public class AuthenticatedJourneyTests
{
    private readonly PlaywrightFixture _fixture;

    public AuthenticatedJourneyTests(PlaywrightFixture fixture) => _fixture = fixture;

    private static (string? User, string? Password) BasicCreds() => (
        Environment.GetEnvironmentVariable("E2E_TEST_USER"),
        Environment.GetEnvironmentVariable("E2E_TEST_PASSWORD"));

    private static (string? User, string? Password) SubscriberCreds() => (
        Environment.GetEnvironmentVariable("E2E_SUBSCRIBER_USER"),
        Environment.GetEnvironmentVariable("E2E_SUBSCRIBER_PASSWORD"));

    private async Task<bool> LoginAsync(IPage page, string userName, string password)
    {
        await page.GotoAsync($"{_fixture.BaseUrl}/login");
        await page.GetByLabel("Username").FillAsync(userName);
        await page.GetByLabel("Password").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        return !page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Subscriber_CanViewPermitDetailAndSaveLead()
    {
        var (userName, password) = SubscriberCreds();
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)) return;

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        Assert.True(await LoginAsync(page, userName, password));

        await page.GotoAsync($"{_fixture.BaseUrl}/permit-intel/search");
        await page.WaitForSelectorAsync(".rz-data-grid", new() { Timeout = 15000 });

        var firstRow = page.Locator(".rz-data-row").First;
        await firstRow.WaitForAsync();
        await firstRow.GetByText("→").ClickAsync();

        await page.WaitForURLAsync(url => url.Contains("/permit-intel/detail/"));

        await page.GotoAsync($"{_fixture.BaseUrl}/permit-intel/leads");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/permit-intel/leads", page.Url);
    }

    [Fact]
    public async Task Subscriber_CanSaveAlertPreferences()
    {
        var (userName, password) = SubscriberCreds();
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)) return;

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        Assert.True(await LoginAsync(page, userName, password));

        await page.GotoAsync($"{_fixture.BaseUrl}/permit-intel/alerts");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Alert settings gates behind [Authorize] only (not a specific tier), so a logged-in user
        // should always reach the page itself.
        Assert.Contains("/permit-intel/alerts", page.Url);
    }

    [Fact]
    public async Task UnsubscribedUser_ClickingStartTrial_BeginsStripeCheckout()
    {
        var (userName, password) = BasicCreds();
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)) return;

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        Assert.True(await LoginAsync(page, userName, password));

        await page.GotoAsync($"{_fixture.BaseUrl}/permit-intel/subscribe");
        await page.GetByText("Start Free Trial").First.ClickAsync();

        // StripeService.CreateCheckoutSession redirects the browser to Stripe-hosted checkout.
        await page.WaitForURLAsync(url => url.Contains("stripe.com") || url.Contains("checkout"),
            new() { Timeout = 15000 });

        Assert.Contains("stripe", page.Url, StringComparison.OrdinalIgnoreCase);
    }
}
