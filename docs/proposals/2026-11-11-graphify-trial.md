> **Status: Rejected — not authoritative.** This is a proposal, not a description
> of how the system works. `AGENTS.md` and the code win over anything here.
>
> Date: 2026-11-11 · Author: coding agent, for Reggie · **Decision: rejected
> 2026-11-12** by Reggie, after day 2 of the trial. Graphify cannot see six of the
> seven links that enforce subscription tiers. See [Decision](#decision-2026-11-12)
> at the end.

# Proposal: trial Graphify in KonXProWebApp

## Summary

**Feasible: yes, at low risk, if the trial is dev-only, code-only and opt-in.**
Graphify is a Python CLI that runs next to the .NET solution. It adds no NuGet
packages, changes nothing in the `.sln`, and nothing in CI or at runtime. At this
repo's size the payoff is **uncertain**. The two main unknowns are how well it
extracts `.razor` files and whether it follows DI wiring. Recommendation: a
two-week trial with exit criteria, not adoption.

---

## The tool (verified 2026-11-11)

| Fact | Value |
|---|---|
| Package / CLI | `graphifyy` on PyPI / `graphify` |
| Version | **0.9.106**. First release 2026-04-04; about 100 releases since |
| License | Apache-2.0 |
| Python | `>=3.10, <3.15` |
| Publisher | Graphify Labs (YC S26), single PyPI owner. Open-core with a paid "Graphify Cloud" upsell |
| Output | `graphify-out/graph.json`, `GRAPH_REPORT.md`, `graph.html`, `cache/` |
| Code parsing | tree-sitter AST, **local, no LLM**. `tree-sitter-c-sharp` is a base dependency |
| Docs / PDFs / images | Sent to an LLM (the assistant's model, or an API key) for a semantic pass |
| Telemetry | None claimed. Queries are logged locally to `~/.cache/graphify-queries.log` |
| Agent integration | `graphify antigravity install`, `graphify claude install`, hooks, optional MCP server |

## Fit with this repo

| Item | Finding |
|---|---|
| Corpus | About 183 `.cs`, 78 `.razor`, 11 `.md`, 2 `.sql` (excluding bin/obj/publish) |
| C# | Supported through tree-sitter |
| `.razor` | Listed as a code extension, but **no Razor grammar ships**. Edge quality for `@inject` / `@code` is unknown |
| Python on the dev machine | **Not installed.** `python`, `uv` and `pipx` are all absent from PATH |
| Agent instructions | `AGENTS.md` (canonical), `CLAUDE.md`, `.claude/settings.json`, `.agents/skills/` |

---

## Pros

1. **No footprint in the product.** No packages, no build change, no runtime or
   deploy impact.
2. **Code parsing is local and needs no API key.** With `--code-only` or
   `GRAPHIFY_BACKEND=none`, nothing leaves the machine.
3. **Impact analysis.** Questions like "what reaches
   `SubscriptionAuthorizationHandler`?" or "who calls `PermitIntelService` vs.
   `Db9f8beeKonxdevService`?" get answered from a call/import graph instead of grep.
4. **Possibly cheaper agent sessions.** Agents run scoped `graphify query`/`path`
   instead of re-reading files.
5. **Edges are honest.** Each edge is tagged `EXTRACTED` or `INFERRED`.
6. **Easy to back out.** `graphify uninstall --purge`.

## Cons and risks

1. **Publish trap.** The root web project uses `Microsoft.NET.Sdk.Web`. Its
   default `Content` glob includes `**\*.json`, so `graphify-out/graph.json` and
   `cache/*.json` **would be published to staging** unless `graphify-out\**` gets
   `Compile/Content/EmbeddedResource/None Remove` items.
2. **It edits canonical instruction files.** The always-on installers edit
   `AGENTS.md`/`CLAUDE.md`, add rules under `.agents/`, and add PreToolUse hooks to
   `.claude/settings.json`. `--strict` mode can deny `Read`/`Grep`.
3. **Blind spots where this repo needs help most.**
   - Tier views are SQL strings in `Data/TierViewDefinitions.cs`, so the
     entitlement surface stays invisible to the graph.
   - DI (`AddScoped`, `@inject`), EF `DbSet`s, OData routes, policy names and
     ingestion ADO.NET SQL are convention or runtime links. Expect missing or
     `INFERRED` edges.
4. **Maturity and supply chain.** Pre-1.0, several releases a week, single
   publisher. Pin the version.
5. **Modest value at this scale.** The vendor's own figures are about 1x token
   savings at 6 files and about 71x at 52. This repo already has curated
   navigation (`AGENTS.md`, `docs/repo-reference.md`).
6. **Doc semantic pass exposure.** `REMEDIATION.md` and `docs/` contain
   infrastructure detail. A headless run with an API key sends them to a third
   party. The Kimi backend routes to servers in China and is auto-selected if
   `MOONSHOT_API_KEY` is set.
7. **New toolchain.** Python 3.10+ and `uv` per developer, Windows PATH setup,
   and a stale graph after every pull unless a post-commit hook is installed. The
   hook adds latency to every commit.

---

## Trial design

**Two weeks, one developer machine, branch `chore/graphify-trial`, code-only.**

| Decision | Choice |
|---|---|
| Install | `winget install astral-sh.uv`, then `uv tool install graphifyy==0.9.106` (optionally set `UV_TOOL_DIR` to put it on `D:\`) |
| LLM backend | `GRAPHIFY_BACKEND=none`, `--code-only` |
| Output | `graphify-out/` gitignored, plus csproj `Remove` items |
| `.graphifyignore` | `publish/`, `publish.zip`, `logs/`, `scratch/`, `_schema-truth/`, `*.docx` |
| Agent integration | None in week 1 (manual `graphify query`). Week 2, optionally `graphify antigravity install --project`, with the diff reviewed and AGENTS.md edits kept hand-written |
| Hooks / `--strict` / git hook | Not installed |
| CI | Untouched |

### Repo changes the trial needs

- `.gitignore`: add `graphify-out/` and `.graphify_*`.
- `KonXProWebApp.csproj`: add four `Remove` lines for `graphify-out\**`.
- `.graphifyignore`: new file.

### Success criteria (end of week 2)

1. **Razor fidelity:** on 5 sampled pages, the page → injected service → method
   edges are correct.
2. **Answer quality:** 5 real questions get answers from `graphify query`/`path`
   that are at least as good as grep.
3. **Build hygiene:** build, all test projects, and a local folder publish contain
   no `graphify-out` files.
4. **Noise:** wrong or `INFERRED` edges are rare enough to trust the answers.

**Adopt** if 1–3 pass. **Drop** (`graphify uninstall --purge`, revert the branch)
if 1 or 2 fail.

## Open questions

1. Is it OK to install `uv` and Python on the dev machine, and should they go on
   the D: drive?
2. Code-only, or include a doc semantic pass? If docs, which backend?
3. If adopted, should graph output stay local or be committed? Recommendation:
   keep it local.

---

## Trial log

### Day 1 — 2026-11-11

**Setup (as recommended):** everything lives under the gitignored, build-excluded
`_tools\`:
- portable `uv` 0.13.7;
- a uv-managed CPython 3.13.18;
- a venv with `graphifyy==0.9.106` and 31 dependencies.

Nothing went onto PATH, into Program Files or into the registry. (The `py`
launcher's Python 3.14 entry pointed at a deleted `C:\Python314`, hence uv.) The
graph was built with `graphify update .`, `GRAPHIFY_BACKEND=none` and
`GRAPHIFY_QUERY_LOG_DISABLE=1`: fully offline, inside the sandbox.

**Build:** 288 files in 30 s. 4,136 nodes, 8,523 edges (7,905 `EXTRACTED`,
618 `INFERRED`), 211 communities. Five files yielded no symbols: two near-empty
`.razor` files, `KonXProWebApp.Functions/Program.cs` (top-level statements) and
two JS files.

#### Criterion 3, build hygiene: **PASS**

- `graphify-out/` is ignored by git.
- The web project's `ResolvedFileToPublish` list (203 files) contains nothing from
  `graphify-out`.
- Solution build: 0 errors.
- Unit and Component tests: `KonXProWebApp.Tests` 161/161, `Functions.Tests`
  59/59.

#### Criterion 1, Razor fidelity: **PARTIAL**

Five pages, ground truth taken from source:

| Page | Injected types linked | Own-code service calls linked |
|---|---|---|
| `PermitIntel/RequireTier.razor` | 3/3 | **0/2** |
| `Campaigns/LandingA.razor` | 3/3 | n/a (framework calls only) |
| `Profile.razor` | 9/9 | 5/5 |
| `FreeTier.razor` | 8/8 | 1/1 (two more "missing" calls are commented out, so correctly absent) |
| `DobjobFilings.razor` | 8/8 | 6/6 |
| **Total** | **31/31** | **12/14** |

- Calls into framework types (`NavigationManager`, `IJSRuntime`, `DialogService`,
  `NotificationService`, EF `SaveChangesAsync`) have no target node, which is
  expected: that code isn't in the repo.
- **Code-behind (`.razor.cs`) works well.** Every miss was inside a `.razor`
  `@code { }` block. Ten `.razor` files have such blocks, including the tier gate
  `RequireTier.razor`.

#### Criterion 2, answer quality vs. grep: **PARTIAL (about 3 of 5)**

| Question | Graphify | Missed (root cause) |
|---|---|---|
| Callers of `StripeService.GetTierInfo()` | 2 of 3 | `Subscribe.razor.cs:41`: fully qualified static call in a field initializer |
| Callers of `PermitIntelService.GetActiveSubscription()` | 6 of 7 | **`Authorization/SubscriptionRequirement.cs:61`**: service resolved with `GetRequiredService<>()` inside the auth handler |
| What uses `SubscriptionAuthorizationHandler` | 6 of 9 | **`Program.cs:28`** DI registration (generic type argument) and two `new SubscriptionAuthorizationHandler(...)` constructions in tests |
| Callers of `SubscriptionTierService.GetUserActiveTier()` | 5 of 6 | **`RequireTier.razor:40`**: `@code` block |
| "What reads or writes the Subscriptions table" (free-text `query`) | Poor | Found the `DbSet` declaration only. Drifted into `design-language.md` ("Tables") and OpenXML; none of the 12 production usages |

**What it did better than grep:**
- Output is structured per method (`Profile.OnInitializedAsync() → …`), not per
  line.
- Each edge carries `EXTRACTED`/`INFERRED`.
- No false positives from comments or similarly named identifiers.
- `explain` is fast once you know the node name.

**Day-1 reading:** the predicted blind spots are real, and they cluster exactly on
the entitlement path: the `RequireTier` gate, the auth handler's
service-locator call, and the DI registration. Graphify answers "who calls this
method?" well for code-behind and services. It is not trustworthy for "is this
complete?" questions about subscription or tier enforcement, which are the ones
this repo most needs answered. Free-text `query` is not useful on this codebase
without the LLM semantic pass.

**Not tested** (the trial stopped on day 2; see below):
- Criterion 4 (noise) beyond the above.
- `path` queries.
- Whether `GRAPH_REPORT.md` communities are a useful architecture view.
- Agent integration (week 2, optional).

### Day 2 — 2026-11-12: the entitlement path

Every link that decides what a subscriber can see or call, checked against the
graph. Ground truth comes from source; graph truth from `graph.json`.

| # | Link | Where | In the graph? |
|---|---|---|---|
| 1 | Pages and API carry a tier policy | `BasicTier`/`MidTier`/`HighTier.razor` line 2, `PermitIntelApiController.cs:15` | **No.** None of the four policy names appear |
| 2 | Policy name → `SubscriptionRequirement` | `Program.cs:42–45` (`AddPolicy`) | **No.** Joined by a string |
| 3 | Requirement → handler | `Program.cs:28` (DI registration) | **No.** Generic type argument |
| 4 | Handler → subscription lookup | `Authorization/SubscriptionRequirement.cs:61` | **No.** Service resolved with `GetRequiredService<>()` |
| 5 | Lookup → `Subscriptions` table | `Services/PermitIntelService.cs:487–506` | Partial. Method present; free-text `query` found none of 12 production usages |
| 6 | Component gate → tier service | `Components/Pages/PermitIntel/RequireTier.razor:40–41` | **No.** Calls are inside a `@code` block |
| 7 | Tier views → visible columns | `Data/TierViewDefinitions.cs` | **No.** SQL inside C# strings |

What the graph does get right are the ordinary callers of the tier and
subscription services from `.razor.cs` pages, services and tests.

**Side finding (not a Graphify issue):** nothing references
`RequireTier.razor`: no page, component or test. Tiers are enforced only by the
four `[Authorize(Policy = ...)]` attributes in link 1, although `AGENTS.md`
describes `RequireTier` as "the component-level gate". Tracked separately.

## Decision (2026-11-12)

**Rejected.** The gaps follow from how this repo is built, not from sampling:
- string policy names;
- DI and service-locator resolution;
- `.razor` `@code` blocks;
- SQL held in strings.

More trial days would not change the result. A tool that silently omits six of
the seven links enforcing entitlements would make "is this complete?" answers
look authoritative when they are not. That is worse than grep plus `AGENTS.md`,
which at least do not imply completeness.

**What stays:** the `.gitignore` entries, the four `graphify-out\**` `Remove`
items in `KonXProWebApp.csproj` and `.graphifyignore` (all from #73). They are
inert, and they stop a stray local graph from ever reaching publish output.

**What goes:** the local install (portable `uv`, the uv-managed Python, the
`graphify-venv`) and `graphify-out/`. All of it is gitignored, so removing it
changes nothing in the repo.

**When to revisit:** only if Graphify gains Razor `@code` extraction and
DI/string-key resolution, or if the question changes to plain code navigation
rather than entitlement completeness.
