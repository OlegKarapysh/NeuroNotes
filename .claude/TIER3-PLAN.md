# Tier 3 — Claude Code Adoption Plan for NeuroNotes

> Status: **PLANNED** (not started). Tiers 1 & 2 are done — see
> `~/.claude/projects/.../memory/agent-tooling-adoption.md` for their record.

**Theme:** Tier 1 gave agents a *foundation* (CLAUDE.md, Central Package Management, guardrails, tests).
Tier 2 gave them *workflow automation* (`.claude/settings.json`, the format-on-Stop hook, the `/new-command`
and `/new-module` scaffolding commands, CI test + format gates). **Tier 3 turns a single well-instructed agent
into a capable system** — one that pulls in authoritative external knowledge, applies house expertise
automatically, and can autonomously deliver actual README roadmap features behind quality gates.

Four work-streams (A–D), roughly in dependency order, plus guardrails (E).

Current gaps (verified 2026-06-01): `.claude/` has `commands/`, `hooks/`, `settings.json` — but
**no `agents/`, no `skills/`, and no `.mcp.json`**. Closing those three gaps + delivering real roadmap
features is what Tier 3 covers.

---

## A. MCP servers — live external knowledge

The codebase leans on fast-moving libraries (**Semantic Kernel ~1.74, .NET 10, MassTransit, Whisper.net**)
where the model's training data is stale and prone to inventing APIs. MCP servers fix that with authoritative,
live context. Config lives in a **committed `.mcp.json` at repo root** (project-scoped, shared with the team).

| Server | Why it earns its place here | Priority |
|---|---|---|
| **Microsoft Learn** (`microsoft.docs.mcp`) | Authoritative .NET 10 / SK / MassTransit API docs — kills hallucinated APIs in the area you build most | **High** |
| **GitHub** | Issue/PR triage, repo + upstream code search, let agents open PRs | **High** |
| **Context7** | Version-pinned library docs (SK 1.74 specifically) | Medium |

**Decision point:** GitHub MCP needs a PAT — scope it read-only first, widen only if you want agents opening
PRs. Recommended start: Microsoft Learn (zero-auth) + GitHub read-only.

## B. Subagents — specialized roles (`.claude/agents/`)

Definitions tuned to *NeuroNotes* conventions, so the main agent can delegate and stay focused:

1. **`code-reviewer`** — reviews diffs against CLAUDE.md: FluentResults (not exceptions) for expected failures,
   extension-member `ServiceInstaller`s, options pattern, `sealed`/file-scoped, nullable-clean, MTP test rules.
   Read-only.
2. **`test-author`** — writes **pure** xUnit v3 tests with hand-written fakes (no mocking lib, no
   network/LLM/Whisper), respecting `OutputType=Exe` + the `--solution` MTP quirk.
3. **`module-architect`** — designs new module boundaries honoring the Public/Application/Infrastructure
   layering before any code is written.

## C. Skills — latent, auto-applied expertise (`.claude/skills/`)

Unlike slash commands (which you invoke), skills trigger *automatically* when relevant:

1. **`neuronotes-conventions`** — the house patterns, so **any** agent applies them without being told.
   The single highest-leverage Tier 3 artifact.
2. **`mtp-testing`** — bundles the `dotnet test --solution`/`--project` rules + a small runner script, so the
   MTP gotcha never bites again.

## D. Delegated roadmap delivery — the payoff

Use A–C to ship the **first real slice of the README roadmap**. The keystone is **durable persistence**: today
`InMemoryNoteStore`/`ChatStateStore`/`LastTranscriptionStore` lose everything on restart. Persistence unblocks
RAG, versioning, and semantic search — they all need stored notes first.

Proposed sequence (each a scoped workflow: design → implement → test → review):

1. **Markdown + frontmatter file persistence** (NFR #2 Obsidian-agnostic, NFR #3 backup) — replace
   `InMemoryNoteStore` with a file-backed `INoteStore`. **The keystone — do first.**
2. **Semantic search + RAG** (FR #12, #18) — SK embeddings + a vector store. Depends on (1).
3. **Auto-tagging & backlinks** (FR #5, #6) — incremental, non-blocking processing.

For these, use the **Workflow tool** (design panel → implement → adversarial test/review), since they're
multi-step and benefit from independent verification.

**Open decisions before building D1:** storage shape — single vault directory vs. per-user folders; where the
vault root is configured (Options pattern); how chat state durability is handled separately from notes.

## E. Guardrails for greater autonomy

As agents do more on their own, tighten safety to match:

- **`PreToolUse` hook** that blocks edits to secrets / `appsettings*.json` / CI / `Directory.*.props` unless
  explicitly intended.
- Carefully widen the permission allowlist (still **no** destructive git).
- Optionally a `Stop` build+test for larger changes (the format hook already covers style).

---

## Suggested order & effort

| Step | Effort | Unblocks |
|---|---|---|
| A — MCP (Learn + GitHub) | ~30 min | Better accuracy everywhere |
| C — `neuronotes-conventions` skill | ~30 min | Consistency for all later work |
| B — subagents | ~1 hr | Delegation for D |
| E — guardrails | ~30 min | Safe autonomy |
| **D1 — file persistence** | **~half day** | **RAG, versioning, search** |
| D2–D3 — RAG, tagging | multi-session | Core product value |

A, B, C, E are pure tooling — low risk, reversible. **D is where Tier 3 stops being meta-work and starts
shipping the actual product**, and D1 (persistence) is the single most valuable thing in the whole roadmap
because so much depends on it.

---

## First move when we pick this up

The cheap, high-leverage trio: `.mcp.json` (Microsoft Learn + GitHub read-only) + the `neuronotes-conventions`
skill. Then decide the persistence storage shape and start D1.
