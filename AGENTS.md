# KonXProWebApp — repo guide for coding agents

> This is the canonical, always-loaded instruction file for this repository. Every
> agent reads it: Antigravity/Gemini load `AGENTS.md` by walking up to the repo
> root, and Claude Code loads it through the `@AGENTS.md` import in `CLAUDE.md`.
> There is no second copy of these facts anywhere — edit them here.
>
> Every technical claim below was re-read from this repository on Sept 20, 2026.
> **If something here disagrees with the code, the code is right — fix this file.**

## What it is

B2B SaaS delivering NYC Department of Buildings permit intelligence to contractors.
Subscribers get alerts when permits are filed in their territory so they can reach
homeowners before competitors.

**Live domain:** konxpro.com
**Market:** contractors across the five NYC boroughs
**Billing:** Stripe subscriptions. `StripeService.GetTierInfo()` returns **six**
tiers — Starter, Pro, Business, Agency, ComplianceAlerts, LandlordCompliance —
but only the first four have authorization policies. Don't conflate the two.

---

## Tech stack

This file is authoritative on stack. The global agent instructions are
stack-agnostic and defer here, so do not infer the stack from anything else.

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
| Web app deploy | **Web Deploy** from a `.pubxml` publish profile, run by a human |
| Functions deploy | GitHub Actions → Azure Function App `KonXProFunctionApp` |
| Default branch | **`master`** |

**Target frameworks, explicitly:** the web app and all four test projects are
**net9.0**; `KonXProWebApp.Functions` alone is **net8.0**. Any tool that builds the
whole solution needs both SDKs.

There is **no Blazor WebAssembly** in this solution — it is Blazor **Server**, and
an earlier version of this file wrongly claimed WebAssembly. There is also **no
PostgreSQL, no PostGIS, no Dapper, no Hangfire and no NetTopologySuite**. If a task
seems to call for one of those, stop and ask — an earlier version of this file
claimed all of them and it was wrong.

---

## Solution layout

Six projects, all rooted at the repository root. There is **no `src/` folder**.

```
KonXProWebApp.sln
├─ KonXProWebApp.csproj          ← the web app, at the repo root
│  ├─ Components/Pages/          ← Razor pages
│  ├─ Controllers/               ← MVC + OData controllers
│  ├─ Services/                  ← business logic
│  ├─ Data/                      ← DbContexts + Migrations
│  ├─ Models/
│  ├─ Authorization/             ← SubscriptionRequirement + handler
│  └─ Filters/
├─ KonXProWebApp.Functions/      ← Azure Functions (ingestion + alerts)
├─ KonXProWebApp.Tests/          ← unit + bUnit component tests
├─ KonXProWebApp.Functions.Tests/
├─ KonXProWebApp.Integration.Tests/  ← WebApplicationFactory<Program>
└─ KonXProWebApp.E2E.Tests/      ← Playwright (PlaywrightFixture, Journeys/)
```

Because the web app csproj sits at the root, it explicitly `<Compile Remove>`s each
sibling project folder **by name**. **If you add a new top-level folder or project,
add matching Remove items** or the root project will compile it into the web app.

Page-by-page map, the campaign landing pages and the two distinct subscribe
pages: see `docs/repo-reference.md`.

---

## Data

### Two DbContexts, one database

Both point at the same connection string, `db_9f8bee_konxdevConnection`.

- **`ApplicationIdentityDbContext`** — users, roles, tenants.
- **`db_9f8bee_konxdevContext`** — permit and contractor data, originally scaffolded
  from an existing database. Split across `Db9f8beeKonxdevContext.cs` and
  `Db9f8beeKonxdevContext.PermitIntel.cs`.

Both registrations in `Program.cs` pin
`MigrationsHistoryTable("__EFMigrationsHistory", "dbo")`, because each SQL login's
default schema is its own name and EF would otherwise put the history table in
`konx_admin` / `konx_staging_admin`.

### What actually happens at startup — read this before touching the database

`Program.cs` does this on boot, skipped entirely when the environment is `Testing`:

1. `identityDb.Database.Migrate()` — EF migrations for Identity.
2. `identityDb.SeedTenantsAdmin()` — seeds a `tenantsadmin` user plus Admin and
   Marketing roles.
