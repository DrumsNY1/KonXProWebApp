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
| Component library | **Radzen.Blazor** (floating version — see Known issues) |
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

### How the schema is actually managed — read this before touching the database

`Program.cs` does three different things at startup, and only the first is migrations:

1. `identityDb.Database.Migrate()` — proper EF migrations for Identity.
2. `permitDb.Database.EnsureCreated()` — **not** migrations.
3. Raw `ExecuteSqlRaw` DDL that creates `Subscriptions`, `SavedLeads` and
   `AlertPreferences` if absent, then `CREATE OR ALTER VIEW` for five tier views.

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

### CI reality

`.github/workflows/master_konxprofunctionapp.yml` is the only workflow. On push to
`master` it publishes **`KonXProWebApp.Functions` only** and deploys it to the Azure
Function App via OIDC. It **runs no tests**.

So: **nothing gates a merge.** Tests only run when you run them. Run them.

The web app itself is not in any pipeline — it goes out by Web Deploy from a publish
profile, by hand.

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

**Never put a credential in a command line, a commit, or a chat message.** Use
`dotnet user-secrets` locally and environment variables on the server. If you need a
value that isn't already in the environment, ask — don't read it out of a config file
and paste it into a command.

Do not run `sqlcmd`, `dotnet publish`, or anything else against production. Schema and
data changes go through the paths described in the Data section, reviewed by a human.

---

## Known issues to be aware of

- **Floating package versions, in both projects.** `Radzen.Blazor Version="*"` and
  `9.*-*` on the EF Core and ASP.NET Core packages in the web app; `2.*`, `3.*`, `4.*`,
  `6.*`, `8.*` across the Functions packages. A restore can change dependencies between
  runs. If a build breaks for no reason you can trace to a code change, suspect this
  first.
- **The solution is mixed-target.** Web app is net9.0, Functions is net8.0, and CI pins
  `DOTNET_VERSION: '8.0'` — correct for the Functions project it builds, but the
  pipeline cannot build the web app as written. Anyone adding the web app to CI must
  bump that first.
- **`publish/`, `publish.zip` (36 MB), `logs/`, `scratch/`** are build output and noise.
  Don't read them looking for source.
- `REGRESSION_TEST_SUITE.md` is 59 KB. Open it only when working on test coverage.

---

## Working agreement

1. Read the `.csproj` before using a language or framework feature — this is net9.0.
2. Run `dotnet build` after writing code. If it fails, analyze and fix it.
3. Run the relevant test project before saying you're done. CI won't catch it for you.
4. Branch off `master`; don't commit directly to it.
5. For UI changes, add or update a Playwright journey in `KonXProWebApp.E2E.Tests`.
6. If a change touches the tier views or the `Program.cs` DDL block, say so explicitly
   in your summary — those affect production entitlements on the next restart.
