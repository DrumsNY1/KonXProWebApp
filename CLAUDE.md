# KonXProWebApp — Claude Code Project Guide

> Every technical claim below was read from this repository on Sept 10, 2026.
> If something here disagrees with the code, the code is right — fix this file.

## What it is

B2B SaaS delivering NYC Department of Buildings permit intelligence to contractors.
Subscribers get alerts when permits are filed in their territory so they can reach
homeowners before competitors.

**Live domain:** konxpro.com
**Market:** contractors across the five NYC boroughs
**Billing:** Stripe subscriptions across four tiers — Starter, Pro, Business, Agency

---

## Tech stack

| Layer | Actual technology |
|-------|------------------|
| Web app | ASP.NET Core, `Microsoft.NET.Sdk.Web`, **net9.0** |
| UI | **Blazor Server** — Razor Components with `InteractiveServerRenderMode` |
| Component library | **Radzen.Blazor** 11.3.2 |
| ORM | EF Core 9 with **`UseSqlServer`** |
| Database | **Microsoft SQL Server**, hosted on Plesk (`plesk9100.is.cc`) |
| Identity | ASP.NET Core Identity, multi-tenant |
| Query API | `Microsoft.AspNetCore.OData` at route `odata/Identity` |
| Background jobs | **Azure Functions v4, isolated worker**, `KonXProWebApp.Functions`, **net8.0** |
| Payments | Stripe.net 52.1.0 |
| Web app deploy | **Web Deploy** to konxpro.com via a `.pubxml` publish profile, run manually |
| Functions deploy | GitHub Actions → Azure Function App `KonXProFunctionApp` |
| Default branch | **`master`** |

There is **no PostgreSQL, no PostGIS, no Dapper, no Hangfire, no NetTopologySuite,
and no Blazor WebAssembly** anywhere in this solution. If a task seems to call for
one of those, stop and ask — an earlier version of this file claimed all of them and
it was wrong.

---

## Solution layout

Six projects, all rooted at the repository root. There is **no `src/` folder**.

```
KonXProWebApp.sln
├─ KonXProWebApp.csproj          ← the web app, at the repo root
│  ├─ Components/Pages/          ← Razor pages (see Page map)
│  ├─ Controllers/               ← MVC + OData controllers
│  ├─ Services/                  ← business logic
│  ├─ Data/                      ← DbContexts + Migrations
│  ├─ Models/
│  ├─ Authorization/             ← SubscriptionRequirement + handler
│  └─ Filters/
├─ KonXProWebApp.Functions/      ← Azure Functions (ingestion + alerts)
├─ KonXProWebApp.Tests/          ← unit tests
├─ KonXProWebApp.Functions.Tests/
├─ KonXProWebApp.Integration.Tests/  ← WebApplicationFactory<Program>
└─ KonXProWebApp.E2E.Tests/      ← Playwright (PlaywrightFixture, Journeys/)
```

Because the web app csproj sits at the root, it explicitly `<Compile Remove>`s every
sibling project folder. **If you add a new project, add matching Remove items** or the
root project will try to compile it.

---

## Data

### Two DbContexts, one database

Both point at the same connection string, `db_9f8bee_konxdevConnection`.

- **`ApplicationIdentityDbContext`** — users, roles, tenants. Managed by EF Core
  migrations in `Data/Migrations/`. `Database.Migrate()` runs at startup.
- **`db_9f8bee_konxdevContext`** — permit and contractor data, scaffolded from an
  existing database. Split across `Db9f8beeKonxdevContext.cs` and
  `Db9f8beeKonxdevContext.PermitIntel.cs`.

### The migrations history table was NOT in dbo (as of the 2026-09-10 capture)

Each SQL login's default schema is its own name, so EF put `__EFMigrationsHistory`
in `konx_admin` (production) and `konx_staging_admin` (staging) rather than `dbo`.
Both DbContext registrations in `Program.cs` now pin
`MigrationsHistoryTable("__EFMigrationsHistory", "dbo")` explicitly, so a freshly
migrated database gets it in `dbo` regardless of which login connects — this
was folded into the database rebuild described in `REMEDIATION.md` rather than
migrated in place. See `REMEDIATION.md`'s "Rebuild plan" section for the current
status of that rebuild (code is ready; staging/production have not been rebuilt yet).

