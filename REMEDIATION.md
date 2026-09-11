# KonXProWebApp — remediation status and plan

Living document. Last updated 2026-09-11.

Phase 0 is complete. Phase 1 is complete. Phases 2–4 are open, and the plan below
has been **revised** from its original form because Phase 1 disproved two of its
assumptions. Read the Findings section before starting any Phase 2 item.

---

## Findings from item 1.1 (schema ground truth)

Captured 2026-09-10 from `priority_konx` (production) and `priority_ks` (staging),
both SQL Server 2022 Express on `plesk9100.is.cc`. Read-only capture; the tooling
lives in `_tools/schema-truth/` (gitignored) and can be re-run with `RUN.cmd`.

### 1. The migration history exists — it is just not in `dbo`

| | production | staging |
|---|---|---|
| schema holding `__EFMigrationsHistory` | `konx_admin` | `konx_staging_admin` |
| migrations recorded | **6** | **1** |

Each login's default schema is its own name, so EF creates the history table in
the *connecting login's* schema rather than `dbo`. Production's 6 rows match the
6 files in `Data/Migrations/` exactly, so **production is fully migrated and up to
date**. Earlier belief that migrations had never been applied was wrong; a
`dbo.__EFMigrationsHistory` query simply could not see the table.

**This is fragile and worth its own work item.** The history follows whichever
login connects. Point the app at a different SQL user and EF sees an empty
history and tries to replay every migration from the beginning, against a
database that already has the objects. Consider setting an explicit migrations
history table with a fixed schema:

```csharp
options.UseSqlServer(cs, sql =>
    sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo"));
```

That is a migration of the history itself and must be done deliberately — see
item 2.0 below.

### 2. Production and staging were built by different mechanisms

Primary key names give it away. EF generates `PK_<Table>`; SQL Server
auto-generates `PK__<8 chars>__<hash>` for an inline `PRIMARY KEY`, which is what
the raw DDL in `Program.cs` writes.

| table | production | staging |
|---|---|---|
| `Subscriptions` | `PK_Subscriptions` (EF) | `PK__Subscrip__3214EC07…` (raw DDL) |
| `SavedLeads` | `PK_SavedLeads` (EF) | `PK__SavedLea__3214EC07…` (raw DDL) |
| `AlertPreferences` | `PK_AlertPreferences` (EF) | `PK__AlertPre__3214EC07…` (raw DDL) |
| `IngestionLogs` | `PK_IngestionLogs` (EF) | **heap — no key at all** |
| `DOBJobFilings` | `PK_DOBJobFilings` (EF) | `PK_DOBJobFilings` (EF) |

Consequently:

| index the EF model declares | production | staging |
|---|---|---|
| `IX_Subscriptions_StripeSubscriptionId` (UNIQUE, filtered `IS NOT NULL`) | PRESENT | **MISSING** |
| `IX_SavedLeads_UserId_DobjobFilingId` (UNIQUE, 2 columns) | PRESENT | **MISSING** |

**Staging is not a rehearsal environment for production.** The original plan said
to apply each schema change to staging, verify, then repeat on production. That
is void — the two schemas are structurally different, so a change verified on
staging proves nothing about production and would give false confidence right
before touching a live database. Verify against a throwaway container built the
same way production was, or rebuild staging from production first.

### 3. No duplicate data anywhere

- duplicate `StripeSubscriptionId` groups: **0** in both
- duplicate `SavedLeads (UserId, DobjobFilingId)` groups: **0** in both
- `Subscriptions` rows with NULL `StripeSubscriptionId`: **0** in both

| table | prod rows | staging rows |
|---|---|---|
| `DOBJobFilings` | 12,033 | 12,033 |
| `IngestionLogs` | 187 | 118 |
| `Subscriptions` | 4 | 4 |
| `AlertPreferences` | 2 | 0 |
| `SavedLeads` | 0 | 3 |

There is no billing decision to make and no data repair to do. The unique-index
item shrinks from "may become its own project" to a few minutes on staging only.

### 4. The entitlement boundary is intact, and there are two of them

Identical in both environments and matching `Program.cs`:

| view | columns | distinguishing column |
|---|---|---|
| `vwFreeTierDashboard` | 7 | — |
| `vwBasicTierDashboard` | 8 | adds `HouseNum` |
| `vwMidTierDashboard` | 9 | adds `EstimatedCost` (money) |
| `vwHighTierDashboard` | 9 | `EstimatedCost` |

Free/Basic is separated by `HouseNum`; Mid/High by `EstimatedCost`. **A test must
assert both boundaries, not just the cost column.** View `modify_date` is recent
in both, consistent with `CREATE OR ALTER VIEW` re-running on every app restart.

---

## Completed

**0.2 — package versions pinned.** `Directory.Packages.props` at the repo root,
central package management, no `Version` attribute in any csproj. Pinned to the
versions that were already resolving; nothing was upgraded except
`Microsoft.Data.SqlClient` in `KonXProWebApp.Functions.Tests`, which moved from
6.0.1 to 6.1.6 because the old value was a NuGet downgrade error (NU1605) against
the project under test.

