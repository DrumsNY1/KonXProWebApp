using Microsoft.Playwright;
using Xunit;

namespace KonXProWebApp.E2E.Tests;

/// <summary>
/// Shares one Playwright + Chromium instance across the whole E2E run. Tests drive a real, already
/// running instance of KonXProWebApp — start it separately (e.g. `dotnet run --project KonXProWebApp.csproj`)
/// before running this project, or point E2E_BASE_URL at a deployed environment.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    public string BaseUrl { get; } =
        Environment.GetEnvironmentVariable("E2E_BASE_URL")?.TrimEnd('/') ?? "https://localhost:5001";

    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }

    /// <summary>
    /// Set E2E_VIDEO_DIR to record every context to disk — used by CI to capture what the browser
    /// actually saw when a journey fails. Videos are flushed when the context closes, so this needs
    /// no per-test start/stop. Unset (the default, including local runs) records nothing.
    /// </summary>
    private static readonly string? VideoDir = Environment.GetEnvironmentVariable("E2E_VIDEO_DIR");

    public Task<IBrowserContext> NewContextAsync() =>
        Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            RecordVideoDir = string.IsNullOrWhiteSpace(VideoDir) ? null : VideoDir,
        });
}

[CollectionDefinition("E2E")]
public class E2ECollection : ICollectionFixture<PlaywrightFixture>
{
}