3. `permitDb.Database.Migrate()` — **EF migrations**, as of 2026-09-19. Both
   contexts have real migrations in `Data/Migrations/`, distinguished by their
   `[DbContext(typeof(...))]` attribute.
4. `identityDb.SeedTierTestUsersAsync(permitDb)` — seeds four confirmed accounts
   (`starter_test@` / `pro_test@` / `business_test@` / `agency_test@konxpro.com`),
   each with an Active `Subscriptions` row.
5. Raw `ExecuteSqlRaw` — **only** `CREATE OR ALTER VIEW` for the five tier views,
   whose SQL lives in `Data/TierViewDefinitions.cs` (shared with the entitlement
   tests in `KonXProWebApp.Integration.Tests`, not inlined in `Program.cs`).

Two consequences that bite:

- **The whole block is wrapped in a `try`/`catch` that only logs.** If migration or
  seeding fails, the app still starts and serves pages. A healthy-looking process is
  not evidence the schema is correct — check the startup log.
- **The view DDL runs against whatever database the app boots against, including
  production**, the moment it restarts. Changing a tier view changes entitlements.

Earlier versions of this file said the permit context used `EnsureCreated()` plus
raw DDL that created `Subscriptions`, `SavedLeads` and `AlertPreferences`. That was
true before 2026-09-19 and is **no longer true**: those tables now come from
migrations, and the raw DDL creates views only.

Do not "clean this up" as a side effect of another task.

### Key tables and views

`DOBJobFilings` is the core permit table, alongside `Subscriptions`, `SavedLeads`,
`AlertPreferences`, Identity tables and contractor/violation tables. Five tier views
(`vwFreeTierDashboard` … `vwDemoDisplay`) expose progressively more columns and are
**an enforcement point for what a subscriber can see — changing a view changes
entitlements.** Column-level detail: `docs/repo-reference.md`.

### Production and staging

Staging's permit schema was rebuilt 2026-09-12. **Production has not been rebuilt**
— scoped 2026-09-19, not executed — so the two still differ. Do not treat either as
a rehearsal for the other. `REMEDIATION.md` is the live record.

### Ingestion

Seven timer-triggered Azure Functions in `KonXProWebApp.Functions/Functions/`. The
Functions project does **not** reference EF Core — it uses `Microsoft.Data.SqlClient`
directly, and ingestion code is hand-written ADO.NET. Don't reach for a DbContext
there. Function names and data-source notes: `docs/repo-reference.md`.

---

## Services and authorization

`Services/`, all registered scoped. `PermitIntelService` is the core query surface;
prefer the Intel services over the large scaffolded `Db9f8beeKonxdevService` for new
work. Full list: `docs/repo-reference.md`.

Four policies in `Program.cs`: `RequiresStarter`, `RequiresPro`, `RequiresBusiness`,
`RequiresAgency`, each adding a `SubscriptionRequirement` evaluated by
`Authorization/SubscriptionAuthorizationHandler`.
`Components/Pages/PermitIntel/RequireTier.razor` is the component-level gate.

Identity is **multi-tenant**: `ApplicationTenant`, with `MultiTenancyUserStore`
registered as `IUserStore<ApplicationUser>`. A user lookup that ignores tenancy is a
bug. Seeded accounts and the tenant-bypass rule: `docs/repo-reference.md`.

---

## Build and test

```powershell
dotnet build KonXProWebApp.sln
dotnet test KonXProWebApp.Tests                 # unit + bUnit component
dotnet test KonXProWebApp.Functions.Tests
dotnet test KonXProWebApp.Integration.Tests     # boots the app in-process
dotnet test KonXProWebApp.E2E.Tests             # Playwright, needs a running app
```

`Program.cs` ends with `public partial class Program { }` specifically so
`WebApplicationFactory<Program>` works in the integration tests. Don't remove it.

### Test categories — new tests must comply or the build fails

Every test class carries exactly one `[Trait("Category", "...")]`:

| Category | Meaning |
|---|---|
| `Unit` | in-process, mocks / EF InMemory, no external anything |
| `Component` | bUnit render tests (derive from `TestContext`) |
| `Integration` | needs a real SQL Server (Testcontainers or a service container) |
| `E2E` | drives a real browser against a running host |

