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

## Staging deployment runbook

Staging is deployed by `.github/workflows/deploy-staging.yml`. It publishes
`KonXProWebApp.csproj` (Release, net10.0) on a Windows runner and runs
`.github/workflows/scripts/set-webconfig-env.ps1` to stamp the published
`web.config` with `ASPNETCORE_ENVIRONMENT=Staging`, the staging connection string
and stdout logging. It then syncs to the Plesk site over Web Deploy (port 8172) and
smoke-checks `/` and `/login`. Production is **not** covered: it is still a manual
Web Deploy by a human.

The Web Deploy endpoint is `https://plesk9100.is.cc:8172/msdeploy.axd?site=staging.konxpro.com`.
Note that it uses the server's hostname, not the site's. Port 8172 serves a single
certificate for the whole server (CN `plesk9100.is.cc`), so connecting as
`staging.konxpro.com` fails with `ERROR_CERTIFICATE_VALIDATION_FAILED`. Do not work
around that with `-allowUntrusted`, which would send the deploy password to any server
that answers. Visual Studio profiles usually hide this with `AllowUntrustedCertificate`.

**One-time setup (a human does this, never an agent):**

1. On the Plesk server, check that the **.NET 10 Hosting Bundle** is installed and
   that Web Deploy on port 8172 accepts connections from outside (GitHub-hosted
   runners use changing Azure IPs).
2. Create a dedicated Plesk Web Deploy user scoped to `staging.konxpro.com`.
3. Create the GitHub environment `staging` with these secrets:
   - `STAGING_WEBDEPLOY_USERNAME`
   - `STAGING_WEBDEPLOY_PASSWORD`
   - `STAGING_DB_CONNECTION_STRING` (the full ADO.NET string for `priority_ks`)
4. Back up the current server `web.config` before the first run. The deploy
   replaces it, and the hand-added connection string moves into the secret.

**Deploying:**

- Manual: Actions → *Deploy to staging* → Run workflow (choose a branch or ref).
- Automatic: set the repository variable `STAGING_AUTO_DEPLOY=true`. Then every
  successful *Build and test* run on `master` deploys that exact commit.
- Rollback: re-run the workflow on an older commit, or dispatch it on a tag.

**Diagnosing:**

- `AppOffline` is used, so the site shows the offline page during sync.
- The ASP.NET Core module writes stdout to `logs\stdout*` under the site root.
  `Program.cs` swallows migration and seeding errors, so a green smoke check does
  **not** prove migrations ran. Check the log.
- `DoNotDeleteRule` is on, so files removed from the app stay on the server until
  someone deletes them by hand.

Local `Properties/PublishProfiles/*.pubxml` files are gitignored and may still pin
`net9.0`. Delete the staging profiles once the pipeline is trusted. Keep or
update the production profile until production gets its own pipeline.

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

- `bunit` is pinned at 1.36.0 — the version actually being restored. The csproj files
  previously asked for 1.35.6, which does not exist on the feed, so NuGet was
  silently rounding up. (Playwright had the same history: 1.49.0 requested, 1.50.0
  restored.)
- `Microsoft.Playwright` / `Microsoft.Playwright.Xunit` are pinned at **1.64.1**
  (bumped from 1.50.0 on 2026-11-11). GitHub's `ubuntu-latest` runners moved to
  Ubuntu 26.04, and Playwright before 1.61.0 refuses to install Chromium there
  (`Playwright does not support chromium on ubuntu26.04-x64`), which broke the E2E
  job at its browser-install step.
- `xunit` / `xunit.runner.visualstudio` were bumped 2.5.3 → 2.8.0: xunit.core 2.5.3
  requires xunit.extensibility.core exactly 2.5.3, but bunit 1.36.0 and
  Playwright.Xunit (1.50.0 and still 1.64.1) pull in 2.8.0 transitively, which was an
  NU1608 conflict on every restore. 2.8.0 is the version NuGet was already resolving.
- Versions are centralized, so a change affects all four test projects — verify test
  discovery in each, not just that the build succeeds.
