using KonXProWebApp.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace KonXProWebApp.Integration.Tests.Infrastructure;

/// <summary>
/// Boots the real KonXProWebApp host in-process against a throwaway SQL Server container (via
/// Testcontainers), with a header-driven fake auth scheme wired in for simulating authenticated
/// requests. One instance is meant to be shared per test class via IClassFixture; xUnit v2 does not
/// await IAsyncLifetime on class fixtures, so callers must explicitly await
/// <see cref="InitializeAsync"/> from their own IAsyncLifetime.InitializeAsync before using the
/// factory (see the test classes for the pattern).
/// </summary>
public class SqlWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder().Build();
    private Task? _initTask;

    /// <summary>Extra configuration merged in before the host is built. Mutate before the first client/Services access.</summary>
    public Dictionary<string, string?> ExtraConfig { get; } = new()
    {
        ["Stripe:WebhookSecret"] = "",
        ["Stripe:SecretKey"] = "sk_test_fake",
    };

    public Task InitializeAsync() => _initTask ??= _sql.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>(ExtraConfig)
            {
                ["ConnectionStrings:db_9f8bee_konxdevConnection"] = _sql.GetConnectionString()
            };
            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    /// <summary>Creates the schema for a fresh container. Safe to call once the factory is initialized.</summary>
    public async Task EnsureDatabaseCreatedAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<db_9f8bee_konxdevContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public HttpClient CreateAuthenticatedClient(string userId, string? email = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        if (email != null)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, email);
        }
        return client;
    }

    // xUnit v2's IClassFixture only calls the synchronous IDisposable.Dispose() path (it does not
    // await IAsyncLifetime/IAsyncDisposable on class fixtures), so the container is torn down here
    // rather than in an overridden DisposeAsync that would never run.
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _sql.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
