# KonXProWebApp — reference detail

> Reference material, not always-loaded. `AGENTS.md` carries what an agent needs on
> nearly every task and points here for the rest. Same rule applies: **if this
> disagrees with the code, the code is right — fix this file.** Read from the
> repository on Sept 20, 2026.

## Page map

`Components/Pages/PermitIntel/` — the product:
`Dashboard`, `PermitSearch`, `PermitDetail`, `PermitMap`, `PermitAnalytics`,
`MyLeads`, `AlertSettings`, `ContractorSearch`, `ContractorDetail`,
`PropertyCompliance`, `Subscribe`, `RequireTier`.

Tier dashboards: `FreeTier`, `BasicTier`, `MidTier`, `HighTier` and the corresponding
`Vw*TierDashboards` pages bound to the SQL views.

`Components/Pages/Campaigns/` — `LandingA.razor`, `LandingB.razor`, `LandingC.razor`,
the three postcard landing pages. UTM shape:
`utm_source=postcard&utm_medium=direct-mail&utm_campaign=dca-nyc&utm_content=version-a|b|c`

`Components/Pages/Subscription.razor` is at `/subscription` and is a **different
page** from `Components/Pages/PermitIntel/Subscribe.razor` at
`/permit-intel/subscribe`. Don't conflate them — some tests target each.

## Services

In `Services/`, all registered scoped in `Program.cs`:

- `PermitIntelService` (~34 KB — the core query surface)
- `ContractorIntelService`
- `ComplianceIntelService`
- `StripeService`
- `SubscriptionTierService`
- `SecurityService`
- `Db9f8beeKonxdevService` (~44 KB, scaffolded CRUD — prefer the Intel services for
  new work)

## Key tables and views

Tables: `DOBJobFilings` (the core permit table), `Subscriptions`, `SavedLeads`,
`AlertPreferences`, plus Identity tables and contractor/violation tables.

Views: `vwFreeTierDashboard`, `vwBasicTierDashboard`, `vwMidTierDashboard`,
`vwHighTierDashboard`, `vwDemoDisplay` — each exposes progressively more columns
(`InitialCost` appears only at Mid and High). View SQL lives in
`Data/TierViewDefinitions.cs`, shared with the entitlement tests in
`KonXProWebApp.Integration.Tests`.

## Seeded accounts

`identityDb.SeedTenantsAdmin()` seeds a `tenantsadmin` user plus Admin and Marketing
roles. `identityDb.SeedTierTestUsersAsync(permitDb)` seeds four confirmed accounts —
`starter_test@`, `pro_test@`, `business_test@`, `agency_test@konxpro.com` — each with
an Active `Subscriptions` row at the matching tier. There is **no Free-tier account**
seeded.

`MultiTenancyUserStore.FindByNameAsync` has an explicit bypass for `tenantsadmin` and
`*_test@konxpro.com`, which is why those accounts log in with no tenant configured.

## Ingestion

Seven Azure Functions in `KonXProWebApp.Functions/Functions/`, all timer-triggered
(`Worker.Extensions.Timer`):

`PermitIngestionFunction`, `DobNowIngestionFunction`, `DobViolationIngestionFunction`,
`HpdViolationIngestionFunction`, `ServiceRequest311IngestionFunction`,
`ContractorIngestionFunction`, `AlertDispatchFunction`.

The Functions project does **not** reference EF Core — it talks to SQL Server through
`Microsoft.Data.SqlClient` directly. Ingestion code is hand-written ADO.NET; don't
reach for a DbContext there.

Source data is NYC public record. Fields exposed to subscribers include owner name,
address, permit type, filing date and borough. The DOB data disclaimer in the Privacy
Notice covers this — it is public record and not guaranteed accurate.

## Environments and schema history

Staging's permit schema was rebuilt on 2026-09-12. **Production has not been
rebuilt** — it was scoped on 2026-09-19 and not executed, so the two still differ.
Do not treat either as a rehearsal for the other.

Both DbContext registrations pin
`MigrationsHistoryTable("__EFMigrationsHistory", "dbo")`, because each SQL login's
default schema is its own name and EF would otherwise put the history table in
`konx_admin` (production) or `konx_staging_admin` (staging).

`REMEDIATION.md` is the live record of what is fixed, what is open, and the schema
findings behind it. It supersedes anything here on status.

## Secrets — history and the `dotnet ef` trap

`appsettings.json`, `appsettings.Staging.json`, `appsettings.Development.json` and
`KonXProWebApp.Functions/local.settings.json` held real production and staging
passwords in plain text, committed to git since the initial commit, until
[PR #37](https://github.com/DrumsNY1/KonXProWebApp/pull/37). They now hold
`PLACEHOLDER`. Rotating the actual SQL passwords is still open — `REMEDIATION.md`
item 0.1.

**`dotnet ef` reads user secrets too.** Once real credentials are in user-secrets,
any `dotnet ef migrations` / `database update` command run from this project builds
the app host the normal way and picks them up — including commands that don't look
like they need a live connection (`migrations remove` checks the target database's
migration history). Override the connection string explicitly when working against a
local or disposable database, rather than relying on `appsettings.json`'s placeholder
to fail closed:

```powershell
$env:ConnectionStrings__db_9f8bee_konxdevConnection = '<connection string>'
```

Use single quotes — this PowerShell setup mis-parses double-quoted strings containing
parentheses, e.g. `(localdb)`.

## Package version rationale

- `bunit` and `Microsoft.Playwright.Xunit` are pinned at 1.36.0 and 1.50.0 — the
  versions actually being restored. The csproj files previously asked for 1.35.6 and
  1.49.0, which do not exist on the feed, so NuGet was silently rounding up.
- `xunit` / `xunit.runner.visualstudio` were bumped 2.5.3 → 2.8.0: xunit.core 2.5.3
  requires xunit.extensibility.core exactly 2.5.3, but bunit 1.36.0 and
  Playwright.Xunit 1.50.0 both pull in 2.8.0 transitively, which was an NU1608
  conflict on every restore. 2.8.0 is the version NuGet was already resolving.
- Versions are centralized, so a change affects all four test projects — verify test
  discovery in each, not just that the build succeeds.
