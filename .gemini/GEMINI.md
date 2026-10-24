# KonXProWebApp — Unified AI Project Guide (Claude Code · Antigravity · Gemini)

> Synced with repository & database ground truth through 2026-09-19 (see REMEDIATION.md). If anything here disagrees with the code, the code is right — update this file.

## What it is
B2B SaaS delivering NYC Department of Buildings permit intelligence to contractors. Subscribers get alerts when permits are filed in their territory so they can reach homeowners before competitors.

**Live domain:** konxpro.com · **Staging:** staging.konxpro.com  
**Market:** Contractors across the five NYC boroughs  
**Billing:** Stripe subscriptions across four tiers — Starter, Pro, Business, Agency

---

## Tech stack

| Layer | Actual technology |
| :--- | :--- |
| Web app | ASP.NET Core, `Microsoft.NET.Sdk.Web`, **net9.0** |
| UI | **Blazor Server** — Razor Components with `InteractiveServerRenderMode` |
| Component library | **Radzen.Blazor** (pinned via `Directory.Packages.props`) |
| ORM | EF Core 9 with **`UseSqlServer`** |
| Database | **Microsoft SQL Server 2022 Express**, hosted on Plesk (`priority_konx` prod / `priority_ks` staging) |
| Identity | ASP.NET Core Identity, multi-tenant |
| Query API | `Microsoft.AspNetCore.OData` at route `odata/Identity` |
| Background jobs | **Azure Functions v4, isolated worker**, `KonXProWebApp.Functions`, **net8.0** |
| Package management | **Central Package Management** (`Directory.Packages.props` at repo root — no `Version` attributes in `.csproj` files) |
| Payments | Stripe.net 52.1.0 |
| CI / PR Gate | `.github/workflows/build-and-test.yml` (builds solution + runs unit, function, and Testcontainers integration tests on PRs to `master`) |
| Web app deploy | **Web Deploy** via `.pubxml` publish profile (manual) |
| Functions deploy | `.github/workflows/master_konxprofunctionapp.yml` → Azure Function App `KonXProFunctionApp` |
| Default branch | **`master`** |

There is **no PostgreSQL, no PostGIS, no Dapper, no Hangfire, no NetTopologySuite, and no Blazor WebAssembly** anywhere in this solution.

---

## Solution layout

Six projects rooted at the repository root (no `src/` folder):
- `Directory.Packages.props` — Central Package Management (all NuGet versions pinned here)
- `KonXProWebApp.csproj` — Web app at repo root (explicitly <Compile Remove>s sibling folders)
  - `Components/Pages/`, `Controllers/`, `Services/`, `Data/` (DbContexts, Migrations, `TierViewDefinitions.cs`), `Models/`, `Authorization/`, `Filters/`
- `KonXProWebApp.Functions/` — Azure Functions (ADO.NET ingestion + alerts)
- `KonXProWebApp.Tests/` — Unit tests (xUnit 2.8.0 + bUnit)
- `KonXProWebApp.Functions.Tests/`
- `KonXProWebApp.Integration.Tests/` — `WebApplicationFactory<Program>` + `Testcontainers.MsSql`
- `KonXProWebApp.E2E.Tests/` — Playwright E2E journeys

---

## Data & Schema Architecture (Read Before Touching EF Core or SQL)

### Two DbContexts, One Database (`dbo.__EFMigrationsHistory`)
Both contexts share `db_9f8bee_konxdevConnection` and explicitly pin `.MigrationsHistoryTable("__EFMigrationsHistory", "dbo")` in `Program.cs`:
1. **`ApplicationIdentityDbContext`** — Multi-tenant users/roles. Calls `identityDb.Database.Migrate()` at startup. (Physical Identity tables on prod/staging reside in `konx_admin`/`konx_staging_admin` schemas via default login schema, while migration history is baselined in `dbo.__EFMigrationsHistory`.)
2. **`db_9f8bee_konxdevContext`** — Permit, violation, contractor, and blog data. Calls `permitDb.Database.Migrate()` at startup (switched from `EnsureCreated()` on 2026-09-19). Migrations are baselined at `20260911165155_InitialCreate` and `20260919124444_AddBlogTables`.

### Critical Database Guardrails
- **Three Tables Are Synonyms (`ExcludeFromMigrations()`):** On production, `dbo.HomeImprovementContractors`, `dbo.DobBisViolations`, and `dbo.ECBViolations` are **SQL Server Synonyms** pointing to physical tables in `konx_admin`. Because synonyms cannot accept DDL (`ALTER TABLE`/`CREATE INDEX`), all three entities are configured with `.ToTable(..., tb => tb.ExcludeFromMigrations())` in `Db9f8beeKonxdevContext.PermitIntel.cs`. **Never remove `ExcludeFromMigrations()` from these three entities.**
- **Five Tier Views (`ToView()`):** `vwFreeTierDashboard`, `vwBasicTierDashboard`, `vwMidTierDashboard`, `vwHighTierDashboard`, and `vwDemoDisplay` are mapped via `.ToView()` in `OnModelCreating` and created/updated at app startup via `CREATE OR ALTER VIEW` from `Data/TierViewDefinitions.cs`. Entitlement boundaries:
  - `Free` vs `Basic`: separated by `HouseNum` (never on Free).
  - `Basic` vs `Mid`/`High`: separated by `EstimatedCost` (never on Free or Basic).
- **Intentional Pending Model Changes (Do Not Scaffold Blindly):** `DobjobFiling.cs` (`[StringLength]` and `money` column alignments) and `SavedLead` (`FK_SavedLeads_DOBJobFilings` with `DeleteBehavior.NoAction`) have runtime model fixes that intentionally differ from `InitialCreate`'s snapshot to match production without running a 75-column `AlterColumn` DDL migration. Do not run `dotnet ef migrations add` without reviewing whether it picks up these ~75 `DOBJobFilings` columns.
- **Startup Raw Table DDL is Retired:** The old `ExecuteSqlRaw` block that created `Subscriptions`, `SavedLeads`, and `AlertPreferences` was removed on 2026-09-12. Only the 5-view `CREATE OR ALTER VIEW` loop remains in `Program.cs`.

---

## Strict Safety & Command Rules
1. **Local `dotnet user-secrets` must ONLY point to `(localdb)\MSSQLLocalDB`.** Never store `priority_konx` or `priority_ks` credentials in `user-secrets`.
2. **Always pass an explicit LocalDB connection string inline on any `dotnet ef` command:**
   `dotnet ef ... --connection "Server=(localdb)\MSSQLLocalDB;Database=KonXPro_Dev;Trusted_Connection=True;"`
3. **NEVER run `dotnet ef database drop`, `sqlcmd`, or `dotnet publish` from an agent shell.**
4. **Package versions live exclusively in `Directory.Packages.props`.** Never add a `Version="..."` attribute to a `<PackageReference>` in any `.csproj`.
