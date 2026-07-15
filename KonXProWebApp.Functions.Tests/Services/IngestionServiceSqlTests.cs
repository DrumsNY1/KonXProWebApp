using KonXProWebApp.Data;
using KonXProWebApp.Functions.Models;
using KonXProWebApp.Functions.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.MsSql;
using Xunit;

namespace KonXProWebApp.Functions.Tests.Services;

/// <summary>
/// Exercises IngestionService's MERGE-based upsert logic against a real SQL Server instance,
/// since the MERGE statements and schema constraints cannot be validated with an in-memory provider.
/// Requires Docker to be available to run (skipped collection lets these run isolated from other SQL tests).
/// </summary>
[Collection("SQL")]
public class IngestionServiceSqlTests : IAsyncLifetime
{
    private MsSqlContainer _sql = null!;
    private IngestionService _service = null!;
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        _sql = new MsSqlBuilder().Build();
        await _sql.StartAsync();
        _connectionString = _sql.GetConnectionString();

        // Reuse the app's EF model (already scaffolded against the real schema) to create the
        // DOBJobFilings / HomeImprovementContractors / IngestionLogs tables that IngestionService's
        // raw ADO.NET MERGE statements target. There is no baseline EF migration for these tables,
        // so EnsureCreated (not Migrate) is what actually produces them here.
        var dbOptions = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var schemaContext = new db_9f8bee_konxdevContext(dbOptions);
        await schemaContext.Database.EnsureCreatedAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SqlConnectionString"] = _connectionString
            })
            .Build();

        _service = new IngestionService(config, new Mock<ILogger<IngestionService>>().Object);
    }

    public async Task DisposeAsync() => await _sql.DisposeAsync();

    [Fact]
    public async Task UpsertPermits_NewRecord_InsertsCorrectly()
    {
        var records = new List<SocrataPermitRecord>
        {
            new()
            {
                JobNumber = "500001", DocNumber = "01", Borough = "BROOKLYN",
                HouseNumber = "123", StreetName = "MAIN ST", JobType = "A1",
                InitialCost = "$75,000", Plumbing = "X"
            }
        };

        var (inserted, updated, skipped) = await _service.UpsertPermits(records);

        Assert.Equal(1, inserted);
        Assert.Equal(0, updated);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public async Task UpsertPermits_ExistingRecord_Updates()
    {
        var record = new SocrataPermitRecord
        { JobNumber = "500002", DocNumber = "01", Borough = "BROOKLYN", JobType = "A1" };
        await _service.UpsertPermits(new[] { record });

        record.Borough = "MANHATTAN";
        var (inserted, updated, skipped) = await _service.UpsertPermits(new[] { record });

        Assert.Equal(0, inserted);
        Assert.Equal(1, updated);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public async Task UpsertPermits_NullJobNumber_Skips()
    {
        var records = new List<SocrataPermitRecord>
        {
            new() { JobNumber = null, DocNumber = "01", Borough = "BROOKLYN" }
        };

        var (inserted, updated, skipped) = await _service.UpsertPermits(records);

        Assert.Equal(0, inserted);
        Assert.Equal(0, updated);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public async Task UpsertPermits_CurrencyParsing_StoredAsDecimal()
    {
        var records = new List<SocrataPermitRecord>
        {
            new()
            {
                JobNumber = "500003", DocNumber = "01", JobType = "NB",
                InitialCost = "$1,234,567.89"
            }
        };

        await _service.UpsertPermits(records);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("SELECT InitialCost FROM DOBJobFilings WHERE JobNum = 500003", conn);
        var result = (decimal)(await cmd.ExecuteScalarAsync())!;
        Assert.Equal(1234567.89m, result);
    }

    [Fact]
    public async Task UpsertContractors_MissingLicenseNumber_Skips()
    {
        var records = new List<SocrataContractorRecord>
        {
            new() { LicenseNumber = null, BusinessUniqueId = "abc", BusinessName = "Test Co" }
        };

        var (inserted, updated, skipped) = await _service.UpsertContractors(records);

        Assert.Equal(0, inserted);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public async Task UpsertContractors_NewRecord_InsertsCorrectly()
    {
        var records = new List<SocrataContractorRecord>
        {
            new()
            {
                LicenseNumber = "LIC-1", BusinessUniqueId = "BIZ-1", BusinessName = "Acme Contracting",
                LicenseStatus = "Active", AddressBorough = "QUEENS"
            }
        };

        var (inserted, updated, skipped) = await _service.UpsertContractors(records);

        Assert.Equal(1, inserted);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public async Task LogIngestionRun_And_GetLastIngestionTimestamp_RoundTrips()
    {
        var timestamp = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await _service.LogIngestionRun(10, 2, 1, "Success", null, timestamp);

        var last = await _service.GetLastIngestionTimestamp();

        Assert.Equal(timestamp, last);
    }

    [Fact]
    public async Task GetLastIngestionTimestamp_OnlyConsidersSuccessfulRuns()
    {
        await _service.LogIngestionRun(0, 0, 5, "Failed", "boom", new DateTime(2024, 1, 1));

        var last = await _service.GetLastIngestionTimestamp();

        Assert.Null(last);
    }
}