Classes that drive Playwright additionally carry one `[Trait("E2E", "...")]` scope:
`Anonymous` (no login or seeded data), `SeededUser` (needs the startup-seeded tier
accounts), `ExternalCredentials` (needs accounts nothing in this repo creates) or
`Manual` (needs a host started by hand).

`Conventions/TestCategoryConventionTests.cs` — one copy per test project — enforces
this by reflection and **fails the build** when a test class has no Category, has
more than one, uses an unknown value, or drives Playwright without being
`Category=E2E` with a valid scope. It also fails if it finds no test classes at all,
so it cannot pass vacuously. It is duplicated per project rather than shared because
a new top-level folder would be compiled into the web app (see Solution layout).

CI selects tests by these traits, not by class name, so **an unlabelled class is a
build failure rather than a test that silently stops running**.

### CI

- **`build-and-test.yml`** — PR to `master`, push to `master`, `workflow_dispatch`.
  Three jobs, none of which deploy anything:
  - *Build + unit tests* — builds the solution in Release, then runs
    `Category=Unit|Category=Component` in `KonXProWebApp.Tests` and
    `Category=Unit|Category=Integration` in `KonXProWebApp.Functions.Tests`. That
    second filter is deliberate: `IngestionServiceSqlTests` lives in the Functions
    test project but uses Testcontainers.
  - *Integration tests* — `Category=Integration`, **advisory**
    (`continue-on-error`) until it has a track record. It is the only job that
    exercises application startup, so it should be a real gate before the startup
    DDL is touched.
  - *E2E browser tests* — starts the web app **on the runner** against a throwaway
    SQL Server service container, waits for it to answer, and runs the E2E scopes
    the runner can satisfy (`E2E!=SeededUser&E2E!=ExternalCredentials&E2E!=Manual`),
    which today means the anonymous journey. Advisory until **2026-10-05**; grep
    `TODO(2026-10-05)`. No Azure credentials, no OIDC, no publish.
- **`master_konxprofunctionapp.yml`** — deploys `KonXProWebApp.Functions` on push to
  `master` via OIDC. Runs no tests. Keeping deploy and verification separate is
  deliberate; do not fold the test gate into it.

The web app itself is not deployed by any pipeline.

---

## Package versions

All versions are centralized in `Directory.Packages.props`
(`ManagePackageVersionsCentrally`). **Individual csproj files carry no `Version`
attribute — do not add one back.**

- `Microsoft.Data.SqlClient` must stay identical in `KonXProWebApp.Functions` and
  `KonXProWebApp.Functions.Tests`. A test project referencing an older version than
  the project under test is a NuGet downgrade error (NU1605).
- `bunit` and `Microsoft.Playwright.Xunit` are pinned at 1.36.0 and 1.50.0, and
  `xunit` at 2.8.0. Earlier csproj files asked for 1.35.6 / 1.49.0, versions that
  **do not exist on the feed**, so NuGet was silently rounding up. Pin to versions
  that exist. Rationale: `docs/repo-reference.md`.

---

## Conventions

- Keep logic out of `.razor` files — use the `.razor.cs` code-behind, and put real
  business logic in a service.
- Async throughout. No `.Result`, no `.Wait()` in new code. (`Program.cs` startup
  seeding uses `.Wait()`; that's existing, don't copy it.)
- `ILogger<T>` for logging, never `Console.WriteLine`.
- Radzen components for UI — match the surrounding page rather than introducing a
  second component library.
- Stripe webhook handling must stay idempotent; the same event can arrive twice.
- Never log raw Stripe payloads — they contain PII.

---

## Secrets

Config keys live in `appsettings.json` / `appsettings.Staging.json`:
`ConnectionStrings:db_9f8bee_konxdevConnection` and `Smtp:*`. They hold
`PLACEHOLDER` — real values belong in `dotnet user-secrets` locally (both
`KonXProWebApp.csproj` and `KonXProWebApp.Functions.csproj` have a `UserSecretsId`)
and environment variables on the server.

**Never put a credential in a command line, a commit, or a chat message.** If you
need a value that isn't already in the environment, ask — don't read it out of a
config file and paste it into a command.

