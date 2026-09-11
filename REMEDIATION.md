# KonXProWebApp — remediation status and plan

Living document. Last updated 2026-09-11.

Phase 0 is complete. Phase 1 is complete. Items 2.0, 2.1, 2.2, and 3.1 are
done in code (see "Rebuild plan" below) but **not yet applied to any real
database**. Phases 2–4 have been **revised twice** from their original form:
once because Phase 1 disproved two of its assumptions, and again because
investigating 2.2 found the first revision was itself wrong about how
production's tables got their shape. Read the Findings section and the
Rebuild plan before starting anything further.

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

**2.0 — migrations history table.** Superseded by the decision to rebuild both
databases from a clean migration set rather than patch them in place (see
"Rebuild plan" below) — a freshly migrated database gets `dbo.__EFMigrationsHistory`
from the start, so the fragile in-place move is no longer needed. Both
`ApplicationIdentityDbContext` and `db_9f8bee_konxdevContext` now pin
`MigrationsHistoryTable("__EFMigrationsHistory", "dbo")` explicitly in
`Program.cs` regardless, so a future change of SQL login can't reintroduce
Finding 1's fragility.

**2.1 — HpdViolations drift.** This item's premise was already stale by the
time it was investigated: `HpdViolation` is present and correctly mapped in
`db_9f8bee_konxdevContextModelSnapshot.cs`. Confirmed clean by regenerating
the baseline migration for item 2.2 below — it produced no unexpected
`CreateTable` for `HpdViolations`.

**2.2 — reconcile the four permit tables with migration history.** The
original finding was wrong: `Data/Migrations/20260712141006_AddServiceRequests311.cs`
*did* contain `CreateTable` for `Subscriptions`, `SavedLeads`, `AlertPreferences`,
`IngestionLogs`, and — critically — the five `vw*TierDashboard` names, mapped as
physical tables with `INSTEAD OF`-trigger annotations. That's a real, latent bug:
the current model still mapped those five views as tables (via a stray
`[Table(...)]` attribute on each POCO, left over from an earlier design), while
`Program.cs` has managed them as real SQL views via `CREATE OR ALTER VIEW` for
some time. A fresh `dotnet ef database update` would `CreateTable` those names,
which then collides with the startup view DDL ("not a view") and breaks app
startup. Someone worked around this on production by manually dropping the
table objects outside of EF's knowledge — which is why schema-truth capture
sees real views today despite the migration history saying otherwise, and
explains Finding 2's EF-shaped table names (they came from this migration
being genuinely applied, not from `EnsureCreated()` as originally guessed).

Fixed by removing the `[Table(...)]` attribute from each of the five view POCOs
(`Models/Db9f8beeKonxdev/Vw*.cs`) and mapping them via `ToView()` in
`db_9f8bee_konxdevContext.OnModelCreating` instead, which correctly excludes
them from migrations. The five original migrations for `db_9f8bee_konxdevContext`
were squashed into one `InitialCreate` baseline generated from the corrected
model — verified against a disposable LocalDB database: the new migration
creates 13 tables and zero `vw*` objects, and `Program.cs`'s exact
`CREATE OR ALTER VIEW` statements then succeed, twice in a row (idempotent
across restarts). Squashing rather than reconciling was possible because the
plan changed to rebuilding both databases from scratch (see below), so there
was no need to preserve compatibility with the old, partially-wrong history.

Also surfaced in passing: the current model maps `DobViolation` to table
`DobBisViolations` and `EcbViolation` to `ECBViolations`, but the old migration
(and presumably production, right now) has them as `DOB_Violations` and
`ECB_Violations`. Same class of drift as the view issue — the rebuild adopts
the model's current names, which is what the running app code actually expects.

**3.1 — tier view entitlement tests.** Added
`KonXProWebApp.Integration.Tests/Database/TierViewEntitlementTests.cs`,
table-driven per tier, asserting both boundaries (`HouseNum` never on Free,
`EstimatedCost` never on Free or Basic). The five view definitions were
extracted out of `Program.cs`'s startup DDL into `Data/TierViewDefinitions.cs`
so the test and the startup code run identical SQL rather than a copy that
could drift. **Not yet executed** — this machine doesn't have Docker Desktop
running and the test needs `Testcontainers.MsSql`. Verified the same SQL and
column shapes directly against a disposable LocalDB database instead; run the
real suite in CI or any Docker-capable environment before treating it as a gate.

**0.2 — package versions pinned.** `Directory.Packages.props` at the repo root,
central package management, no `Version` attribute in any csproj. Pinned to the
versions that were already resolving; nothing was upgraded except
`Microsoft.Data.SqlClient` in `KonXProWebApp.Functions.Tests`, which moved from
6.0.1 to 6.1.6 because the old value was a NuGet downgrade error (NU1605) against
the project under test.

**0.3 — CI test gate.** Corrected 2026-09-11: this was marked Completed but
`.github/workflows/build-and-test.yml` had never actually been committed to any
branch — the description below was aspirational, not real, and nothing was
validating any PR. Actually created now. Runs on every PR to `master` (and, for
the moment, to `feat/central-package-management`, since that branch hasn't
merged yet and PRs are currently targeting it) and on push to `master`. Builds
the solution with both .NET 8 and .NET 9 SDKs and runs `KonXProWebApp.Tests`
and `KonXProWebApp.Functions.Tests`. A second job runs
`KonXProWebApp.Integration.Tests` against a Testcontainers SQL Server (GitHub's
`ubuntu-latest` runners have Docker preinstalled), currently `continue-on-error:
true` since it has no track record yet. Remove the `feat/central-package-management`
target and the `continue-on-error` line once that branch has merged and the
integration job has proven stable, respectively.

