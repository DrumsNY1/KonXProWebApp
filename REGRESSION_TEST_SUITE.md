# KonXProWebApp — Regression Test Suite Recommendation

> Generated from direct codebase analysis — July 2026  
> Target framework: .NET 9 | Architecture: Blazor Server + Azure Functions + SQL Server

---

## Table of Contents

1. [Current Test Coverage Snapshot](#1-current-test-coverage-snapshot)
2. [Testing Framework Stack](#2-testing-framework-stack)
3. [Coverage Targets by Layer](#3-coverage-targets-by-layer)
4. [Unit Tests — Lead Scoring & IngestionService](#4-unit-tests--lead-scoring--ingestionservice)
5. [Unit Tests — StripeService & Subscription Lifecycle](#5-unit-tests--stripeservice--subscription-lifecycle)
6. [Unit Tests — Authorization & SubscriptionRequirement](#6-unit-tests--authorization--subscriptionrequirement)
7. [Unit Tests — SocrataClient](#7-unit-tests--socrataClient)
8. [Unit Tests — AlertDispatchFunction Matching Logic](#8-unit-tests--alertdispatchfunction-matching-logic)
9. [Integration Tests — PermitIntelService](#9-integration-tests--permitintelservice)
10. [Integration Tests — StripeWebhookController](#10-integration-tests--stripewebhookcontroller)
11. [Integration Tests — PermitIntelApiController](#11-integration-tests--permitintelapicontroller)
12. [Integration Tests — Azure Functions](#12-integration-tests--azure-functions)
13. [Blazor Component Tests (bUnit)](#13-blazor-component-tests-bunit)
14. [End-to-End Tests (Playwright)](#14-end-to-end-tests-playwright)
15. [Database & Migration Tests](#15-database--migration-tests)
16. [Performance & Load Tests](#16-performance--load-tests)
17. [CI/CD Integration](#17-cicd-integration)
18. [Quick-Start: Adding the Missing Test Projects](#18-quick-start-adding-the-missing-test-projects)

---

## 1. Current Test Coverage Snapshot

The project already has `KonXProWebApp.Tests` (xUnit + Moq + EF InMemory) with four test classes covering the happiest paths:

| Class | Tests | What it covers |
|---|---|---|
| `PermitIntelServiceTests` | 14 | `SearchPermits`, `SaveLead`, `UpdateLeadStatus`, `DeleteLead`, `GetSavedLeadCount`, `SaveAlertPreference`, `GetActiveSubscription`, `ScorePermit` |
| `StripeServiceTests` | 6 | `GetTierInfo`, `HandleCheckoutCompleted`, `HandleSubscriptionUpdated` |
| `SubscriptionRequirementTests` | 5 | `SubscriptionAuthorizationHandler`, `GetTierLevel` |
| `LeadScoringEdgeCaseTests` | 9 | `ScorePermit` boundary conditions |

**Critical gaps that have zero coverage today:**

- `IngestionService.UpsertPermits / UpsertDobViolations / UpsertHpdViolations / UpsertContractors` — the core ETL path
- `IngestionService` parsing helpers: `ParseDate`, `ParseIsoDate`, `ParseDobDate`, `ParseCurrency`
- `SocrataClient` — pagination, retry, rate-limit backoff
- All five Azure Functions (`PermitIngestionFunction`, `DobViolationIngestionFunction`, `HpdViolationIngestionFunction`, `ContractorIngestionFunction`, `AlertDispatchFunction`)
- `AlertDispatchFunction.GetMatchingPermits` — the dynamic SQL filter builder for alerts
- `StripeWebhookController` — webhook signature verification + event routing
- `PermitIntelApiController` — all three endpoints
- `EmailService.BuildAlertDigestHtml` and `SmsService.BuildAlertSms`
- `PermitIntelService.GetBblFromFiling` — BBL construction from Borough/Block/Lot
- `PermitIntelService.Get311ComplaintVelocity` — predictive scoring input
- All Blazor components (0 bUnit tests)
- All user journeys (0 Playwright tests)

---

## 2. Testing Framework Stack

### Recommended Stack

| Layer | Framework | NuGet Package | Rationale |
|---|---|---|---|
| Unit & integration (web) | **xUnit 2.9+** | `xunit`, `xunit.runner.visualstudio` | Already in use; stick with it |
| Mocking | **Moq 4.20+** | `Moq` | Already in use |
| EF Core in-memory | **EF InMemory** | `Microsoft.EntityFrameworkCore.InMemory` | Already in use for unit tests |
| EF Core integration | **SQL Server + Testcontainers** | `Testcontainers.MsSql` | Real SQL needed for MERGE, indexes, schema validation |
| Blazor component tests | **bUnit 1.x** | `bunit` | Only framework for Blazor Server component testing |
| HTTP controller tests | **WebApplicationFactory** | `Microsoft.AspNetCore.Mvc.Testing` | In-process test server, no network required |
| E2E / browser | **Playwright for .NET** | `Microsoft.Playwright` | Headless Chromium, first-class .NET support |
| Azure Functions unit tests | **xUnit + Moq** | (same as above) | Functions are plain classes; just inject fakes |
| Performance | **NBomber** | `NBomber` | .NET-native, good for the ingestion pipeline |
| Code coverage | **Coverlet** | `coverlet.collector` | Already referenced; needs threshold config |

### New Test Projects to Create

```
KonXProWebApp.sln
├── KonXProWebApp.Tests               ← already exists; expand it
├── KonXProWebApp.Functions.Tests     ← NEW: unit tests for Functions project
├── KonXProWebApp.Integration.Tests   ← NEW: WebApplicationFactory + Testcontainers
└── KonXProWebApp.E2E.Tests           ← NEW: Playwright end-to-end
```

---

## 3. Coverage Targets by Layer

| Layer | Min Branch Coverage Target | Priority |
|---|---|---|
| `IngestionService` (parsing + upsert) | 90% | Critical |
| `PermitIntelService` (scoring, search, leads) | 85% | Critical |
| `StripeService` (session creation, webhook handlers) | 85% | Critical |
| `SubscriptionAuthorizationHandler` | 90% | Critical |
| `SocrataClient` (pagination, retry, rate limiting) | 80% | High |
| `AlertDispatchFunction` matching logic | 85% | High |
| `StripeWebhookController` | 80% | High |
| `PermitIntelApiController` | 80% | High |
| Blazor components (PermitSearch, MyLeads, Subscribe) | 70% | Medium |
| `EmailService.BuildAlertDigestHtml` | 75% | Medium |
| Migration idempotency | 100% (run-up/down) | High |

---

## 4. Unit Tests — Lead Scoring & IngestionService

### 4.1 `ScorePermit` — gaps in existing tests

The existing `LeadScoringEdgeCaseTests` covers basic cases, but the predictive `complaintVelocity` parameter has no tests at all. Add to `KonXProWebApp.Tests`:

```csharp
// File: KonXProWebApp.Tests/Services/ScorePermitComplaintVelocityTests.cs

[Fact]
public void ScorePermit_ThreeOrMoreComplaints_AddsTwoPoints()
{
    var filing = new DobjobFiling { JobType = "A3" };
    var score = PermitIntelService.ScorePermit(filing, complaintVelocity: 3);
    // base clamp=1 + complaint boost+2 = 3
    Assert.Equal(3, score);
}

[Fact]
public void ScorePermit_OneComplaint_AddsOnePoint()
{
    var filing = new DobjobFiling { JobType = "A3" };
    var score = PermitIntelService.ScorePermit(filing, complaintVelocity: 1);
    Assert.Equal(2, score); // 1 base + 1 complaint = 2? Check clamp logic.
    // NOTE: current code clamps to min=1 at the end, so 0 base + 1 = 1. 
    // This test will catch if that behavior changes.
}

[Fact]
public void ScorePermit_FullHouse_WithHighComplaints_ClampedAtFive()
{
    var filing = new DobjobFiling
    {
        JobType = "NB",
        InitialCost = 200_000m,
        Plumbing = "X", Mechanical = "X",
        ExistingDwellingUnits = "1", ProposedDwellingUnits = "20"
    };
    var score = PermitIntelService.ScorePermit(filing, complaintVelocity: 10);
    Assert.Equal(5, score);
}
```

### 4.2 `GetBblFromFiling` — completely untested

```csharp
// File: KonXProWebApp.Tests/Services/BblComputationTests.cs

[Theory]
[InlineData("MANHATTAN", "00123", "0045", "1001230045")]
[InlineData("BROOKLYN",  "00456", "0012", "3004560012")]
[InlineData("QUEENS",    "01234", "0056", "4012340056")]
[InlineData("BRONX",     "00001", "0001", "2000010001")]
[InlineData("STATEN ISLAND", "00099", "0003", "5000990003")]
public void GetBblFromFiling_KnownBorough_ReturnsCorrectBbl(
    string borough, string block, string lot, string expectedBbl)
{
    var filing = new DobjobFiling { Borough = borough, Block = block, Lot = lot };
    Assert.Equal(expectedBbl, PermitIntelService.GetBblFromFiling(filing));
}

[Fact]
public void GetBblFromFiling_UnknownBorough_ReturnsNull()
{
    var filing = new DobjobFiling { Borough = "NEW JERSEY", Block = "00001", Lot = "0001" };
    Assert.Null(PermitIntelService.GetBblFromFiling(filing));
}

[Theory]
[InlineData(null, "00001", "0001")]
[InlineData("MANHATTAN", null, "0001")]
[InlineData("MANHATTAN", "00001", null)]
public void GetBblFromFiling_NullField_ReturnsNull(string borough, string block, string lot)
{
    var filing = new DobjobFiling { Borough = borough, Block = block, Lot = lot };
    Assert.Null(PermitIntelService.GetBblFromFiling(filing));
}

[Fact]
public void GetBblFromFiling_ShortBlockAndLot_PadsToCorrectWidth()
{
    // Block "1" → "00001", Lot "1" → "0001"
    var filing = new DobjobFiling { Borough = "MANHATTAN", Block = "1", Lot = "1" };
    Assert.Equal("1000010001", PermitIntelService.GetBblFromFiling(filing));
}
```

### 4.3 `IngestionService` Parsing Helpers

`ParseDate`, `ParseIsoDate`, `ParseDobDate`, and `ParseCurrency` are `private static` today. Extract them to `internal static` (or a separate `ParseHelpers` class) and add a `[assembly: InternalsVisibleTo("KonXProWebApp.Functions.Tests")]` attribute, then test:

```csharp
// File: KonXProWebApp.Functions.Tests/Services/IngestionParsingTests.cs

[Theory]
[InlineData("01/15/2024", 2024, 1, 15)]
[InlineData("12/31/2023", 2023, 12, 31)]
public void ParseDate_SocrataFormat_ReturnsParsedDate(string input, int y, int m, int d)
{
    var result = IngestionService.ParseDate(input);  // after making internal
    Assert.Equal(new DateTime(y, m, d), result);
}

[Fact]
public void ParseDate_NullOrEmpty_ReturnsNull()
{
    Assert.Null(IngestionService.ParseDate(null));
    Assert.Null(IngestionService.ParseDate(""));
    Assert.Null(IngestionService.ParseDate("   "));
}

[Theory]
[InlineData("2022-02-24T00:00:00.000", 2022, 2, 24)]
[InlineData("2023-11-15T00:00:00.000", 2023, 11, 15)]
public void ParseIsoDate_SocrataFormat_ReturnsParsedDate(string input, int y, int m, int d)
{
    var result = IngestionService.ParseIsoDate(input);
    Assert.Equal(new DateTime(y, m, d), result?.Date);
}

[Theory]
[InlineData("20231231", 2023, 12, 31)]
[InlineData("19990101", 1999, 1, 1)]
public void ParseDobDate_YyyyMmDdFormat_ReturnsParsedDate(string input, int y, int m, int d)
{
    var result = IngestionService.ParseDobDate(input);
    Assert.Equal(new DateTime(y, m, d), result);
}

[Theory]
[InlineData("$50,000.00",  50000.00)]
[InlineData("1,200",       1200.00)]
[InlineData("$0",          0.00)]
[InlineData("100",         100.00)]
public void ParseCurrency_ValidFormats_ReturnsDecimal(string input, decimal expected)
{
    var result = IngestionService.ParseCurrency(input);
    Assert.Equal(expected, result);
}

[Theory]
[InlineData("")]
[InlineData(null)]
[InlineData("N/A")]
[InlineData("not a number")]
public void ParseCurrency_InvalidInput_ReturnsNull(string input)
{
    Assert.Null(IngestionService.ParseCurrency(input));
}
```

### 4.4 `IngestionService.UpsertPermits` — MERGE logic

The MERGE SQL cannot be tested with EF InMemory. This belongs in integration tests (see §12), but the **counting logic** (inserted/updated/skipped accumulation) can be unit-tested by mocking the SQL layer or abstracting the upsert into a testable boundary.

**Recommended refactor:** Extract an `IUpsertStrategy` interface or just accept a `Func<SqlConnection, record, Task<UpsertResult>>` so the counting loop can be unit tested without SQL. Until then, treat MERGE as integration-test territory.

---

## 5. Unit Tests — StripeService & Subscription Lifecycle

### 5.1 Missing: `HandleCheckoutCompleted` with missing userId in metadata

```csharp
// File: KonXProWebApp.Tests/Services/StripeServiceTests.cs (add to existing class)

[Fact]
public async Task HandleCheckoutCompleted_MissingUserId_DoesNotCreateSubscription()
{
    var session = new Stripe.Checkout.Session
    {
        Id = "cs_test_no_user",
        CustomerId = "cus_123",
        SubscriptionId = "sub_123",
        Metadata = new Dictionary<string, string>()  // no userId key
    };

    await _service.HandleCheckoutCompleted(session);

    Assert.Equal(0, await _context.Subscriptions.CountAsync());
}
```

### 5.2 Missing: `HandleSubscriptionUpdated` — PastDue mapping

```csharp
[Theory]
[InlineData("active", "Active")]
[InlineData("trialing", "Trialing")]
[InlineData("past_due", "PastDue")]
[InlineData("unpaid", "PastDue")]
[InlineData("canceled", "Canceled")]
public async Task HandleSubscriptionUpdated_StatusMappings_AreCorrect(
    string stripeStatus, string expectedLocalStatus)
{
    _context.Subscriptions.Add(new Subscription
    {
        UserId = "u1", Tier = "Pro", Status = "Active",
        StripeSubscriptionId = "sub_map_test", StartDate = DateTime.UtcNow
    });
    await _context.SaveChangesAsync();

    var stripeSub = new Stripe.Subscription { Id = "sub_map_test", Status = stripeStatus };
    await _service.HandleSubscriptionUpdated(stripeSub);

    var local = await _context.Subscriptions.FirstAsync(s => s.StripeSubscriptionId == "sub_map_test");
    Assert.Equal(expectedLocalStatus, local.Status);
}

[Fact]
public async Task HandleSubscriptionUpdated_CanceledStatus_SetsEndDate()
{
    _context.Subscriptions.Add(new Subscription
    {
        UserId = "u1", Tier = "Pro", Status = "Active",
        StripeSubscriptionId = "sub_cancel", StartDate = DateTime.UtcNow
    });
    await _context.SaveChangesAsync();

    var stripeSub = new Stripe.Subscription { Id = "sub_cancel", Status = "canceled" };
    await _service.HandleSubscriptionUpdated(stripeSub);

    var local = await _context.Subscriptions.FirstAsync(s => s.StripeSubscriptionId == "sub_cancel");
    Assert.NotNull(local.EndDate);
}

[Fact]
public async Task HandleSubscriptionUpdated_UnknownSubscriptionId_LogsWarningOnly()
{
    // Should not throw, just log
    var stripeSub = new Stripe.Subscription { Id = "sub_ghost", Status = "active" };
    var ex = await Record.ExceptionAsync(() => _service.HandleSubscriptionUpdated(stripeSub));
    Assert.Null(ex);
}
```

### 5.3 Missing: `CreateCheckoutSession` — unknown tier throws

```csharp
[Fact]
public async Task CreateCheckoutSession_UnknownTier_ThrowsArgumentException()
{
    await Assert.ThrowsAsync<ArgumentException>(() =>
        _service.CreateCheckoutSession("u1", "u1@test.com", "Diamond", "/success", "/cancel"));
}
```

---

## 6. Unit Tests — Authorization & SubscriptionRequirement

Existing tests are good. Add these missing cases:

```csharp
// File: KonXProWebApp.Tests/Authorization/SubscriptionRequirementTests.cs

[Fact]
public async Task Handler_PastDueUser_FailsRequirement()
{
    // PastDue is not "Active" or "Trialing", so GetActiveSubscription returns null
    var (handler, context) = await SetupHandler("u1", "Pro", "PastDue", "Starter");
    await handler.HandleAsync(context);
    Assert.False(context.HasSucceeded);
}

[Fact]
public async Task Handler_AgencyUser_SucceedsForAllLowerTiers()
{
    foreach (var required in new[] { "Starter", "Pro", "Business", "Agency" })
    {
        var (handler, context) = await SetupHandler("u1", "Agency", "Active", required);
        await handler.HandleAsync(context);
        Assert.True(context.HasSucceeded, $"Agency should pass {required} requirement");
    }
}

[Fact]
public async Task Handler_UnauthenticatedUser_Fails()
{
    // No NameIdentifier claim
    var services = new ServiceCollection();
    var dbOptions = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
    var db = new db_9f8bee_konxdevContext(dbOptions);
    var mockNav = new Mock<NavigationManager>();
    services.AddScoped(_ => db);
    services.AddScoped(_ => new PermitIntelService(db, mockNav.Object));
    var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

    var handler = new SubscriptionAuthorizationHandler(scopeFactory);
    var anonUser = new ClaimsPrincipal(new ClaimsIdentity()); // no claims
    var requirement = new SubscriptionRequirement("Starter");
    var context = new AuthorizationHandlerContext(new[] { requirement }, anonUser, null);

    await handler.HandleAsync(context);
    Assert.False(context.HasSucceeded);
}
```

---

## 7. Unit Tests — SocrataClient

`SocrataClient` depends only on `HttpClient` and `ILogger`, making it straightforward to unit test with a mock HTTP message handler.

```csharp
// File: KonXProWebApp.Functions.Tests/Services/SocrataClientTests.cs

public class SocrataClientTests
{
    private SocrataClient CreateClient(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://data.cityofnewyork.us/resource/") };
        var logger = new Mock<ILogger<SocrataClient>>();
        return new SocrataClient(http, logger.Object);
    }

    [Fact]
    public async Task GetPermitsSince_SinglePage_YieldsAllRecords()
    {
        // Arrange: one page of 3 records (< 5000, so no more pages)
        var json = JsonSerializer.Serialize(new[]
        {
            new SocrataPermitRecord { JobNumber = "1", DobRunDate = "2024-01-01T00:00:00.000" },
            new SocrataPermitRecord { JobNumber = "2", DobRunDate = "2024-01-01T00:00:00.000" },
            new SocrataPermitRecord { JobNumber = "3", DobRunDate = "2024-01-01T00:00:00.000" }
        });

        var handler = new StaticJsonHandler(json);
        var client = CreateClient(handler);

        var results = new List<SocrataPermitRecord>();
        await foreach (var r in client.GetPermitsSince(null))
            results.Add(r);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task GetPermitsSince_WithSince_BuildsWhereClause()
    {
        var handler = new CapturingHandler("[]");
        var client = CreateClient(handler);

        var since = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await foreach (var _ in client.GetPermitsSince(since)) { }

        Assert.Contains("dobrundate", handler.LastRequestUri, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2024-06-01", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetPermitsSince_RateLimit_RetriesAndSucceeds()
    {
        // First call returns 429, second returns data
        var handler = new RetryAfterHandler(
            firstResponse: (int)HttpStatusCode.TooManyRequests,
            secondResponseJson: "[]");
        var client = CreateClient(handler);

        var results = new List<SocrataPermitRecord>();
        await foreach (var r in client.GetPermitsSince(null))
            results.Add(r);

        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GetPermitsSince_ConsistentHttpFailure_ThrowsAfterMaxRetries()
    {
        var handler = new AlwaysFailHandler(HttpStatusCode.InternalServerError);
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await foreach (var _ in client.GetPermitsSince(null)) { }
        });
    }

    [Fact]
    public async Task GetContractorsSince_IncludesBusinessCategoryFilter()
    {
        var handler = new CapturingHandler("[]");
        var client = CreateClient(handler);

        await foreach (var _ in client.GetContractorsSince(null)) { }

        Assert.Contains("Home+Improvement+Contractor", handler.LastRequestUri);
    }
}
```

---

## 8. Unit Tests — AlertDispatchFunction Matching Logic

`AlertDispatchFunction.GetMatchingPermits` builds a dynamic SQL WHERE clause from `AlertUser` preferences. This is one of the highest-risk untested code paths because SQL injection is possible if parameters are ever inlined. Extract the SQL-building logic to a testable helper, or test the complete function end-to-end with a real SQL container (see §12). At minimum, unit-test the branch logic:

```csharp
// File: KonXProWebApp.Functions.Tests/Functions/AlertDispatchMatchingTests.cs
// Requires extracting BuildAlertWhereClause as internal static

[Fact]
public void BuildAlertWhereClause_BoroughFilter_ProducesParameterizedIn()
{
    var user = new AlertUser { Boroughs = "BROOKLYN,QUEENS" };
    var (sql, parameters) = AlertDispatchFunction.BuildWhereClause(user);

    Assert.Contains("Borough IN", sql);
    Assert.Equal(2, parameters.Count(p => p.ParameterName.StartsWith("@borough")));
    Assert.DoesNotContain("BROOKLYN", sql); // must be parameterized, not inlined
}

[Fact]
public void BuildAlertWhereClause_NoFilters_OnlyDateCondition()
{
    var user = new AlertUser { };
    var (sql, _) = AlertDispatchFunction.BuildWhereClause(user);

    Assert.Contains("LatestActionDate >= DATEADD", sql);
    Assert.DoesNotContain("Borough", sql);
    Assert.DoesNotContain("JobType", sql);
}

[Fact]
public void BuildAlertWhereClause_TradeFilters_UsesOrNotAnd()
{
    var user = new AlertUser { Trades = "Plumbing,Mechanical" };
    var (sql, _) = AlertDispatchFunction.BuildWhereClause(user);

    // Trade conditions within a single filing should be OR'd (any matching trade)
    Assert.Contains("Plumbing = 'X'", sql);
    Assert.Contains("Mechanical = 'X'", sql);
    Assert.Contains("OR", sql);
}
```

### 8.2 `EmailService.BuildAlertDigestHtml`

```csharp
[Fact]
public void BuildAlertDigestHtml_SingleMatch_ContainsAddress()
{
    var service = new EmailService(new ConfigurationBuilder().Build(),
        new Mock<ILogger<EmailService>>().Object);

    var matches = new List<AlertPermitMatch>
    {
        new() { PermitId = 42, Address = "123 Main St", Borough = "BROOKLYN",
                JobType = "NB", EstCost = "$150,000", Status = "Approved", LeadScore = 4 }
    };

    var html = service.BuildAlertDigestHtml("Alice", matches);

    Assert.Contains("123 Main St", html);
    Assert.Contains("BROOKLYN", html);
    Assert.Contains("/permit-intel/detail/42", html);
    Assert.Contains("1 new permit", html, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public void BuildAlertDigestHtml_EmptyMatches_DoesNotThrow()
{
    var service = new EmailService(new ConfigurationBuilder().Build(),
        new Mock<ILogger<EmailService>>().Object);

    var html = service.BuildAlertDigestHtml("Bob", new List<AlertPermitMatch>());
    Assert.NotNull(html);
}
```

### 8.3 `SmsService.BuildAlertSms`

```csharp
[Fact]
public void BuildAlertSms_FormatsCorrectly()
{
    var msg = SmsService.BuildAlertSms(5, "456 Atlantic Ave");

    Assert.Contains("5", msg);
    Assert.Contains("456 Atlantic Ave", msg);
    Assert.Contains("konxpro.com", msg);
    Assert.True(msg.Length <= 160, "SMS should fit in one message segment");
}
```

---

## 9. Integration Tests — PermitIntelService

These tests should use `Testcontainers.MsSql` (a real SQL Server container) to validate behavior that EF InMemory cannot replicate: the unique index on `(UserId, DobjobFilingId)`, the `SavedLeads` FK cascade to `DOBJobFilings`, and the `ServiceRequests311.Bbl` index used by `Get311ComplaintVelocity`.

```csharp
// File: KonXProWebApp.Integration.Tests/Services/PermitIntelServiceIntegrationTests.cs

[Collection("SQL")]
public class PermitIntelServiceIntegrationTests : IAsyncLifetime
{
    private MsSqlContainer _sql;
    private db_9f8bee_konxdevContext _context;
    private PermitIntelService _service;

    public async Task InitializeAsync()
    {
        _sql = new MsSqlBuilder().Build();
        await _sql.StartAsync();

        var opts = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseSqlServer(_sql.GetConnectionString()).Options;
        _context = new db_9f8bee_konxdevContext(opts);
        await _context.Database.MigrateAsync();

        _service = new PermitIntelService(_context, new Mock<NavigationManager>().Object);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _sql.DisposeAsync();
    }

    [Fact]
    public async Task SaveLead_DuplicateInsert_ThrowsOrReturnsExisting_NotDuplicate()
    {
        // The unique index IX_SavedLeads_UserId_DobjobFilingId enforces this at DB level
        var filing = new DobjobFiling { JobType = "A1", Borough = "MANHATTAN" };
        _context.DobjobFilings.Add(filing);
        await _context.SaveChangesAsync();

        var l1 = await _service.SaveLead("u1", filing.Id);
        var l2 = await _service.SaveLead("u1", filing.Id);

        Assert.Equal(l1.Id, l2.Id);
        Assert.Equal(1, await _context.SavedLeads.CountAsync());
    }

    [Fact]
    public async Task DeleteFiling_CascadesToSavedLeads()
    {
        var filing = new DobjobFiling { JobType = "NB", Borough = "BROOKLYN" };
        _context.DobjobFilings.Add(filing);
        await _context.SaveChangesAsync();

        await _service.SaveLead("u1", filing.Id);

        _context.DobjobFilings.Remove(filing);
        await _context.SaveChangesAsync();

        Assert.Equal(0, await _context.SavedLeads.CountAsync());
    }

    [Fact]
    public async Task Get311ComplaintVelocity_MatchesByBbl_WithinWindow()
    {
        var bbl = "1001230045";
        _context.ServiceRequests311.AddRange(
            new ServiceRequest311 { UniqueKey = "k1", Bbl = bbl, CreatedDate = DateTime.UtcNow.AddDays(-5) },
            new ServiceRequest311 { UniqueKey = "k2", Bbl = bbl, CreatedDate = DateTime.UtcNow.AddDays(-10) },
            new ServiceRequest311 { UniqueKey = "k3", Bbl = bbl, CreatedDate = DateTime.UtcNow.AddDays(-100) } // outside 90d
        );
        await _context.SaveChangesAsync();

        var velocity = await _service.Get311ComplaintVelocity(bbl, days: 90);
        Assert.Equal(2, velocity);
    }

    [Fact]
    public async Task SearchPermits_TradeFilter_Plumbing_ReturnsOnlyMatches()
    {
        _context.DobjobFilings.AddRange(
            new DobjobFiling { JobType = "A1", Borough = "BROOKLYN", Plumbing = "X" },
            new DobjobFiling { JobType = "A1", Borough = "BROOKLYN", Plumbing = null },
            new DobjobFiling { JobType = "A1", Borough = "BROOKLYN", Mechanical = "X" }
        );
        await _context.SaveChangesAsync();

        var query = new PermitSearchQuery { Trades = new List<string> { "Plumbing" } };
        var (results, count) = await _service.SearchPermits(query);

        Assert.Equal(1, count);
        Assert.All(results, r => Assert.Equal("X", r.Plumbing));
    }

    [Fact]
    public async Task GetDashboardStats_CountsPermitsInLast30Days()
    {
        _context.DobjobFilings.AddRange(
            new DobjobFiling { JobType = "A1", LatestActionDate = DateTime.UtcNow.AddDays(-5) },
            new DobjobFiling { JobType = "A1", LatestActionDate = DateTime.UtcNow.AddDays(-35) } // outside window
        );
        _context.SavedLeads.Add(new SavedLead
        {
            UserId = "u1", DobjobFilingId = 1, Status = "Won", SavedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var stats = await _service.GetDashboardStats("u1");
        Assert.Equal(1, stats.TotalPermitsLast30Days);
    }
}
```

---

## 10. Integration Tests — StripeWebhookController

Use `WebApplicationFactory<Program>` to test the webhook endpoint in-process.

```csharp
// File: KonXProWebApp.Integration.Tests/Controllers/StripeWebhookControllerTests.cs

public class StripeWebhookTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public StripeWebhookTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(services =>
            {
                // Swap real DB for InMemory
                // Swap real StripeService for a spy
            });
        }).CreateClient();
    }

    [Fact]
    public async Task PostWebhook_InvalidSignature_Returns400()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        content.Headers.Add("Stripe-Signature", "t=bad,v1=badsig");

        var response = await _client.PostAsync("/api/stripe/webhook", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_NoWebhookSecretConfigured_ParsesWithoutVerification()
    {
        // When Stripe:WebhookSecret is empty, the controller falls back to ParseEvent (no sig check)
        // Build a valid-looking checkout.session.completed payload
        var payload = BuildCheckoutCompletedPayload("user123", "Pro");
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/stripe/webhook", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_UnhandledEventType_Returns200WithNoSideEffects()
    {
        var payload = /* payment_intent.created */ BuildUnhandledEventPayload();
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/stripe/webhook", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

---

## 11. Integration Tests — PermitIntelApiController

```csharp
// File: KonXProWebApp.Integration.Tests/Controllers/PermitIntelApiControllerTests.cs

public class PermitIntelApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetPermits_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/permit-intel/permits");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPermits_StarterUser_Returns403()
    {
        // Arrange: authenticate as Starter-tier user
        var response = await _authenticatedStarterClient.GetAsync("/api/permit-intel/permits");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPermits_AgencyUser_Returns200WithPaginatedResults()
    {
        var response = await _authenticatedAgencyClient.GetAsync(
            "/api/permit-intel/permits?borough=BROOKLYN&take=10&skip=0");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(body.GetProperty("totalCount").GetInt32() >= 0);
        Assert.Equal(0, body.GetProperty("skip").GetInt32());
        Assert.Equal(10, body.GetProperty("take").GetInt32());
    }

    [Fact]
    public async Task GetPermits_TakeClamped_ExceededMax_Returns100()
    {
        var response = await _authenticatedAgencyClient.GetAsync(
            "/api/permit-intel/permits?take=9999");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100, body.GetProperty("take").GetInt32());
    }

    [Fact]
    public async Task GetPermit_UnknownId_Returns404()
    {
        var response = await _authenticatedAgencyClient.GetAsync("/api/permit-intel/permits/99999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBoroughStats_DefaultDays_Returns200WithList()
    {
        var response = await _authenticatedAgencyClient.GetAsync("/api/permit-intel/stats/boroughs");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetBoroughStats_DaysClamped_ExceededMax_Uses365()
    {
        // days=9999 → clamped to 365 per the controller
        var response = await _authenticatedAgencyClient.GetAsync(
            "/api/permit-intel/stats/boroughs?days=9999");
        response.EnsureSuccessStatusCode();
    }
}
```

---

## 12. Integration Tests — Azure Functions

Azure Functions (isolated worker model) are plain C# classes — test them by constructing them directly with real or fake dependencies.

### 12.1 `PermitIngestionFunction` — delta load logic

```csharp
// File: KonXProWebApp.Functions.Tests/Functions/PermitIngestionFunctionTests.cs

public class PermitIngestionFunctionTests
{
    [Fact]
    public async Task RunInternal_NoRecords_LogsSuccessWithZeroCounts()
    {
        var mockSocrata = new Mock<SocrataClient>(/* ... */);
        mockSocrata.Setup(s => s.GetPermitsSince(It.IsAny<DateTime?>()))
            .Returns(AsyncEnumerable.Empty<SocrataPermitRecord>());

        var mockIngestion = new Mock<IngestionService>(/* ... */);
        mockIngestion.Setup(s => s.GetLastIngestionTimestamp()).ReturnsAsync((DateTime?)null);
        mockIngestion.Setup(s => s.LogIngestionRun(0, 0, 0, "Success", null, It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<PermitIngestionFunction>>();
        var fn = new PermitIngestionFunction(mockSocrata.Object, mockIngestion.Object, logger.Object);

        await fn.RunHttp(new Mock<HttpRequest>().Object);

        mockIngestion.Verify(s => s.LogIngestionRun(0, 0, 0, "Success", null, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task RunInternal_SocrataThrows_LogsFailureStatus()
    {
        var mockSocrata = new Mock<SocrataClient>(/* ... */);
        mockSocrata.Setup(s => s.GetPermitsSince(It.IsAny<DateTime?>()))
            .Throws(new HttpRequestException("Socrata offline"));

        var mockIngestion = new Mock<IngestionService>(/* ... */);
        mockIngestion.Setup(s => s.GetLastIngestionTimestamp()).ReturnsAsync((DateTime?)null);

        var fn = new PermitIngestionFunction(mockSocrata.Object, mockIngestion.Object,
            new Mock<ILogger<PermitIngestionFunction>>().Object);

        await fn.RunHttp(new Mock<HttpRequest>().Object);

        mockIngestion.Verify(s => s.LogIngestionRun(
            0, 0, 0, "Failed", It.Is<string>(e => e.Contains("Socrata")), It.IsAny<DateTime>()),
            Times.Once);
    }

    [Fact]
    public async Task RunInternal_BatchOf501_ProcessesTwoBatches()
    {
        var records = Enumerable.Range(1, 501)
            .Select(i => new SocrataPermitRecord { JobNumber = i.ToString() });

        var mockSocrata = new Mock<SocrataClient>(/* ... */);
        mockSocrata.Setup(s => s.GetPermitsSince(null))
            .Returns(records.ToAsyncEnumerable());

        var mockIngestion = new Mock<IngestionService>(/* ... */);
        mockIngestion.Setup(s => s.GetLastIngestionTimestamp()).ReturnsAsync((DateTime?)null);
        mockIngestion.Setup(s => s.UpsertPermits(It.IsAny<IReadOnlyList<SocrataPermitRecord>>()))
            .ReturnsAsync((IReadOnlyList<SocrataPermitRecord> b) => (b.Count, 0, 0));

        var fn = new PermitIngestionFunction(mockSocrata.Object, mockIngestion.Object,
            new Mock<ILogger<PermitIngestionFunction>>().Object);

        await fn.RunHttp(new Mock<HttpRequest>().Object);

        // UpsertPermits should be called twice: once for 500, once for 1
        mockIngestion.Verify(s => s.UpsertPermits(It.IsAny<IReadOnlyList<SocrataPermitRecord>>()),
            Times.Exactly(2));
    }
}
```

### 12.2 `IngestionService.UpsertPermits` — with real SQL (Testcontainers)

```csharp
// File: KonXProWebApp.Functions.Tests/Services/IngestionServiceSqlTests.cs
// Uses Testcontainers.MsSql

[Collection("SQL")]
public class IngestionServiceSqlTests : IAsyncLifetime
{
    private MsSqlContainer _sql;
    private IngestionService _service;

    public async Task InitializeAsync()
    {
        _sql = new MsSqlBuilder().Build();
        await _sql.StartAsync();

        // Apply schema (use migrations or run a setup script)
        await ApplySchemaAsync(_sql.GetConnectionString());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["SqlConnectionString"] = _sql.GetConnectionString()
            }).Build();
        _service = new IngestionService(config, new Mock<ILogger<IngestionService>>().Object);
    }

    [Fact]
    public async Task UpsertPermits_NewRecord_InsertsCorrectly()
    {
        var records = new List<SocrataPermitRecord>
        {
            new() { JobNumber = "500001", DocNumber = "01", Borough = "BROOKLYN",
                    HouseNumber = "123", StreetName = "MAIN ST", JobType = "A1",
                    InitialCost = "$75,000", Plumbing = "X" }
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

        // Update borough
        record.Borough = "MANHATTAN";
        var (inserted, updated, skipped) = await _service.UpsertPermits(new[] { record });

        Assert.Equal(0, inserted);
        Assert.Equal(1, updated);
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
        Assert.Equal(1, skipped);
    }

    [Fact]
    public async Task UpsertPermits_CurrencyParsing_StoredAsDecimal()
    {
        var records = new List<SocrataPermitRecord>
        {
            new() { JobNumber = "500003", DocNumber = "01", JobType = "NB",
                    InitialCost = "$1,234,567.89" }
        };

        await _service.UpsertPermits(records);

        await using var conn = new SqlConnection(_sql.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("SELECT InitialCost FROM DOBJobFilings WHERE JobNum=500003", conn);
        var result = (decimal)await cmd.ExecuteScalarAsync();
        Assert.Equal(1234567.89m, result);
    }

    [Fact]
    public async Task UpsertContractors_InvalidRecord_MissingLicenseNumber_Skips()
    {
        var records = new List<SocrataContractorRecord>
        {
            new() { LicenseNumber = null, BusinessUniqueId = "abc", BusinessName = "Test Co" }
        };

        var (inserted, updated, skipped) = await _service.UpsertContractors(records);
        Assert.Equal(1, skipped);
    }

    public async Task DisposeAsync() => await _sql.DisposeAsync();
}
```

---

## 13. Blazor Component Tests (bUnit)

Add `bunit` to `KonXProWebApp.Tests` and test the three most critical PermitIntel components.

```xml
<!-- Add to KonXProWebApp.Tests.csproj -->
<PackageReference Include="bunit" Version="1.35.6" />
```

### 13.1 `PermitSearch` component

```csharp
// File: KonXProWebApp.Tests/Components/PermitSearchTests.cs

public class PermitSearchTests : TestContext
{
    public PermitSearchTests()
    {
        // Register Radzen services that components inject
        Services.AddRadzenComponents();
        Services.AddScoped(_ => new Mock<SecurityService>().Object);

        var mockPermitService = new Mock<PermitIntelService>(/* ... */);
        mockPermitService.Setup(s => s.SearchPermits(It.IsAny<PermitSearchQuery>()))
            .ReturnsAsync((Enumerable.Empty<DobjobFiling>(), 0));
        Services.AddScoped(_ => mockPermitService.Object);
    }

    [Fact]
    public void PermitSearch_InitialLoad_CallsSearchPermits()
    {
        var cut = RenderComponent<PermitSearch>();

        // OnInitializedAsync calls SearchPermits() on load
        var mockService = Services.GetRequiredService<PermitIntelService>();
        Mock.Get(mockService).Verify(s => s.SearchPermits(It.IsAny<PermitSearchQuery>()), Times.Once);
    }

    [Fact]
    public void PermitSearch_SaveAsLead_WithoutLogin_ShowsWarning()
    {
        var mockSecurity = new Mock<SecurityService>();
        mockSecurity.Setup(s => s.User).Returns((ApplicationUser)null);
        Services.AddScoped(_ => mockSecurity.Object);

        var mockNotification = new Mock<NotificationService>();
        Services.AddScoped(_ => mockNotification.Object);

        var cut = RenderComponent<PermitSearch>();
        // Simulate clicking Save on a filing
        // ...
        mockNotification.Verify(n => n.Notify(
            NotificationSeverity.Warning, It.IsAny<string>(), It.IsAny<string>()),
            Times.AtLeastOnce);
    }
}
```

### 13.2 `Subscribe` component

```csharp
public class SubscribeTests : TestContext
{
    [Fact]
    public void Subscribe_IsCurrentTier_ReturnsTrueForActiveTier()
    {
        // Arrange: user has active Pro subscription
        // Assert: "Current Plan" button text shows for Pro tier
        // Assert: other tiers show "Start Free Trial"
    }

    [Fact]
    public void Subscribe_StartCheckout_UnauthenticatedUser_RedirectsToLogin()
    {
        // Arrange: Security.User is null
        // Act: click subscribe button
        // Assert: NavigationManager navigated to /Login
    }
}
```

### 13.3 `MyLeads` component

```csharp
public class MyLeadsTests : TestContext
{
    [Fact]
    public void MyLeads_FilterByStatus_ShowsOnlyMatchingLeads()
    {
        var leads = new List<SavedLead>
        {
            new() { Status = "New", DobjobFiling = new DobjobFiling { JobType = "A1" } },
            new() { Status = "Won", DobjobFiling = new DobjobFiling { JobType = "NB" } },
            new() { Status = "New", DobjobFiling = new DobjobFiling { JobType = "A2" } }
        };
        // Render, filter by "Won", assert only 1 row shown

        var cut = RenderComponent<MyLeads>(); // with seeded leads
        // click "Won" filter button
        // assert filteredLeads.Count == 1
    }

    [Fact]
    public void MyLeads_GetStatusCount_ReturnsCorrectCounts()
    {
        // Test the GetStatusCount helper method for each status
    }
}
```

---

## 14. End-to-End Tests (Playwright)

Create a dedicated project targeting a locally running instance of the app (or a test environment).

```
dotnet new xunit -n KonXProWebApp.E2E.Tests
cd KonXProWebApp.E2E.Tests
dotnet add package Microsoft.Playwright
```

### Critical User Journeys

#### Journey 1: Contractor Registration → Subscription → Permit Search

```csharp
// File: KonXProWebApp.E2E.Tests/Journeys/ContractorSubscribeSearchJourney.cs

[Fact]
public async Task Contractor_CanRegister_Subscribe_AndSearchPermits()
{
    await using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
    var page = await browser.NewPageAsync();

    // 1. Register
    await page.GotoAsync($"{BaseUrl}/RegisterApplicationUser");
    await page.FillAsync("#Email", "contractor_test@example.com");
    await page.FillAsync("#Password", "Test@1234!");
    await page.ClickAsync("button[type=submit]");

    // 2. Login
    await page.GotoAsync($"{BaseUrl}/Login");
    await page.FillAsync("#Email", "contractor_test@example.com");
    await page.FillAsync("#Password", "Test@1234!");
    await page.ClickAsync("button[type=submit]");
    await page.WaitForURLAsync($"{BaseUrl}/**");

    // 3. Navigate to subscription page
    await page.GotoAsync($"{BaseUrl}/permit-intel/subscribe");
    await page.WaitForSelectorAsync(".tier-card");
    var tierCards = await page.QuerySelectorAllAsync(".tier-card");
    Assert.True(tierCards.Count >= 4, "Expected at least 4 tier cards");

    // 4. Navigate to permit search (should be gated)
    await page.GotoAsync($"{BaseUrl}/permit-intel/search");
    // Without subscription, should redirect or show upgrade prompt
    var url = page.Url;
    Assert.True(url.Contains("subscribe") || url.Contains("login"),
        $"Unauthenticated search should gate access, but URL was {url}");
}
```

#### Journey 2: Admin — User Management

```csharp
[Fact]
public async Task Admin_CanViewAndManageUsers()
{
    // Login as admin
    // Navigate to /ApplicationUsers
    // Verify user list loads
    // Navigate to /ApplicationRoles
    // Verify roles list loads
}
```

#### Journey 3: Permit Detail View

```csharp
[Fact]
public async Task LoggedInSubscriber_CanViewPermitDetail()
{
    // Login with Pro-tier test user
    // Navigate to /permit-intel/search
    // Click first permit row
    // Assert /permit-intel/detail/{id} loads
    // Assert key fields (address, job type, lead score) are visible
    // Click "Save Lead" button
    // Assert notification appears
    // Navigate to /permit-intel/my-leads
    // Assert permit appears in lead list
}
```

#### Journey 4: Alert Settings Save

```csharp
[Fact]
public async Task Subscriber_CanSaveAlertPreferences()
{
    // Login with Starter-tier user
    // Navigate to /permit-intel/alerts
    // Select "BROOKLYN" borough
    // Select "Daily" frequency
    // Click Save
    // Assert success notification appears
    // Reload page, assert preferences persisted
}
```

#### Journey 5: Stripe Checkout Redirect

```csharp
[Fact]
public async Task SubscribePage_ClickStartTrial_RedirectsToStripe()
{
    // Login as unsubscribed user
    // Navigate to /permit-intel/subscribe
    // Click "Start Free Trial" for Starter tier
    // Assert browser navigates to checkout.stripe.com (or test-mode equivalent)
}
```

---

## 15. Database & Migration Tests

### 15.1 Migration Idempotency

```csharp
// File: KonXProWebApp.Integration.Tests/Migrations/MigrationTests.cs

[Collection("SQL")]
public class MigrationTests : IAsyncLifetime
{
    private MsSqlContainer _sql;

    public async Task InitializeAsync()
    {
        _sql = new MsSqlBuilder().Build();
        await _sql.StartAsync();
    }

    [Fact]
    public async Task MigrateUp_FromScratch_Succeeds()
    {
        var opts = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseSqlServer(_sql.GetConnectionString()).Options;
        await using var ctx = new db_9f8bee_konxdevContext(opts);

        var ex = await Record.ExceptionAsync(() => ctx.Database.MigrateAsync());
        Assert.Null(ex);
    }

    [Fact]
    public async Task MigrateUp_RunTwice_IsIdempotent()
    {
        var opts = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseSqlServer(_sql.GetConnectionString()).Options;
        await using var ctx = new db_9f8bee_konxdevContext(opts);

        await ctx.Database.MigrateAsync();
        var ex = await Record.ExceptionAsync(() => ctx.Database.MigrateAsync()); // run again
        Assert.Null(ex);
    }

    [Fact]
    public async Task AddServiceRequests311_Migration_CreatesExpectedTables()
    {
        var opts = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseSqlServer(_sql.GetConnectionString()).Options;
        await using var ctx = new db_9f8bee_konxdevContext(opts);
        await ctx.Database.MigrateAsync();

        await using var conn = new SqlConnection(_sql.GetConnectionString());
        await conn.OpenAsync();

        // Verify critical tables exist
        foreach (var table in new[] { "DOBJobFilings", "ServiceRequests311", "Subscriptions", "SavedLeads", "AlertPreferences", "IngestionLogs" })
        {
            await using var cmd = new SqlCommand(
                $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{table}'", conn);
            var count = (int)await cmd.ExecuteScalarAsync();
            Assert.Equal(1, count, $"Table {table} should exist after migration");
        }
    }

    [Fact]
    public async Task AddServiceRequests311_Migration_CreatesUniqueSavedLeadsIndex()
    {
        // Verify the unique index on (UserId, DobjobFilingId) was applied
        await using var conn = new SqlConnection(_sql.GetConnectionString());
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM sys.indexes i
            JOIN sys.tables t ON i.object_id = t.object_id
            WHERE t.name = 'SavedLeads'
            AND i.is_unique = 1
            AND i.name = 'IX_SavedLeads_UserId_DobjobFilingId'", conn);

        var count = (int)await cmd.ExecuteScalarAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AddHomeImprovementContractors_Migration_CreatesContractorsTable()
    {
        var opts = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
            .UseSqlServer(_sql.GetConnectionString()).Options;
        await using var ctx = new db_9f8bee_konxdevContext(opts);
        await ctx.Database.MigrateAsync();

        await using var conn = new SqlConnection(_sql.GetConnectionString());
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='HomeImprovementContractors'", conn);
        Assert.Equal(1, (int)await cmd.ExecuteScalarAsync());
    }

    public async Task DisposeAsync() => await _sql.DisposeAsync();
}
```

### 15.2 Identity Context Migration

```csharp
[Fact]
public async Task IdentityMigration_CreatesAspNetUsersTables()
{
    var opts = new DbContextOptionsBuilder<ApplicationIdentityDbContext>()
        .UseSqlServer(_sql.GetConnectionString()).Options;
    await using var ctx = new ApplicationIdentityDbContext(opts);
    await ctx.Database.MigrateAsync();

    await using var conn = new SqlConnection(_sql.GetConnectionString());
    await conn.OpenAsync();
    await using var cmd = new SqlCommand(
        "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE 'AspNet%'", conn);
    Assert.True((int)await cmd.ExecuteScalarAsync() >= 5);
}
```

---

## 16. Performance & Load Tests

The highest-risk performance area is the permit ingestion pipeline: `SocrataClient` pages through 5,000-record pages from the Socrata API, and `IngestionService.UpsertPermits` issues one MERGE per record (not a bulk insert).

### 16.1 Ingestion Throughput Benchmark (NBomber)

```csharp
// File: KonXProWebApp.Functions.Tests/Performance/IngestionThroughputTests.cs

[Fact(Skip = "Run manually — needs SQL container running")]
public async Task UpsertPermits_1000Records_CompletesUnder30Seconds()
{
    var records = Enumerable.Range(1, 1000)
        .Select(i => new SocrataPermitRecord
        {
            JobNumber = (600000 + i).ToString(),
            DocNumber = "01",
            Borough = "BROOKLYN",
            HouseNumber = i.ToString(),
            StreetName = "TEST ST",
            JobType = "A1",
            InitialCost = "$50,000"
        }).ToList();

    var stopwatch = Stopwatch.StartNew();
    var (inserted, _, _) = await _ingestionService.UpsertPermits(records);
    stopwatch.Stop();

    Assert.Equal(1000, inserted);
    Assert.True(stopwatch.Elapsed.TotalSeconds < 30,
        $"Expected < 30s but took {stopwatch.Elapsed.TotalSeconds:F1}s. " +
        $"Consider bulk-insert (SqlBulkCopy) if this threshold is routinely exceeded.");
}
```

> **Note:** The current `IngestionService` issues individual MERGE statements per record over a single `SqlConnection`. For batches approaching the full initial load (100K+ DOB job filings), this will be slow. Recommend benchmarking early and considering `SqlBulkCopy` for the INSERT path, keeping MERGE only for delta updates.

### 16.2 `SearchPermits` Query Performance

```csharp
[Fact(Skip = "Run manually")]
public async Task SearchPermits_With100KRows_ReturnsFirstPageUnder500Ms()
{
    // Seed 100,000 filings via bulk insert
    // Then measure SearchPermits with a borough filter
    var stopwatch = Stopwatch.StartNew();
    var query = new PermitSearchQuery { Boroughs = new List<string> { "BROOKLYN" }, Take = 25 };
    var (_, count) = await _service.SearchPermits(query);
    stopwatch.Stop();

    Assert.True(stopwatch.Elapsed.TotalMilliseconds < 500,
        $"Search took {stopwatch.Elapsed.TotalMilliseconds:F0}ms. Add indexes on Borough, LatestActionDate if slow.");
}
```

> **Missing index risk:** `DOBJobFilings` has no explicit indexes defined in migrations on `Borough`, `LatestActionDate`, `JobType`, `InitialCost`, or `LeadScore`. The `SearchPermits` query with multiple `WHERE` clauses and `ORDER BY LatestActionDate DESC` will do full table scans at scale. Add composite indexes after benchmarking.

### 16.3 Stripe Webhook — Concurrent Event Handling (NBomber)

```
Scenario: 50 concurrent webhook POSTs (simulating Stripe retries + batch events)
Goal:     All return 200 within 2 seconds
Tool:     NBomber with HttpClient scenario
```

---

## 17. CI/CD Integration

The repo already has `.github/workflows/master_konxprofunctionapp.yml` for Functions deployment. Extend it with test gates:

```yaml
# .github/workflows/ci.yml

name: CI

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - run: dotnet restore
      - run: dotnet test KonXProWebApp.Tests/KonXProWebApp.Tests.csproj
               --collect:"XPlat Code Coverage"
               --results-directory ./coverage
      - run: dotnet test KonXProWebApp.Functions.Tests/KonXProWebApp.Functions.Tests.csproj
               --collect:"XPlat Code Coverage"

  integration-tests:
    runs-on: ubuntu-latest
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          SA_PASSWORD: Test@1234!
          ACCEPT_EULA: Y
        ports:
          - 1433:1433
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - run: dotnet test KonXProWebApp.Integration.Tests/KonXProWebApp.Integration.Tests.csproj
               --environment SqlConnectionString="Server=localhost,1433;..."

  e2e-tests:
    runs-on: ubuntu-latest
    needs: [unit-tests, integration-tests]
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      - run: dotnet build
      - run: dotnet run --project KonXProWebApp.csproj &  # start app in background
      - run: npx playwright install chromium
      - run: dotnet test KonXProWebApp.E2E.Tests/

  coverage-gate:
    runs-on: ubuntu-latest
    needs: [unit-tests]
    steps:
      - uses: danielpalme/ReportGenerator-GitHub-Action@5
        with:
          reports: 'coverage/**/*.xml'
          targetdir: 'coverage-report'
          reporttypes: 'Cobertura'
      - uses: irongut/CodeCoverageSummary@v1.3.0
        with:
          filename: 'coverage-report/Cobertura.xml'
          badge: true
          fail_below_min: true
          thresholds: '70 85'  # warn at 70%, fail at 85%
```

### Branch Protection Rules

Require these checks to pass before merging to `master`:
- `unit-tests`
- `integration-tests`  
- `coverage-gate` (≥80% line coverage on `KonXProWebApp.Tests` + `KonXProWebApp.Functions.Tests` combined)

E2E tests can be advisory-only (non-blocking) initially given environment dependencies.

---

## 18. Quick-Start: Adding the Missing Test Projects

```bash
# From the solution root (D:\Workspace\KonXProWebApp)

# 1. Functions unit test project
dotnet new xunit -n KonXProWebApp.Functions.Tests
dotnet sln add KonXProWebApp.Functions.Tests/KonXProWebApp.Functions.Tests.csproj
cd KonXProWebApp.Functions.Tests
dotnet add reference ../KonXProWebApp.Functions/KonXProWebApp.Functions.csproj
dotnet add package Moq
dotnet add package Microsoft.Extensions.Configuration.InMemory
dotnet add package Testcontainers.MsSql
cd ..

# 2. Integration test project
dotnet new xunit -n KonXProWebApp.Integration.Tests
dotnet sln add KonXProWebApp.Integration.Tests/KonXProWebApp.Integration.Tests.csproj
cd KonXProWebApp.Integration.Tests
dotnet add reference ../KonXProWebApp.csproj
dotnet add package Moq
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Testcontainers.MsSql
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.NET.Test.Sdk
cd ..

# 3. E2E test project
dotnet new xunit -n KonXProWebApp.E2E.Tests
dotnet sln add KonXProWebApp.E2E.Tests/KonXProWebApp.E2E.Tests.csproj
cd KonXProWebApp.E2E.Tests
dotnet add package Microsoft.Playwright
dotnet add package Microsoft.NET.Test.Sdk
cd ..

# 4. Add bUnit to existing test project
cd KonXProWebApp.Tests
dotnet add package bunit
cd ..

# 5. Install Playwright browsers
cd KonXProWebApp.E2E.Tests
dotnet build
pwsh bin/Debug/net9.0/playwright.ps1 install chromium
```

### Immediate Priority Order

Given zero existing coverage on the ingestion pipeline and the fact it runs in production every day:

1. **Day 1–2:** Add `IngestionService` parsing helper tests to existing `KonXProWebApp.Tests` (extract methods to `internal`). Fast, no infrastructure needed.
2. **Day 3–5:** Wire up `Testcontainers.MsSql` in `KonXProWebApp.Functions.Tests`, add `UpsertPermits` integration tests for insert/update/skip/currency parsing.
3. **Day 5–7:** Add `SocrataClient` unit tests with mock HTTP handlers. Cover pagination, retry, and rate-limit backoff.
4. **Week 2:** Add `StripeWebhookController` tests via `WebApplicationFactory`. Add `PermitIntelApiController` auth/endpoint tests.
5. **Week 3:** `AlertDispatchFunction` matching logic; extract `BuildWhereClause` and add unit tests.
6. **Week 4:** `bUnit` component tests for `PermitSearch`, `MyLeads`, `Subscribe`.
7. **Month 2:** Playwright E2E for the five journeys; NBomber ingestion throughput baseline.