**`dotnet ef` reads user secrets too**, including commands that don't look like they
need a live connection. Override the connection string explicitly when working
against a disposable database — see `docs/repo-reference.md` for the exact form and
a PowerShell quoting trap.

---

## Deployment — CI owns it

This section adds repo-specific detail to the global rule that agents do not deploy;
it does not soften it.

- **Functions:** deployed by `master_konxprofunctionapp.yml` on push to `master`.
  To ship a change, merge it to `master`.
- **Web app:** no pipeline exists. It goes out by Web Deploy from a publish profile,
  by hand, by a human. **That missing pipeline is a gap to report, not one to work
  around** — do not substitute a manual publish for it.
- **Never run** `func azure functionapp publish`, `dotnet publish` to a live target,
  `sqlcmd`, or anything else against production or staging. Schema and data changes
  go through the paths in the Data section, reviewed by a human.

A manual `func azure functionapp publish KonXProFunctionApp --dotnet-isolated` exists
as a break-glass path for humans when CI is unavailable. An agent must **never** run
it on its own initiative — not to verify a build, not to speed up a merge, not as a
fallback when CI fails. Only when Reggie explicitly asks, in that moment, for a
manual publish. A request to fix, build or test Functions code is not such a request.

### Tool paths (D: drive)

Development tools are installed on the **D: drive**. Prepend to `$env:PATH` before
use:

| Tool | Path |
|---|---|
| **Git** | `D:\Program Files\Git\cmd` |
| **GitHub CLI (gh)** | `D:\Program Files\GitHub CLI` |
| **Azure CLI (az)** | `D:\Program Files\Microsoft SDKs\Azure\CLI2\wbin` |
| **Azure Functions Core Tools (func)** | `D:\Program Files\Microsoft\Azure Functions Core Tools` |

```powershell
$env:PATH = "D:\Program Files\Git\cmd;D:\Program Files\GitHub CLI;D:\Program Files\Microsoft SDKs\Azure\CLI2\wbin;D:\Program Files\Microsoft\Azure Functions Core Tools;" + $env:PATH
```

---

## Reference documents — read when the task calls for it

These are **not** loaded automatically. Open them when the work touches their area.

- **`docs/design-language.md`** — the Impeccable Design Language: color, type scale,
  spacing, component specs, motion, responsive rules. **Read before any UI or visual
  work**: Razor components, CSS, layout, the campaign landing pages, or anything a
  reviewer judges by looking at it.
- **`docs/repo-reference.md`** — page map, services, key tables and views, seeded
  accounts, ingestion detail, environment/schema history, secrets history and the
  package-version rationale. Read when a task needs specifics this file summarizes.
- **`REMEDIATION.md`** — the live remediation plan: what's fixed, what's open, the
  dependency order, and the schema findings behind it. **Read before starting
  anything to do with migrations, CI, packaging or the tier views.**
- **`REGRESSION_TEST_SUITE.md`** — 59 KB. Open only when working on test coverage.

---

## Known traps

- **Mixed-target solution.** Both .NET 8 and .NET 9 SDKs are required to build it.
- **Integration tests build their schema from the EF model, not production's.**
  `SqlWebApplicationFactory` sets the environment to `Testing` (skipping all startup
  migration, seeding and view DDL) and calls `EnsureCreatedAsync()`. Those tests run
  against a schema that *excludes* the five tier views. A passing integration test is
  not evidence that production's schema is correct.
- **`publish/`, `publish.zip`, `logs/`, `scratch/`** are build output and noise.
  Don't read them looking for source.

---

## Working agreement

1. Read the `.csproj` before using a language or framework feature — this is net9.0.
2. Run `dotnet build` after writing code. If it fails, analyze and fix it.
3. Run the relevant test project before saying you're done. CI won't catch it for you.
4. Branch off `master`; don't commit directly to it.
5. New test classes need a `Category` trait, or the convention guard fails the build.
6. For UI changes, add or update a Playwright journey in `KonXProWebApp.E2E.Tests`.
7. If a change touches the tier views or the `Program.cs` startup block, say so
   explicitly in your summary — those affect production entitlements on next restart.