**1.1 — ground truth.** See Findings above.

---

## Open

### 0.1 — Rotate exposed credentials · do first, blocks nothing · revised

Worse than originally described: the production (`konx_admin` / `priority_konx`)
and staging (`konx_staging_admin` / `priority_ks`) SQL passwords, plus two SMTP
passwords, were committed in **plain text in git** — `appsettings.json`,
`appsettings.Staging.json`, `appsettings.Development.json`, and
`KonXProWebApp.Functions/local.settings.json` — since the repository's initial
commit, and pushed to `origin/master`. The `~/.gemini/config/config.json` copy
this item originally described is a separate, additional exposure on top of that.

[PR #37](https://github.com/DrumsNY1/KonXProWebApp/pull/37) (merged) replaces
all four real values with `PLACEHOLDER`, adds `UserSecretsId` to both csproj
files so `dotnet user-secrets` works, and untracks `local.settings.json` going
forward (Azure Functions local settings should never be committed). **The
production and staging SQL Server passwords have been rotated** (2026-09-11).
Local `dotnet user-secrets` has been updated with the new passwords. **Still open:**

- Update the server's environment variables/Plesk app settings with the new
  passwords — local dev is covered, but the deployed app/Functions will still
  fail to connect until the server side is updated too.
- Rotate the Web Deploy password and clean up `~/.gemini/config/config.json`.
- Decide whether/how to scrub the old values from git history (disruptive with
  ~30 active branches — worth a deliberate decision, not a reflexive rewrite).
  Lower urgency now that the credentials they protect are no longer valid.

This matters more now that an agent with a shell runs on this machine — and
note that `dotnet ef` commands build the app host the normal way, which reads
user secrets automatically; once real credentials are in user-secrets, any
`dotnet ef` invocation against this project can silently connect to whichever
database they point at unless the connection string is explicitly overridden
(e.g. via a `ConnectionStrings__db_9f8bee_konxdevConnection` environment
variable, which outranks user secrets in ASP.NET Core's configuration
precedence). This is exactly how a read-only production connection happened
once already during this remediation work.

### 2.3 — Add the two missing unique indexes to staging · small

Production already has both. Staging has neither and no conflicting data. Add
them, or rebuild staging from production, which also fixes the heap
`IngestionLogs` and the raw-DDL key names.

### 2.4 — Retire the startup DDL · blocked by 2.3 · deliberately deferred

Remove `EnsureCreated()` and the now-redundant `Subscriptions`/`SavedLeads`/
`AlertPreferences` table DDL from `Program.cs`; replace with
`permitDb.Database.Migrate()`. The five-view `CREATE OR ALTER VIEW` loop stays
regardless (now reading from `Data/TierViewDefinitions.cs`) unless 3.2 also
happens. Keep the `IsEnvironment("Testing")` guard — the integration tests
depend on startup doing nothing. Consider moving migration out of app startup
entirely into a deploy step; an app that migrates on boot races itself when
two instances start.

Deliberately not done yet in this pass — 3.1's tests exist now as the safety
net this item's ordering always called for, but haven't been run against a
real database in this environment (see 3.1 above), so removing the code that
creates these objects felt premature until that test suite has actually gone
green somewhere with Docker available.

### 3.2 — Move the five views into migrations · optional, not required by the rebuild

`CREATE OR ALTER VIEW` is already idempotent, so each view could move into its
own migration nearly as-is, making a future change a reviewable diff. Not done
as part of this pass — the views were instead excluded from migrations
entirely (`ToView()`) and left as raw DDL in `Program.cs`, which was enough to
fix the real bug (item 2.2) without also relocating working code. Revisit only
if the raw-DDL startup block is retired for good in 2.4.

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

## Rebuild plan (chosen approach for 2.0–2.4)

Neither database is serving real traffic yet, which opened up a cleaner path
than the original item-by-item patching: rather than reconcile the old,
partially-wrong migration history in place, both databases get rebuilt from a
clean migration set. What's actually irreplaceable is small — Identity
(accounts/tenants), `Subscriptions` (4 rows), `AlertPreferences` (2 rows) — and
gets exported and reimported. `DOBJobFilings` is public NYC record data and can
be re-ingested via the existing Azure Functions rather than restored from a
backup. `IngestionLogs` and staging's 3 `SavedLeads` rows are disposable.

Status: the corrected migration baseline and the entitlement tests (2.0–2.2,
3.1) are done and committed on `fix/migrations-history-and-schema-rebuild`,
verified against a disposable LocalDB database. **The actual rebuild of
staging or production has not happened** — that's a deliberate, separate step:
back up both databases, export the small irreplaceable tables, drop and
rebuild staging first (`dotnet ef database update`), reimport, re-run
ingestion, verify, then repeat on production only after staging is clean.

## Revised critical path

```
0.1 (independent, credential rotation still open — see above)
2.0, 2.1, 2.2, 3.1 — done (rebuild plan above)
2.3 (independent, small — folds into the rebuild regardless)
2.4 -> 3.2 (optional) — deferred until 3.1 has actually run green somewhere
4.1, 4.2, 4.3 (independent, untouched by this pass)
```
