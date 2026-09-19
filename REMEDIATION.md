# KonXProWebApp — remediation status and plan

Living document. Last updated 2026-09-19.

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

> **Historical — staging's schema described here no longer exists.** This
> finding captures the 2026-09-10 capture, before item 2.3 (indexes) and the
> staging rebuild (see "Rebuild plan" below) replaced staging's permit schema
> entirely on 2026-09-12. Kept as-is for the record of what was found and why
> "staging is not a rehearsal for production" was true at the time. Production
> has not been touched and still matches this section.

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

**0.4 — baseline production's `dbo.__EFMigrationsHistory`.** Discovered
2026-09-13 (see "Rebuild plan" below): the code pin from item 2.0 means
`identityDb.Database.Migrate()` reads `dbo.__EFMigrationsHistory` on every
startup, but production's actual Identity migration history lives under
`konx_admin` (the login's default schema, per Finding 1) — the exact failure
already hit and fixed on staging. Left urgent and open because it can fire on
an ordinary IIS/Plesk app-pool recycle, not just a deliberate restart, and is
independent of the rebuild plan.

Read-only verification via `_tools/schema-truth/` on 2026-09-18 (a section was
added specifically for this) confirmed every precondition before anything was
written: `dbo.__EFMigrationsHistory` did not exist yet (proving production had
not yet restarted under the item 2.0 pin — the failure had not happened yet);
`konx_admin.__EFMigrationsHistory` held exactly 6 rows; all 8 Identity tables
(`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`,
`AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`, `AspNetTenants`)
resolved to `konx_admin`, created within the same second as
`__EFMigrationsHistory` itself; and `konx_admin.__EFMigrationsHistory`'s exact
column shape (`MigrationId nvarchar(150)`, `ProductVersion nvarchar(32)`,
`PK___EFMigrationsHistory`) matched EF Core's standard shape exactly, so the
new `dbo` copy could be built to mirror it precisely rather than by assumption.

Fixed 2026-09-18, run directly against production inside an explicit
transaction (`CREATE TABLE dbo.__EFMigrationsHistory` matching the verified
shape, then the baseline `INSERT ... SELECT ... FROM
konx_admin.__EFMigrationsHistory`, then a `SELECT *` reviewed before
committing):

```sql
CREATE TABLE dbo.__EFMigrationsHistory (
    MigrationId nvarchar(150) NOT NULL,
    ProductVersion nvarchar(32) NOT NULL,
    CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
);

INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
SELECT MigrationId, ProductVersion
FROM konx_admin.__EFMigrationsHistory
WHERE MigrationId NOT IN (SELECT MigrationId FROM dbo.__EFMigrationsHistory);
```

Confirmed committed: `dbo.__EFMigrationsHistory` holds exactly 6 rows outside
the transaction — `00000000000000_CreateIdentitySchema` plus the five
pre-squash `db_9f8bee_konxdevContext` migrations item 2.2 describes
(`AddServiceRequests311`, `AddHomeImprovementContractors`,
`AddMarketingFieldsToContractors`, `ScaffoldPendingChanges`,
`AddCampaignTrackingToContractors`) — matching `konx_admin`'s copy exactly, as
expected since production has not been rebuilt.

**Not yet closed out: proof at an actual restart.** This fixes the history
table itself, but the risk this item exists for — `Migrate()` throwing and the
startup try/catch in `Program.cs` silently swallowing it — is only provably
resolved once production restarts for real and its logs show "No migrations
were applied" instead of an exception. That hasn't been observed yet.

Tried and inconclusive, 2026-09-18: a Plesk "restart application pool" action,
and separately touching `web.config` in Plesk's File Manager (the standard
ANCM trigger for recycling an out-of-process worker), were both tried to force
a fresh restart for verification. Neither shows any evidence of actually
having recycled the app process — two independent, unrelated signals in the
database (the tier views' `modify_date`, and the test-tier `Subscriptions`
rows' `UpdatedAt`, both written by different steps of the same startup block)
are still frozen at the same prior restart, 2026-09-08. Note this also means
`stdoutLogEnabled`'s log files (`logs\stdout_*.log`, per `web.config`) are
stale for the same reason and can't be used to check this either — the last
one is from July 15, despite the database evidence showing the app has
restarted several times since then. Root cause of why Plesk's restart/touch
actions aren't recycling the process is unknown; only Plesk web panel access
is available (no RDP/console), which limits the options for forcing it
further. Decided to wait for a natural IIS app-pool recycle instead of
continuing to force one blind.

**To check later:** re-run `_tools/schema-truth/` against production and look
at either signal above — if `modify_date`/`UpdatedAt` have moved past
2026-09-08, a restart has happened since, and the stdout log directory should
be checked at that point for "No migrations were applied" vs. "An exception
occurred while migrating or seeding the database on startup."

**4.2 — removed the three excluded dead files.** `ClaudeDobFilings.razor`,
`EditDobjobFiling.razor`, `HighTierDashboard.razor`, and the two matching
`.razor.cs` code-behind files, all deleted, along with their now-pointless
`<Compile Remove>`/`<Content Remove>` entries in `KonXProWebApp.csproj`.
Verified before deleting, not just trusted from this document's own claim:
`ClaudeDobFilings.razor` declares the identical route
(`@page "/view-dobjob-filing"`) as the live `ViewDobjobFiling.razor`, which
raised the question of whether `<Content Remove>` alone actually excludes a
`.razor` file from Razor component compilation (as opposed to just static-file
copying) — if it didn't, this would have been a live ambiguous-route bug, not
dead code. Confirmed empirically with a build before touching anything: 0
errors, no ambiguous-route failure, and neither of the two files (nor their
routes) appear anywhere in the build output, so the exclusion genuinely works
as documented. Rebuilt and ran `KonXProWebApp.Tests` after deleting: build
clean, 156 passed / 0 failed / 4 skipped (pre-existing E2E skips, no live
host) — identical to the pre-deletion baseline plus the five fewer files.

**4.3 — xunit version alignment.** Bumped `xunit` and `xunit.runner.visualstudio`
from 2.5.3 to 2.8.0 in `Directory.Packages.props` — 2.8.0 specifically because
it's the exact `xunit.extensibility.core` version `bunit` 1.36.0 and
`Microsoft.Playwright.Xunit` 1.50.0 were already pulling in transitively, so
this closes the gap rather than introducing a third version into the mix.
`xunit` is centralized, so the change reaches all four test projects even
though only `KonXProWebApp.Tests` references `bunit`/`Playwright.Xunit`
directly — verified each one rather than trusting a solution-wide build alone,
since silent test discovery failures were the specific risk this item called
out:
- `dotnet restore`/`build` on the whole solution: no NU1608 warning, 0 errors.
- `KonXProWebApp.Tests`: 156 passed / 0 failed / 4 skipped — identical to the
  pre-bump baseline.
- `KonXProWebApp.Functions.Tests`: 64 tests discovered (not zero), 56 passed;
  the 8 failures are `Docker is either not running or misconfigured`
  (Testcontainers), pre-existing and unrelated — this machine has no Docker
  Desktop running, same gap noted elsewhere in this document.
- `KonXProWebApp.Integration.Tests`: 21 tests discovered, all 21 fail on the
  same Docker/Testcontainers unavailability — expected, this project is
  entirely Testcontainers-based and was already known to need a Docker-capable
  environment (see 3.1).
- `KonXProWebApp.E2E.Tests`: all 11 Playwright journeys discovered cleanly via
  `--list-tests` (not executed — needs a live host, per this project's own
  requirements, unrelated to this change).

**4.1 — CLOSED, false alarm: the web app already builds in CI.** This item's
premise was stale, same class of issue as several others found this pass.
[`build-and-test.yml:31`](.github/workflows/build-and-test.yml:31) runs
`dotnet build KonXProWebApp.sln --configuration Release --no-restore` — the
whole solution, including `KonXProWebApp.csproj` — so a web app compile
failure already fails the PR check today. What's genuinely still true (from
CLAUDE.md, unchanged): deployment is not automated, and CI never runs
`dotnet publish`, which can fail in ways a plain build won't catch (web.config
transforms, static asset bundling). Presented as an explicit choice rather
than assumed: user chose to close this as already-satisfied rather than add a
`publish` step or a build-artifact step. Revisit only if a `publish`-specific
failure actually happens in production and a CI check would have caught it.

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
validating any PR. Actually created now. Runs on every PR to `master` and on
push to `master`. Builds the solution with both .NET 8 and .NET 9 SDKs and runs
`KonXProWebApp.Tests` and `KonXProWebApp.Functions.Tests`. A second job runs
`KonXProWebApp.Integration.Tests` against a Testcontainers SQL Server (GitHub's
`ubuntu-latest` runners have Docker preinstalled), currently `continue-on-error:
true` since it has no track record yet. It has since run for real on three PRs
(#37, #38, #39) — the job itself isn't clean (11 pre-existing failures unrelated
to any of those PRs, now a separate follow-up task), but the mechanism works:
Testcontainers spins up SQL Server fine on GitHub's runners, and the new tier-view
entitlement tests (item 3.1) passed all 8 cases every time. The workflow briefly also
targeted PRs against `feat/central-package-management` while that branch was
still unmerged; removed 2026-09-12 now that it has merged into `master`
(PR #36).

**1.1 — ground truth.** See Findings above.

**0.1 — rotate exposed credentials.** Worse than originally described: the
production (`konx_admin` / `priority_konx`) and staging (`konx_staging_admin` /
`priority_ks`) SQL passwords, plus two SMTP passwords, were committed in
**plain text in git** — `appsettings.json`, `appsettings.Staging.json`,
`appsettings.Development.json`, and `KonXProWebApp.Functions/local.settings.json`
— since the repository's initial commit, and pushed to `origin/master`. The
`~/.gemini/config/config.json` copy this item originally described was a
separate, additional exposure on top of that.

Sequence of what actually got done:

1. [PR #37](https://github.com/DrumsNY1/KonXProWebApp/pull/37) (merged)
   replaced all four real values with `PLACEHOLDER`, added `UserSecretsId` to
   both csproj files so `dotnet user-secrets` works, and untracked
   `local.settings.json` going forward (Azure Functions local settings should
   never be committed).
2. Production and staging SQL Server passwords rotated (2026-09-11); local
   `dotnet user-secrets` updated with the new values.
3. Web Deploy password rotated for both staging and production (2026-09-13).
4. Reading `~/.gemini/config/config.json` while investigating step 3 surfaced
   that it held **18** command strings with plaintext credentials in its
   `allow` array — not just the Web Deploy password, but the same SQL
   passwords already rotated in step 2. Cleaned up: backed up the original
   file locally, then removed every entry matching a `sqlcmd -P '...'` or
   `dotnet publish ... /p:Password=...` pattern (194 of the original 212
   entries remain; everything else in the file — plugins, theme,
   remote-control settings — untouched). This file lives outside the repo, so
   there was no git-history exposure for it specifically.
5. Server-side Plesk application settings updated with the rotated SQL
   passwords, so the deployed app/Functions actually use the new credentials.

**Decided 2026-09-19: do not rewrite git history.** The remaining piece of
this item was whether/how to scrub the old plaintext values from history —
weighed and closed rather than left open indefinitely. Rewriting history
(`git filter-repo`/BFG) would break all ~30 active branches (every one needs
recreating or rebasing onto the new history), every open PR would need
closing and reopening against it, everyone with a local clone would need to
discard and re-clone, and GitHub's own PR/fork caches can retain old commit
data even after a force-push — a real guarantee of removal needs GitHub
Support involved, not just a rewrite. Against that cost, the benefit is now
almost entirely cosmetic: every credential this would remove (production and
staging SQL passwords, both SMTP passwords) is already rotated and confirmed
invalid, so there is no active leak being stopped, only a dead password
sitting in history. Accepted as a standing risk; revisit only if a
compliance or audit requirement specifically mandates a history purge later.

**Standing caution, unrelated to the decision above and still worth keeping
in mind:** now that an agent with a shell runs on this machine, note that
`dotnet ef` commands build the app host the normal way, which reads user
secrets automatically — once real credentials are in user-secrets, any
`dotnet ef` invocation against this project can silently connect to whichever
database they point at unless the connection string is explicitly overridden
(e.g. via a `ConnectionStrings__db_9f8bee_konxdevConnection` environment
variable, which outranks user secrets in ASP.NET Core's configuration
precedence). This is exactly how a read-only production connection happened
once already during this remediation work.

**2.3 — staging unique indexes.** Added 2026-09-12, directly against staging
(`priority_ks`) — re-checked for conflicting data first (still none: 0 duplicate
`StripeSubscriptionId` groups, 0 duplicate `(UserId, DobjobFilingId)` groups),
then created both indexes to match production exactly:
`IX_Subscriptions_StripeSubscriptionId` (unique, filtered
`WHERE StripeSubscriptionId IS NOT NULL`) and
`IX_SavedLeads_UserId_DobjobFilingId` (unique, on `(UserId, DobjobFilingId)`).
Needed `SET QUOTED_IDENTIFIER ON` first — `sqlcmd` defaults it off, which
`CREATE INDEX` on a filtered index rejects. Staging's heap `IngestionLogs` and
the raw-DDL-generated key names are unchanged; this item was scoped to the two
indexes only, not a full staging rebuild.

---

## Open

### 2.4 — Retire the startup DDL · partially done, `Migrate()` switch blocked

**Done (2026-09-12):** removed the now-dead `Subscriptions`/`SavedLeads`/
`AlertPreferences` raw-DDL block from `Program.cs`. It was a genuine no-op in
every current path — `EnsureCreated()` (which runs just before it) already
creates those three tables from the model on any fresh database, since they're
ordinary `DbSet`s, and they already exist on both staging and production. The
five-view `CREATE OR ALTER VIEW` loop is untouched (still reads from
`Data/TierViewDefinitions.cs`).

**Still blocked: switching `EnsureCreated()` to `permitDb.Database.Migrate()`.**
This is *not* safe to do before the rebuild plan below actually happens.
Neither staging (0 `db_9f8bee_konxdevContext` migrations recorded) nor
production (6 rows recorded, all for the five migrations squashed away in item
2.2) has a history row for the new `InitialCreate` migration. If `Migrate()`
ran against either database as it exists today, EF would treat `InitialCreate`
as pending and try to `CreateTable` everything it defines — which already
exists there — crashing app startup on the next restart. Do the rebuild first;
this half of 2.4 lands as part of it. Keep the `IsEnvironment("Testing")` guard
regardless — the integration tests depend on startup doing nothing. Consider
moving migration out of app startup entirely into a deploy step; an app that
migrates on boot races itself when two instances start.

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


### 4.4 — CLOSED, false alarm: `HPD_Violations` is fine

Originally logged 2026-09-18 as "`HpdViolations` table does not exist on
production," based on a schema-truth query that checked for the wrong table
name. Corrected same day once `HpdViolation.cs`'s actual mapping was checked —
it carries `[Table("HPD_Violations", Schema = "dbo")]` (with an underscore),
not `HpdViolations`. Re-checked with the corrected name: `dbo.HPD_Violations`
exists exactly where the model expects it, holding 112,262 rows, created
2026-07-11. No gap, no action needed. Left in this document as a record that
the original finding was wrong and why, rather than deleting it — the same
investigation surfaced a real, separate bug (below).

---

## Rebuild plan (chosen approach for 2.0–2.4)

Neither database is serving real traffic yet, which opened up a cleaner path
than the original item-by-item patching: rather than reconcile the old,
partially-wrong migration history in place, both databases get rebuilt from a
clean migration set.

**Staging: done, 2026-09-12.** Revised the "what's irreplaceable" assumption
first — it turned out to be nothing. `Program.cs` runs `SeedTenantsAdmin()` and
`SeedTierTestUsersAsync()` unconditionally on every non-`Testing` startup, and
staging's entire Identity dataset (5 users, 1 tenant) and all 4 `Subscriptions`
rows were exactly those seeded fixtures — confirmed by matching UpdatedAt
timestamps to the last app restart. `AlertPreferences` was empty, and
`SavedLeads` (3 rows) was already called disposable. So nothing needed
exporting/reimporting; a full logical backup was taken anyway as a precaution
(20 tables' data, ~46.7 MB, verified row-for-row against `DOBJobFilings` —
saved locally, not committed).

Three previously-unknown pieces of drift turned up along the way:

- Identity tables (`AspNetUsers` etc.) and `__EFMigrationsHistory` live in the
  `konx_staging_admin` schema, not `dbo` — same login-default-schema mechanism
  as Finding 1, just never previously checked for the Identity tables
  specifically. Harmless (EF is internally consistent about it) and out of
  scope for this rebuild; left untouched.
- Six tables — `HomeImprovementContractors`, `DobBisViolations`,
  `ECBViolations`, plus three (`BusinessLicense`, `DOB_Permits`,
  `DOBApprovedPermits`) matching no current model at all — existed only in
  `konx_staging_admin`, not `dbo`. The current app code, which expects
  `dbo.*`, could not see this data.
- `dbo` had **synonyms** with the same six names, each pointing at the
  misplaced table in `konx_staging_admin` — an apparent workaround so
  `dbo`-facing code could still reach the data. Dropping the underlying tables
  without also dropping these left them dangling and blocked the migration
  ("already an object named 'DobBisViolations'") until caught and cleared.

Rebuild steps actually run: dropped the 7 `dbo` permit tables + 5 views, the 6
stray `konx_staging_admin` tables, and the 6 dangling synonyms (Identity/
`konx_staging_admin.__EFMigrationsHistory` untouched); ran
`dotnet ef database update --context db_9f8bee_konxdevContext` with the
connection string overridden via environment variable (never via
`appsettings.json` or a command-line argument) to point at staging; manually
ran the five `CREATE OR ALTER VIEW` statements from
`Data/TierViewDefinitions.cs` (no way to trigger an actual app restart on the
deployed staging instance from here). Verified: 13 tables + 5 views in `dbo`,
both unique indexes present, migration history shows exactly
`20260911165155_InitialCreate`, no leftover synonyms, and the view column sets
match the entitlement boundaries exactly (`HouseNum` absent from Free,
`EstimatedCost` absent from Free/Basic).

**Staging health check, 2026-09-13 — found and fixed a real startup bug.**
After the rebuild, a confirmed Plesk app restart still left `Subscriptions` at
0 rows (expected 4 from `SeedTierTestUsersAsync()`), which is the exact
precondition check below failing. No access to Plesk/IIS logs from here, so
ran the web app locally with `ASPNETCORE_ENVIRONMENT=Staging` and the
connection string overridden via environment variable (never touching
`appsettings.json`) to see the actual startup exception directly.

Root cause: `identityDb.Database.Migrate()` was throwing
`SqlException: There is already an object named 'AspNetTenants' in the
database`, caught and swallowed by the try/catch that wraps the whole startup
sequence — so the app looked "up" but never reached `SeedTenantsAdmin()`,
`EnsureCreated()`, `SeedTierTestUsersAsync()`, or the view-creation loop.
Cause: item 2.0 pinned `MigrationsHistoryTable("__EFMigrationsHistory",
"dbo")`, so EF started reading a brand-new, empty `dbo.__EFMigrationsHistory`
— but the Identity tables themselves (`AspNetUsers`, `AspNetTenants`, etc.)
already existed in `konx_staging_admin` (the login's default schema, per
Finding 1), created by a prior successful `Migrate()` run recorded in the
*old* history table, `konx_staging_admin.__EFMigrationsHistory` (one row:
`00000000000000_CreateIdentitySchema`, product version 9.0.19). EF saw no
recorded history in `dbo` and tried to recreate tables that already existed
under the default-schema fallback.

Fixed on staging with a one-row baseline insert — not a schema change, just
telling EF the migration it's about to try has already happened:

```sql
INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
SELECT MigrationId, ProductVersion
FROM konx_staging_admin.__EFMigrationsHistory
WHERE MigrationId NOT IN (SELECT MigrationId FROM dbo.__EFMigrationsHistory);
```

Re-ran the local app against staging afterward: `Migrate()` logged "No
migrations were applied. The database is already up to date," then seeding
and the five `CREATE OR ALTER VIEW` statements all ran successfully.
Confirmed by direct query: `Subscriptions` = 4 rows, all 5 `vw*` views present.
**Staging is now genuinely healthy — the rebuild plan's precondition is met.**

**This is not staging-specific — it is a live risk for production right now,
independent of the rebuild timeline.** Production has the same
`MigrationsHistoryTable("...", "dbo")` pin already in `Program.cs`
(committed as part of item 2.0) and, per Finding 1, its Identity tables and
`__EFMigrationsHistory` almost certainly still live under `konx_admin` (the
production login's default schema) with the same one-migration history this
staging case had. The **next time production's app pool restarts for any
reason** — not just the planned rebuild — it will hit this identical
exception on `Migrate()` and silently skip seeding and view-refresh. See the
new item below.

**Known gap, investigated 2026-09-12, not solved here:** `DOBJobFilings` and the
contractor/violation tables are now empty pending re-ingestion. There is no
separate staging Function App or deployment slot — only one, `KonXProFunctionApp`,
deployed to slot `Production` (`.github/workflows/master_konxprofunctionapp.yml`).
Investigated how staging is actually meant to receive data:

- `IngestionService` has an optional `StagingSqlConnectionString` config key
  (added by commit `cd12d71`, "feat: add dual-write staging replication for
  permit ingestion", 2026-08-29). When set, **only** `UpsertPermits` and
  `UpsertDobNowFilings` replicate each successful batch to staging after
  writing to production, best-effort. This is presumably how staging's
  `DOBJobFilings` and `HPD_Violations` data existed before the rebuild.
  Whether it's still active depends on whether `StagingSqlConnectionString`
  is configured as an Azure Application Setting on the real deployed Function
  App — not verifiable from this repo/machine.
- The other ingestion paths — 311, DOB violations, HPD violations,
  contractors — have **no** staging mechanism at all; they only ever write to
  whichever connection string is primary (production).

**Separate, more serious finding surfaced along the way — fixed 2026-09-12,
[PR #45](https://github.com/DrumsNY1/KonXProWebApp/pull/45):**
`IngestionService.cs`'s DOB-violations MERGE hardcoded `konx_admin.DobBisViolations`
(from commit `bb3cf46`, "restore konx_admin schema mapping"), but the EF model
was switched to `dbo.DobBisViolations` ten days later (commit `2a83bc4`, "map
all entity schemas to dbo for universal compatibility") and `IngestionService.cs`
was never updated to match. Unlike the other ingestion MERGE statements (which
are unqualified and so benefit from SQL Server's default-schema-then-`dbo`
fallback), this one was explicitly schema-qualified and never got that
fallback — it always wrote to `konx_admin`/`konx_staging_admin`, exactly the
same class of bug as the stray tables and dangling synonyms found during the
staging rebuild above.

Fixed by dropping the schema qualifier so this MERGE follows the same
fallback pattern as every other ingestion path. An audit of every other MERGE
statement in `IngestionService.cs` against its EF model's `ToTable` mapping
found no other mismatches (there is no `ECBViolations` ingestion path at all,
and `HomeImprovementContractors` was already unqualified).

**CLOSED 2026-09-18, false alarm — corrected twice in the same investigation.**
First pass (read-only): confirmed no real table at `dbo.DobBisViolations` via
`sys.tables`/`sys.schemas`, concluded it was a genuine visibility bug like the
one PR #45 already fixed once, and worked out a `CREATE TABLE` +
data-migration script to fix it (same shape as item 0.4's baseline insert).

That script's `CREATE TABLE dbo.DobBisViolations` failed on production with
`Msg 2714: There is already an object named 'DobBisViolations' in the
database` — no partial effect, `@@TRANCOUNT` confirmed `0` immediately after.
That error was the tell: **`dbo.DobBisViolations` is a synonym pointing at
`[konx_admin].[DobBisViolations]`**, confirmed via `sys.synonyms` (a query
`sys.tables` alone will never surface — the same blind spot the earlier
staging rebuild hit with its six dangling synonyms). Synonyms redirect
transparently for both reads and writes, so:
- EF's `ToTable("DobBisViolations", "dbo")` mapping was already resolving
  correctly through the synonym the entire time. There is no visibility bug
  and never was one for the *current* codebase — the 2,166 rows in
  `konx_admin.DobBisViolations` are reachable from `dbo` right now.
- A code change was made and then reverted: qualifying the ingestion `MERGE`
  as `dbo.DobBisViolations` (mirroring item 0.4's forward-fix reasoning)
  turned out to be functionally a no-op once the synonym was known about — it
  resolves through the same synonym to the same physical table, identical to
  the unqualified version. Reverted rather than kept, since it doesn't
  actually change anything and keeping it would misrepresent a fix that
  didn't happen.

**No action needed.** This entry stays as a record of two consecutive wrong
diagnoses in one sitting (wrong table name, then a missed synonym) and how
each was caught — by re-checking a claim against the actual database instead
of trusting the previous read-only capture's coverage.

**Production: scoped 2026-09-19, not yet executed.** Staging's "drop
everything and let EF recreate it" approach is **not** the right model for
production — production has real accumulated data (`DOBJobFilings` ~13,946
rows under active daily ingestion, `HPD_Violations` ~112,271 rows,
`IngestionLogs` history), unlike staging, which turned out to have nothing
but seeded test fixtures. Chosen approach instead: **baseline the single
squashed `20260911165155_InitialCreate` migration into
`dbo.__EFMigrationsHistory`** — the exact same zero-DDL, proven-safe pattern
already used for item 0.4 — rather than executing any `CREATE TABLE`/
`DROP TABLE` against a live, populated database. This is sound specifically
because `Migrate()`'s pending-migration check is a pure `MigrationId`
string-diff against the history table; it never introspects the live schema,
so once baselined, a migration is skipped regardless of what the real schema
looks like (verified by an independent design review before finalizing this
approach).

Done so far as part of scoping:
- **`DobViolation` entity fixed** (`Data/Db9f8beeKonxdevContext.PermitIntel.cs`):
  added `.ToTable("DobBisViolations", "dbo", tb => tb.ExcludeFromMigrations())`
  — `dbo.DobBisViolations` is a synonym (see the `DobBisViolations` entry
  above), and **synonyms cannot take DDL** (`ALTER TABLE`/`CREATE INDEX` fail
  outright against one, even though `SELECT`/`INSERT`/`UPDATE`/`DELETE`
  resolve through it fine) — without this, the next migration that touches
  `DobViolation` at all would hard-fail against production the moment it
  runs. Also fixed two bare `HasColumnType("varchar")` calls (`isn_dob_bis_viol`,
  `boro`) to their real lengths (`varchar(20)`, `varchar(5)`, confirmed via
  schema-truth) — doesn't affect `Migrate()`'s safety, but ordinary
  queries/`SaveChanges` read the current C# model at runtime, so the bare
  type was a real parameter-sizing correctness gap independent of the
  migration question. Verified via `dotnet ef migrations
  has-pending-model-changes` (connection string overridden to LocalDB, never
  `appsettings.json`) that this doesn't introduce a new pending migration —
  "No changes have been made to the model since the last migration." Build
  and `KonXProWebApp.Tests` both clean (156/0/4, same as baseline).
- **`_tools/schema-truth/schema-truth.sql` extended** with: one comprehensive
  `sys.objects`/`sys.schemas` query covering all 12 `InitialCreate` tables +
  5 views at once (does it exist, as what — table/view/synonym — in which
  schema), generalizing the `DobBisViolations` synonym discovery (which was
  found reactively, one table at a time, after a `CREATE TABLE` failure —
  see that entry above) so the same class of miss can't repeat for
  `ServiceRequests311`, `HomeImprovementContractors`, `BlogContent`,
  `BlogFeedSources`, `ECBViolations`; a matching all-names `sys.synonyms`
  check; full column shapes for the six tables never yet compared against
  `InitialCreate` (`DOBJobFilings` — ~90 columns — `ServiceRequests311`,
  `HomeImprovementContractors`, `ECBViolations`, `BlogContent`,
  `BlogFeedSources`); and a check for whether `SavedLeads` already has
  `FK_SavedLeads_DOBJobFilings_DobjobFilingId`.

**Extended schema-truth capture came back 2026-09-19, fully verified. Final
disposition of every table:**

| Table | Real state | Disposition |
|---|---|---|
| `Subscriptions`, `SavedLeads`, `AlertPreferences`, `IngestionLogs` | Real `dbo` table, exact column match | Include in baseline, no DDL |
| `HPD_Violations` | Real `dbo` table, exact column match | Include in baseline, no DDL |
| `ServiceRequests311` | Real `dbo` table, exact column match | Include in baseline, no DDL |
| `HomeImprovementContractors` | Synonym in `dbo` → real table in `konx_admin`; columns match exactly | `ExcludeFromMigrations()` (done — no column fix needed) |
| `DobBisViolations` | Synonym in `dbo` → real table in `konx_admin`; 2 bare-`varchar` columns fixed | `ExcludeFromMigrations()` (done, plus the 2-column fix from earlier) |
| `ECBViolations` | Synonym in `dbo` → real table in `konx_admin`; **extensive column drift** (see below) | `ExcludeFromMigrations()` (done) — column drift deliberately not fixed, see new item below |
| `BlogContent`, `BlogFeedSources` | **Genuinely absent** — not in `dbo`, not a synonym, not in `konx_admin`, anywhere | Split out of `InitialCreate` into a new migration, run for real (done — see below) |
| `DOBJobFilings` | Real `dbo` table, right columns/count, but **widespread type drift** (see below) | Include in baseline, no DDL — drift doesn't block `Migrate()`'s safety, logged as its own follow-up item |

Three tables turned out to be synonyms, not one — `ECBViolations` and
`HomeImprovementContractors` needed the exact same `ExcludeFromMigrations()`
treatment as `DobBisViolations`, both now applied in
`Data/Db9f8beeKonxdevContext.PermitIntel.cs`.

**`BlogContent`/`BlogFeedSources` are genuinely missing** — confirmed live,
actively-used features (real CRUD pages: `EditBlogContent.razor`,
`AddBlogContent.razor`, `BlogContents.razor`, `BlogFeedSources.razor`,
scaffolded through `Db9f8beeKonxdevService`) not mentioned anywhere in
CLAUDE.md's page map, that simply don't exist in the database at all. Handled
exactly per this item's own "missing table" contingency: `InitialCreate`
(`Data/Migrations/20260911165155_InitialCreate.cs` and its `.Designer.cs`,
plus `db_9f8bee_konxdevContextModelSnapshot.cs`) had both tables' blocks
removed, and a new migration, `20260919124444_AddBlogTables`, was scaffolded
via `dotnet ef migrations add` containing just their two `CreateTable`
operations — reviewed and confirmed it picked up nothing else. **Consequence
for staging**: staging already ran the *original* `InitialCreate` for real on
2026-09-12, including creating these two tables there — staging needs the
same one-row baseline-insert for `AddBlogTables` once this is deployed, or
its own next `Migrate()` call would see it as pending and try to recreate
tables that already exist there too. Low urgency (staging is non-critical),
but don't lose track of it.

**Rehearsed for real, not just reasoned about.** Applied the edited
migration set (`InitialCreate` + `AddBlogTables`) via `dotnet ef database
update` against a disposable local database (connection string overridden
inline in the same command — see the near-miss note above) — succeeded
cleanly from scratch, creating all 12 tables across both migrations. Ran
`database update` again immediately after: **"No migrations were applied.
The database is already up to date"** — the exact confirmation this item
needs, demonstrating the migration set is internally consistent end-to-end,
not just individually plausible.

**Both follow-up items fixed 2026-09-19** (neither was blocking the
baseline — `Migrate()`'s safety is purely history-table-driven, confirmed
earlier in this section — but both were real runtime-correctness gaps worth
closing):

- **`DOBJobFilings`** (`Models/Db9f8beeKonxdev/DobjobFiling.cs`): added
  `[StringLength(N)]` to every string column matching its real captured
  length (`nvarchar(255)`, `nvarchar(50)`, etc. — dozens of columns; a
  handful that are genuinely `nvarchar(max)` in reality were left alone), and
  `[Column(TypeName = "money")]` on `InitialCost`/`TotalEstFee` (real type,
  vs. EF's unconfigured `decimal(18,2)` default). Leaves the model with a
  "pending model change" relative to `InitialCreate`'s own embedded snapshot
  — expected and left alone deliberately: generating and applying the
  resulting ~75-column `AlterColumn` migration for real is a separate, much
  bigger decision than fixing the model's correctness, and isn't needed for
  the baseline (which doesn't care what `InitialCreate`'s body says once
  baselined). A future migration for any other real change will naturally
  pick this up too.
- **`EcbViolation`** (`Models/Db9f8beeKonxdev/EcbViolation.cs` +
  `Data/Db9f8beeKonxdevContext.PermitIntel.cs`): the model barely resembled
  the real table. Added the two missing real columns (`CreatedDate`,
  `ModifiedDate`) and one previously-undiscovered one
  (`DobViolationNumber`); fixed `Boro` (`int?`, was non-nullable `int`) and
  `HearingDate`/`ServedDate`/`IssueDate` (`DateTime?`, were non-nullable
  `DateTime`) using the exact same `HasConversion` int/DateTime-as-string
  pattern already proven for `DobViolation.Boro`/`IssueDate` — the C#
  property types didn't need to change, only the value converter, which
  meant **zero changes needed to the `/ecb-violations` Razor pages** despite
  them binding `RadzenNumeric`/`RadzenDatePicker` directly to these
  properties. Removed `[Required]` from every column that's actually
  nullable in reality (nearly all of them) and fixed
  `PenalityImposed`/`AmountPaid`/`BalanceDue` to `decimal(10,2)` (real
  precision, vs. the default `decimal(18,2)`). This was worth doing (not just
  logging) once schema-truth surfaced that `/ecb-violations` has real,
  live CRUD pages (`EcbViolations.razor`, `AddEcbViolation.razor`,
  `EditEcbViolation.razor`) — another feature missing from CLAUDE.md's page
  map — so an unusable model here was a live gap, not a dormant one.

**Verification note, stated plainly rather than glossed over**: build and
`KonXProWebApp.Tests` both clean (156/0/4), and `has-pending-model-changes`
correctly shows a pending change for `DOBJobFilings` only (expected,
`EcbViolation` is excluded from migrations so its changes don't register the
same way). **Not done**: full browser verification of the
`/ecb-violations` add/edit pages against a live database. That would need
wiring a running app instance to a real connection string, and right after
today's near-miss this felt like the wrong moment to improvise a new way of
doing that. Mitigating factors: the nullable-type change follows an
already-proven pattern byte-for-byte, and this is a low-traffic internal
admin page with no ingestion path writing to it today. Worth a real
browser check next time someone's already got a safe local environment
wired up for this table.
- Minor, very low priority given `SavedLeads` has 0 rows currently: the real
  `FK_SavedLeads_DOBJobFilings` foreign key exists but is named differently
  from what `InitialCreate` expects (`FK_SavedLeads_DOBJobFilings_DobjobFilingId`)
  and is `NO_ACTION` on delete rather than `CASCADE`. Still open, still not
  fixed — didn't come up naturally while working on the two items above.

**Final baseline script, ready to run** — combines the `InitialCreate`
baseline with actually creating the two genuinely-missing tables, in one
transaction so both succeed or neither does:

```sql
BEGIN TRANSACTION;

CREATE TABLE dbo.BlogContent (
    ContentID int IDENTITY(1,1) NOT NULL,
    Content nvarchar(max) NULL,
    Summary nvarchar(max) NULL,
    CompletionDate datetime2 NULL,
    SourceID int NULL,
    CONSTRAINT PK_BlogContent PRIMARY KEY (ContentID)
);

CREATE TABLE dbo.BlogFeedSources (
    FeedID int IDENTITY(1,1) NOT NULL,
    FeedName nvarchar(max) NULL,
    FeedUrl nvarchar(max) NULL,
    FeedCategory nvarchar(max) NULL,
    CONSTRAINT PK_BlogFeedSources PRIMARY KEY (FeedID)
);

INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20260919124444_AddBlogTables', '9.0.20');

INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20260911165155_InitialCreate', '9.0.20');

SELECT * FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;

-- Expect 8 rows: the original 6 plus these 2. Only after confirming that,
-- run: COMMIT TRANSACTION;
-- If anything looks wrong instead, run: ROLLBACK TRANSACTION;
```

Still needs its own explicit go-ahead before running against production —
nothing above has been executed there. Once it lands, the follow-up for
staging is a single-row version of the same `AddBlogTables` insert (staging's
`InitialCreate` is already for-real applied from the 2026-09-12 rebuild, so
only the new migration needs baselining there).
- **Explicitly deferred, not part of this rebuild**: flipping
  `Program.cs:105`'s `permitDb.Database.EnsureCreated()` to
  `permitDb.Database.Migrate()` (the rest of item 2.4) stays its own later
  decision, with its own build/test/deploy cycle — bundling it into this
  rebuild would conflate a one-time metadata write with a permanent change to
  every future startup's behavior.

**Near-miss, 2026-09-19 — recorded in full rather than glossed over.** While
rehearsing the migration split below, two `dotnet ef` commands
(`database update`, then `database drop --force`) were run in separate tool
calls without re-applying the LocalDB connection-string override used
earlier in the same session. Root cause: the shell environment does not
persist variables between separate command invocations in this environment,
so the override from an earlier command was silently gone. Without it,
`dotnet ef` fell through to `dotnet user-secrets`, which holds real
production credentials on this machine — `database update` attempted
`CREATE TABLE AlertPreferences` against `priority_konx` directly (failed
immediately: the table already exists there, exactly the standing
`dotnet ef`/user-secrets caution already documented in this file), and
`database drop --force` then attempted to drop `priority_konx` entirely
(failed: the `konx_admin` login lacks server-level permission to drop a
database). **Confirmed via a fresh read-only schema-truth capture that
production is unaffected** — every table's `CreatedUtc`/`ModifiedUtc` and
every row count matched the pre-incident capture exactly, byte-for-byte.
No actual damage occurred, but both commands genuinely reached production
before failing, which is the real lesson: two independent safety nets (the
table already existing, and a permissions boundary) are what prevented harm
here, not the process. Fix going forward: the connection-string override
must be inline with every single `dotnet ef` invocation, in the same
command, every time — never assumed to carry over from an earlier one.

## Revised critical path

```
0.1 — fully done; git-history scrub decided against 2026-09-19 (accepted risk, all credentials already rotated)
0.4 — done (2026-09-18): baseline production's dbo.__EFMigrationsHistory. Still watching for proof at an actual restart.
2.0, 2.1, 2.2, 2.3, 3.1 — done
2.4 -> 3.2 (optional) — worth reconsidering now that 3.1 has run green in real CI
Staging rebuild — done AND verified healthy for real (2026-09-13): Subscriptions=4, all 5 views present, clean restart
Production rebuild — fully scoped and rehearsed 2026-09-19 (baseline-only strategy); 3 synonym tables excluded from migrations, BlogContent/BlogFeedSources split into a real migration, final combined script ready; needs explicit go-ahead to run against production
4.1, 4.2, 4.3, 4.4 (independent, untouched by this pass)
```
