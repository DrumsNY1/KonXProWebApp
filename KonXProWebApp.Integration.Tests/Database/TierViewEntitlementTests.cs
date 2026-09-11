using KonXProWebApp.Data;
using KonXProWebApp.Integration.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KonXProWebApp.Integration.Tests.Database;

/// <summary>
/// Asserts the exact column set of each tier-dashboard view. These views are the entitlement
/// boundary between subscription tiers (see REMEDIATION.md Finding 4): Free/Basic is separated by
/// HouseNum, Mid/High by EstimatedCost. A regression here silently changes what a subscriber can
/// see, so this must check both boundaries, not just the cost column.
///
/// SqlWebApplicationFactory.EnsureDatabaseCreatedAsync() only creates the 13 base tables (the views
/// are excluded from migrations by design, see TierViewDefinitions) so this test creates the views
/// itself, using the exact same SQL Program.cs runs at startup.
/// </summary>
public class TierViewEntitlementTests : IClassFixture<SqlWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqlWebApplicationFactory _factory;

    public TierViewEntitlementTests(SqlWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        await _factory.EnsureDatabaseCreatedAsync();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<db_9f8bee_konxdevContext>();
        foreach (var (_, sql) in TierViewDefinitions.Views)
        {
            await context.Database.ExecuteSqlRawAsync(sql);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public static TheoryData<string, string[]> TierViewColumns => new()
    {
        { "vwFreeTierDashboard", ["JobNum", "Borough", "Street", "LatestActionDate", "ProjectType", "JobDescription", "Neighborhood"] },
        { "vwBasicTierDashboard", ["JobNum", "Borough", "HouseNum", "Street", "LatestActionDate", "ProjectType", "JobDescription", "Neighborhood"] },
        { "vwMidTierDashboard", ["JobNum", "Borough", "HouseNum", "Street", "LatestActionDate", "ProjectType", "EstimatedCost", "JobDescription", "Neighborhood"] },
        { "vwHighTierDashboard", ["JobNum", "Borough", "HouseNum", "Street", "LatestActionDate", "ProjectType", "EstimatedCost", "JobDescription", "Neighborhood"] },
    };

    [Theory]
    [MemberData(nameof(TierViewColumns))]
    public async Task View_ExposesExactlyItsTierColumns(string viewName, string[] expectedColumns)
    {
        var actualColumns = await GetColumnNamesAsync(viewName);

        Assert.Equal(expectedColumns.OrderBy(c => c), actualColumns.OrderBy(c => c));
    }

    [Fact]
    public async Task FreeTier_NeverExposesHouseNumOrEstimatedCost()
    {
        var columns = await GetColumnNamesAsync("vwFreeTierDashboard");

        Assert.DoesNotContain("HouseNum", columns);
        Assert.DoesNotContain("EstimatedCost", columns);
    }

    [Fact]
    public async Task BasicTier_ExposesHouseNumButNeverEstimatedCost()
    {
        var columns = await GetColumnNamesAsync("vwBasicTierDashboard");

        Assert.Contains("HouseNum", columns);
        Assert.DoesNotContain("EstimatedCost", columns);
    }

    [Theory]
    [InlineData("vwMidTierDashboard")]
    [InlineData("vwHighTierDashboard")]
    public async Task MidAndHighTiers_ExposeBothHouseNumAndEstimatedCost(string viewName)
    {
        var columns = await GetColumnNamesAsync(viewName);

        Assert.Contains("HouseNum", columns);
        Assert.Contains("EstimatedCost", columns);
    }

    private async Task<List<string>> GetColumnNamesAsync(string viewName)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<db_9f8bee_konxdevContext>();
        var connection = (SqlConnection)context.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @viewName";
            command.Parameters.AddWithValue("@viewName", viewName);

            var columns = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(0));
            }
            return columns;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