### Production and staging schemas do not match

They were built by different mechanisms. Production's permit tables carry
EF-generated key names and every index the model declares. Staging's carry SQL
Server auto-generated names from the `Program.cs` raw DDL, lack both unique
indexes, and `IngestionLogs` there is a heap. **Do not treat staging as a
rehearsal for production.** `REMEDIATION.md` has the full comparison.

### How the schema is actually managed — read this before touching the database

`Program.cs` does three different things at startup, and only the first is migrations:

1. `identityDb.Database.Migrate()` — proper EF migrations for Identity.
2. `permitDb.Database.EnsureCreated()` — **not** migrations.
3. Raw `ExecuteSqlRaw` DDL that creates `Subscriptions`, `SavedLeads` and
   `AlertPreferences` if absent, then `CREATE OR ALTER VIEW` for five tier views
   (the view SQL itself lives in `Data/TierViewDefinitions.cs`, shared with the
   entitlement tests in `KonXProWebApp.Integration.Tests`, not inlined here).

So the app **runs raw DDL against whatever database it boots against, including
production.** This is the single most surprising thing in the codebase. Consequences:

- Changing those three tables or five views means editing the SQL strings in
  `Program.cs`, not adding a migration.
- Adding an EF migration for them will conflict with the startup DDL.
- Any change to that block ships to production the moment the app restarts.

Do not "clean this up" as a side effect of another task. If it should move to
migrations, that is its own piece of work with its own testing.

### Key tables and views

Tables: `DOBJobFilings` (the core permit table), `Subscriptions`, `SavedLeads`,
`AlertPreferences`, plus Identity tables and contractor/violation tables.

Views: `vwFreeTierDashboard`, `vwBasicTierDashboard`, `vwMidTierDashboard`,
`vwHighTierDashboard`, `vwDemoDisplay` — each exposes progressively more columns
(`InitialCost` appears only at Mid and High). The tier views are the enforcement
point for what a subscriber can see, so **changing a view changes entitlements.**

### Ingestion

Seven Azure Functions in `KonXProWebApp.Functions/Functions/`:

`PermitIngestionFunction`, `DobNowIngestionFunction`, `DobViolationIngestionFunction`,
`HpdViolationIngestionFunction`, `ServiceRequest311IngestionFunction`,
`ContractorIngestionFunction`, and `AlertDispatchFunction`.

All are timer-triggered (`Worker.Extensions.Timer`). Note the Functions project does
**not** reference EF Core — it talks to SQL Server through `Microsoft.Data.SqlClient`
directly. Ingestion code is hand-written ADO.NET; don't reach for a DbContext there.

Source data is NYC public record. Fields exposed to subscribers include owner name,
address, permit type, filing date and borough. The DOB data disclaimer in the Privacy
Notice covers this — it is public record and not guaranteed accurate.

---

## Services

In `Services/`, all registered scoped in `Program.cs`:

- `PermitIntelService` (~34 KB — the core query surface)
- `ContractorIntelService`
- `ComplianceIntelService`
- `StripeService`
- `SubscriptionTierService`
- `SecurityService`
- `Db9f8beeKonxdevService` (~44 KB, scaffolded CRUD — prefer the Intel services for new work)

---

## Authorization

Four policies registered in `Program.cs`: `RequiresStarter`, `RequiresPro`,
`RequiresBusiness`, `RequiresAgency`. Each adds a `SubscriptionRequirement`, evaluated
by `Authorization/SubscriptionAuthorizationHandler`.

`Components/Pages/PermitIntel/RequireTier.razor` is the component-level gate.

Identity is **multi-tenant**: `ApplicationTenant`, `MultiTenancyUserStore` registered
as `IUserStore<ApplicationUser>`. A user lookup that ignores tenancy is a bug.