**0.3 — CI test gate.** `.github/workflows/build-and-test.yml` runs on every PR
to `master` and on push. Builds the solution with both .NET 8 and .NET 9 SDKs and
runs `KonXProWebApp.Tests` and `KonXProWebApp.Functions.Tests`. A second job runs
`KonXProWebApp.Integration.Tests` against a Testcontainers SQL Server, currently
`continue-on-error: true`.

**1.1 — ground truth.** See Findings above.

---

## Open

### 0.1 — Rotate exposed credentials · do first, blocks nothing

Production and staging SQL passwords and the Web Deploy password are in plaintext
in `~/.gemini/config/config.json`, saved as part of approved command strings.
Rotate all three. Move local development to `dotnet user-secrets`, use
`SQLCMDPASSWORD` for ad-hoc queries, put the deploy password in GitHub secrets.
Then delete the `allow` array from that config file.

This matters more now that an agent with a shell runs on this machine.

### 2.0 — Pin the migrations history table · NEW, from Finding 1

Configure `MigrationsHistoryTable("__EFMigrationsHistory", "dbo")` and move the
existing rows from `konx_admin.__EFMigrationsHistory` to `dbo.` in each
environment. Until this is done, changing the SQL login the app connects with
will make EF replay every migration against a populated database.

Do this before 2.1 and 2.2 — both depend on EF reading the correct history.

### 2.1 — Defuse the HpdViolations drift · blocked by 2.0

`HpdViolation` is a `DbSet` in `Db9f8beeKonxdevContext.PermitIntel.cs` but is
absent from `db_9f8bee_konxdevContextModelSnapshot.cs`. The next
`dotnet ef migrations add` will therefore generate a `CreateTable` for
`HpdViolations`, which fails on apply where the table exists.

Done when `dotnet ef migrations add Probe --context db_9f8bee_konxdevContext`
produces an **empty** migration. Delete the probe afterwards.

### 2.2 — Reconcile the four permit tables with migration history · revised

Originally "write a baseline migration for four phantom tables". Finding 1 changes
this: production's tables are EF-shaped and its history is complete, so the goal
is narrower — make sure a **fresh** database built by `dotnet ef database update`
alone produces the same schema production has, without `Program.cs` creating
anything.

Note that no migration in `Data/Migrations/` contains a `CreateTable` for
`Subscriptions`, `SavedLeads`, `AlertPreferences` or `IngestionLogs`, yet they
exist in production with EF-generated names and every index the model declares,
and they appear in the model snapshot. `20260715003319_ScaffoldPendingChanges`
has an empty `Up()` and `Down()`. Establish how they got there before writing
anything — the answer determines whether a guarded baseline migration is needed
or whether the snapshot simply needs to agree with reality.

### 2.3 — Add the two missing unique indexes to staging · small

Production already has both. Staging has neither and no conflicting data. Add
them, or rebuild staging from production, which also fixes the heap
`IngestionLogs` and the raw-DDL key names.

### 2.4 — Retire the startup DDL · blocked by 2.2, 2.3, 3.1

Remove `EnsureCreated()`, the `ExecuteSqlRaw` table DDL and the view loop from
`Program.cs`; replace with `permitDb.Database.Migrate()`. Keep the
`IsEnvironment("Testing")` guard — the integration tests depend on startup doing
nothing. Consider moving migration out of app startup entirely into a deploy
step; an app that migrates on boot races itself when two instances start.

### 3.1 — Test what each tier view exposes · gates 2.4

`KonXProWebApp.Integration.Tests` already references `Testcontainers.MsSql`.
Assert the exact column set of all four tier views, table-driven, one row per
tier. Must fail if `EstimatedCost` appears in Free or Basic, **and** if
`HouseNum` appears in Free.

Note the trap: `SqlWebApplicationFactory` sets the environment to `Testing`
(skipping the startup DDL) and calls `EnsureCreatedAsync()`, so the views do not
exist in a test container today. The test needs the views applied from migrations
first, which couples 3.1 to 3.2 more tightly than the original plan assumed.

### 3.2 — Move the five views into migrations · blocked by 3.1, 2.4

`CREATE OR ALTER VIEW` is already idempotent, so each view can move into its own
migration nearly as-is, making a future change a reviewable diff.

### 4.1 — Build the web app in CI · decision needed

Extend the PR workflow to build the web app. Whether to automate the Plesk
Web Deploy is a separate decision with a rollback question attached.

### 4.2 — Remove excluded dead files

`ClaudeDobFilings.razor`, `EditDobjobFiling.razor` and `HighTierDashboard.razor`
are excluded from compilation in the csproj but read as live code.

### 4.3 — xunit version alignment

`xunit` is pinned at 2.5.3 while `bunit` and `Microsoft.Playwright.Xunit` pull in
`xunit.extensibility.core` 2.8.0 (NU1608 on every build). Moving all four test
projects to xunit 2.8.x risks test discovery, so do it on its own.

---

## Revised critical path

```
0.1 (independent)
2.0 -> 2.1 -> 2.2 -> 3.1 -> 3.2 -> 2.4
              2.3 (independent, small)
```

`3.1` sits before `2.4` deliberately: the entitlement test must exist before the
code that creates the views is deleted, because that deletion is the moment you
find out whether the database's views match the source.
