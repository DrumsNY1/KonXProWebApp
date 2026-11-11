> **Status: Proposed — not authoritative.** This is a proposal, not a description
> of how the system works. `AGENTS.md` and the code win over anything here.
>
> Date: 2026-11-11 · Author: coding agent, for Reggie · Decision: pending

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