---

## Page map

`Components/Pages/PermitIntel/` — the product:
`Dashboard`, `PermitSearch`, `PermitDetail`, `PermitMap`, `PermitAnalytics`,
`MyLeads`, `AlertSettings`, `ContractorSearch`, `ContractorDetail`,
`PropertyCompliance`, `Subscribe`, `RequireTier`.

`Components/Pages/Campaigns/` — `LandingA.razor`, `LandingB.razor`, `LandingC.razor`,
the three postcard landing pages. UTM shape:
`utm_source=postcard&utm_medium=direct-mail&utm_campaign=dca-nyc&utm_content=version-a|b|c`

Tier dashboards: `FreeTier`, `BasicTier`, `MidTier`, `HighTier` and the corresponding
`Vw*TierDashboards` pages bound to the SQL views.

Note `ClaudeDobFilings.razor`, `EditDobjobFiling.razor` and `HighTierDashboard.razor`
are excluded from compilation in the csproj. They are dead weight — don't edit them
expecting a change, and don't delete them without asking.

---

## Build, test, deploy

```powershell
dotnet build KonXProWebApp.sln
dotnet test KonXProWebApp.Tests                 # unit
dotnet test KonXProWebApp.Functions.Tests       # functions
dotnet test KonXProWebApp.Integration.Tests     # boots the app in-process
dotnet test KonXProWebApp.E2E.Tests             # Playwright, needs a running app
```

`Program.cs` ends with `public partial class Program { }` specifically so
`WebApplicationFactory<Program>` works in the integration tests. Don't remove it.
The app skips all startup migration and seeding when the environment is `Testing`.

### CI

Two workflows, with different jobs:

- **`build-and-test.yml`** — runs on every PR to `master` and on push to `master`.
  Sets up both the .NET 8 and .NET 9 SDKs (the solution needs both), builds the whole
  solution in Release, then runs `KonXProWebApp.Tests` and
  `KonXProWebApp.Functions.Tests`. A second job runs `KonXProWebApp.Integration.Tests`
  against a Testcontainers SQL Server; it is marked `continue-on-error` and is
  **advisory**, not a gate. Remove that line once it has proven stable — it is the
  only job that exercises application startup, so it should be a real gate before the
  startup DDL is touched.
- **`master_konxprofunctionapp.yml`** — deploys `KonXProWebApp.Functions` to the Azure
  Function App on push to `master`, via OIDC. Runs no tests. Do not fold the test gate
  into this workflow; keeping deploy and verification separate is deliberate.

`KonXProWebApp.E2E.Tests` is never run in CI — it needs a live host.

The web app itself is still not deployed by any pipeline. It goes out by Web Deploy
from a publish profile, by hand.

---

## Package versions

All package versions are centralized in `Directory.Packages.props` at the repo root
(`ManagePackageVersionsCentrally`). **Individual csproj files carry no `Version`
attribute — do not add one back.** To change a version, change it there.

Two constraints to respect:

- `Microsoft.Data.SqlClient` must stay identical in `KonXProWebApp.Functions` and
  `KonXProWebApp.Functions.Tests`. The test project referencing an older version than
  the project under test is a NuGet downgrade error (NU1605), which is how this was
  first found.
- `bunit` and `Microsoft.Playwright.Xunit` are pinned at 1.36.0 and 1.50.0. Earlier
  csproj files asked for 1.35.6 and 1.49.0, versions that do not exist on the feed, so
  NuGet was silently rounding up. Pin to versions that exist.

---

## Conventions

- Keep logic out of `.razor` files — use the `.razor.cs` code-behind, and put real
  business logic in a service. This codebase mostly follows that; match it.
- Async throughout. No `.Result`, no `.Wait()` in new code. (`Program.cs` startup
  seeding uses `.Wait()`; that's existing, don't copy it.)
- `ILogger<T>` for logging, never `Console.WriteLine`.
- Radzen components for UI — match the surrounding page rather than introducing a
  second component library.
