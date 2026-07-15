using Microsoft.Playwright;
using Xunit;

namespace KonXProWebApp.E2E.Tests.Journeys;

/// <summary>
/// End-to-end tests validating the authorization behavior of different subscription tiers:
/// - Free: Can only access FreeTier dashboard.
/// - Starter: Can access FreeTier and BasicTier dashboards.
/// - Pro: Can access FreeTier, BasicTier, and MidTier dashboards.
/// - Business: Can access FreeTier, BasicTier, MidTier, and HighTier dashboards.
/// </summary>
[Collection("E2E")]
public class TierJourneyTests
{
    private readonly PlaywrightFixture _fixture;

    public TierJourneyTests(PlaywrightFixture fixture) => _fixture = fixture;

    private static (string? User, string? Password) FreeCreds() => (
        Environment.GetEnvironmentVariable("E2E_FREE_USER"),
        Environment.GetEnvironmentVariable("E2E_FREE_PASSWORD"));

    private static (string? User, string? Password) StarterCreds() => (
        Environment.GetEnvironmentVariable("E2E_STARTER_USER"),
        Environment.GetEnvironmentVariable("E2E_STARTER_PASSWORD"));

    private static (string? User, string? Password) ProCreds() => (
        Environment.GetEnvironmentVariable("E2E_PRO_USER"),
        Environment.GetEnvironmentVariable("E2E_PRO_PASSWORD"));

    private static (string? User, string? Password) BusinessCreds() => (
        Environment.GetEnvironmentVariable("E2E_BUSINESS_USER"),
        Environment.GetEnvironmentVariable("E2E_BUSINESS_PASSWORD"));

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
    public async Task FreeUser_TierAccessRules()
    {
        var (userName, password) = FreeCreds();
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)) return;

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        Assert.True(await LoginAsync(page, userName, password));

        // 1. Visit FreeTier -> Success
        await page.GotoAsync($"{_fixture.BaseUrl}/FreeTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/FreeTier", page.Url);

        // 2. Visit BasicTier -> Redirect to Unauthorized
        await page.GotoAsync($"{_fixture.BaseUrl}/BasicTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/Unauthorized", page.Url);

        // 3. Visit MidTier -> Redirect to Unauthorized
        await page.GotoAsync($"{_fixture.BaseUrl}/MidTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/Unauthorized", page.Url);

        // 4. Visit HighTier -> Redirect to Unauthorized
        await page.GotoAsync($"{_fixture.BaseUrl}/highTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/Unauthorized", page.Url);
    }

    [Fact]
    public async Task StarterUser_TierAccessRules()
    {
        var (userName, password) = StarterCreds();
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)) return;

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        Assert.True(await LoginAsync(page, userName, password));

        // 1. Visit FreeTier -> Success
        await page.GotoAsync($"{_fixture.BaseUrl}/FreeTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/FreeTier", page.Url);

        // 2. Visit BasicTier -> Success
        await page.GotoAsync($"{_fixture.BaseUrl}/BasicTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/BasicTier", page.Url);

        // 3. Visit MidTier -> Redirect to Unauthorized
        await page.GotoAsync($"{_fixture.BaseUrl}/MidTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/Unauthorized", page.Url);

        // 4. Visit HighTier -> Redirect to Unauthorized
        await page.GotoAsync($"{_fixture.BaseUrl}/highTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/Unauthorized", page.Url);
    }

    [Fact]
    public async Task ProUser_TierAccessRules()
    {
        var (userName, password) = ProCreds();
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)) return;

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        Assert.True(await LoginAsync(page, userName, password));

        // 1. Visit FreeTier -> Success
        await page.GotoAsync($"{_fixture.BaseUrl}/FreeTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/FreeTier", page.Url);

        // 2. Visit BasicTier -> Success
        await page.GotoAsync($"{_fixture.BaseUrl}/BasicTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/BasicTier", page.Url);

        // 3. Visit MidTier -> Success
        await page.GotoAsync($"{_fixture.BaseUrl}/MidTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/MidTier", page.Url);

        // 4. Visit HighTier -> Redirect to Unauthorized
        await page.GotoAsync($"{_fixture.BaseUrl}/highTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/Unauthorized", page.Url);
    }

    [Fact]
    public async Task BusinessUser_TierAccessRules()
    {
        var (userName, password) = BusinessCreds();
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)) return;

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        Assert.True(await LoginAsync(page, userName, password));

        // 1. Visit FreeTier -> Success
        await page.GotoAsync($"{_fixture.BaseUrl}/FreeTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/FreeTier", page.Url);

        // 2. Visit BasicTier -> Success
        await page.GotoAsync($"{_fixture.BaseUrl}/BasicTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/BasicTier", page.Url);

        // 3. Visit MidTier -> Success
        await page.GotoAsync($"{_fixture.BaseUrl}/MidTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/MidTier", page.Url);

        // 4. Visit HighTier -> Success
        await page.GotoAsync($"{_fixture.BaseUrl}/highTier");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/highTier", page.Url);
    }
}
