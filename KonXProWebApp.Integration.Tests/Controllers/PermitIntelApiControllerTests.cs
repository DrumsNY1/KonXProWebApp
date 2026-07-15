using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KonXProWebApp.Data;
using KonXProWebApp.Integration.Tests.Infrastructure;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KonXProWebApp.Integration.Tests.Controllers;

/// <summary>
/// Exercises /api/permit-intel end-to-end through the real ASP.NET Core pipeline, including the
/// [Authorize(Policy = "RequiresAgency")] gate on the controller (backed by
/// SubscriptionAuthorizationHandler + a real subscription row in the database).
/// </summary>
public class PermitIntelApiControllerTests : IClassFixture<SqlWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqlWebApplicationFactory _factory;

    public PermitIntelApiControllerTests(SqlWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        await _factory.EnsureDatabaseCreatedAsync();
        await SeedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<db_9f8bee_konxdevContext>();

        if (!context.Subscriptions.Any(s => s.UserId == "starter_user"))
        {
            context.Subscriptions.Add(new Subscription
            {
                UserId = "starter_user",
                Tier = "Starter",
                Status = "Active",
                StartDate = DateTime.UtcNow.AddDays(-5)
            });
        }

        if (!context.Subscriptions.Any(s => s.UserId == "agency_user"))
        {
            context.Subscriptions.Add(new Subscription
            {
                UserId = "agency_user",
                Tier = "Agency",
                Status = "Active",
                StartDate = DateTime.UtcNow.AddDays(-5)
            });
        }

        if (!context.DobjobFilings.Any(f => f.JobNum == 900001))
        {
            context.DobjobFilings.Add(new DobjobFiling
            {
                JobNum = 900001,
                Borough = "BROOKLYN",
                JobType = "A1",
                HouseNum = "1",
                StreetName = "TEST ST",
                LatestActionDate = DateTime.UtcNow.AddDays(-1)
            });
        }

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPermits_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/permit-intel/permits");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPermits_StarterUser_Returns403()
    {
        var client = _factory.CreateAuthenticatedClient("starter_user");

        var response = await client.GetAsync("/api/permit-intel/permits");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPermits_AgencyUser_Returns200WithPaginatedResults()
    {
        var client = _factory.CreateAuthenticatedClient("agency_user");

        var response = await client.GetAsync("/api/permit-intel/permits?borough=BROOKLYN&take=10&skip=0");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(body.GetProperty("totalCount").GetInt32() >= 0);
        Assert.Equal(0, body.GetProperty("skip").GetInt32());
        Assert.Equal(10, body.GetProperty("take").GetInt32());
    }

    [Fact]
    public async Task GetPermits_TakeClamped_ExceededMax_Returns100()
    {
        var client = _factory.CreateAuthenticatedClient("agency_user");

        var response = await client.GetAsync("/api/permit-intel/permits?take=9999");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100, body.GetProperty("take").GetInt32());
    }

    [Fact]
    public async Task GetPermits_TakeBelowMin_ClampedToOne()
    {
        var client = _factory.CreateAuthenticatedClient("agency_user");

        var response = await client.GetAsync("/api/permit-intel/permits?take=0");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("take").GetInt32());
    }

    [Fact]
    public async Task GetPermit_UnknownId_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient("agency_user");

        var response = await client.GetAsync("/api/permit-intel/permits/99999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBoroughStats_DefaultDays_Returns200WithList()
    {
        var client = _factory.CreateAuthenticatedClient("agency_user");

        var response = await client.GetAsync("/api/permit-intel/stats/boroughs");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
    }

    [Fact]
    public async Task GetBoroughStats_DaysClamped_ExceededMax_StillReturns200()
    {
        var client = _factory.CreateAuthenticatedClient("agency_user");

        // days=9999 is clamped to 365 internally by the controller; just verify it doesn't error out.
        var response = await client.GetAsync("/api/permit-intel/stats/boroughs?days=9999");

        response.EnsureSuccessStatusCode();
    }
}