- Stripe webhook handling must stay idempotent; the same event can arrive twice.
- Never log raw Stripe payloads — they contain PII.

## Secrets

Config keys live in `appsettings.json` / `appsettings.Staging.json`:
`ConnectionStrings:db_9f8bee_konxdevConnection` and `Smtp:*` (host konxpro.com:587).
Both files, plus `appsettings.Development.json` and
`KonXProWebApp.Functions/local.settings.json`, held real production/staging
passwords in plain text — committed to git since the initial commit — until
[PR #37](https://github.com/DrumsNY1/KonXProWebApp/pull/37). They now hold
`PLACEHOLDER`; real values belong in `dotnet user-secrets` locally (both
`KonXProWebApp.csproj` and `KonXProWebApp.Functions.csproj` have a
`UserSecretsId`) and environment variables on the server. Rotating the actual
SQL passwords is still open — see item 0.1 in `REMEDIATION.md`.

**Never put a credential in a command line, a commit, or a chat message.** Use
`dotnet user-secrets` locally and environment variables on the server. If you need a
value that isn't already in the environment, ask — don't read it out of a config file
and paste it into a command.

**`dotnet ef` reads user secrets too.** Once real credentials are in user-secrets,
any `dotnet ef migrations`/`database update` command run from this project builds
the app host the normal way and picks them up automatically — including commands
that don't look like they'd need a live connection (`migrations remove` checks the
target database's migration history). Override the connection string explicitly
(e.g. `$env:ConnectionStrings__db_9f8bee_konxdevConnection = '<connection string>'`
before the command, using single quotes — this PowerShell setup mis-parses
double-quoted strings containing parentheses, e.g. `(localdb)`) when working
against a local/disposable database, rather than relying on `appsettings.json`'s
placeholder to fail closed.

Do not run `sqlcmd`, `dotnet publish`, or anything else against production. Schema and
data changes go through the paths described in the Data section, reviewed by a human.

---

## Known issues to be aware of

- **`xunit` is behind its own transitive dependencies (NU1608).** `xunit` is pinned at
  2.5.3, but `bunit` and `Microsoft.Playwright.Xunit` pull in
  `xunit.extensibility.core` 2.8.0. The build warns on every restore. Fixing it means
  moving all four test projects to xunit 2.8.x, which risks test discovery — do it as
  its own change, not as a side effect.
- **The solution is mixed-target.** Web app and all four test projects are net9.0;
  `KonXProWebApp.Functions` is net8.0. Any pipeline or tool that builds the whole
  solution needs both SDKs installed. The deploy workflow pins `DOTNET_VERSION: '8.0'`,
  which is correct for the Functions project it builds alone.
- **Integration tests build their schema from the EF model, not from production's.**
  `SqlWebApplicationFactory` sets the environment to `Testing` (skipping the startup
  DDL) and calls `EnsureCreatedAsync()`. So those tests run against a schema that
  *includes* the unique indexes production lacks, and *excludes* the five tier views,
  which only exist in the startup DDL. Do not read a passing integration test as
  evidence that production's schema is correct.
- **`publish/`, `publish.zip` (36 MB), `logs/`, `scratch/`** are build output and noise.
  Don't read them looking for source.
- `REGRESSION_TEST_SUITE.md` is 59 KB. Open it only when working on test coverage.

---

## Current work

`REMEDIATION.md` at the repo root is the live plan: what has been fixed, what is
open, the dependency order, and the schema findings behind it. Read it before
starting anything to do with migrations, CI, packaging or the tier views.

---

## Working agreement

1. Read the `.csproj` before using a language or framework feature — this is net9.0.
2. Run `dotnet build` after writing code. If it fails, analyze and fix it.
3. Run the relevant test project before saying you're done. CI won't catch it for you.
4. Branch off `master`; don't commit directly to it.
5. For UI changes, add or update a Playwright journey in `KonXProWebApp.E2E.Tests`.
6. If a change touches the tier views or the `Program.cs` DDL block, say so explicitly
   in your summary — those affect production entitlements on the next restart.
