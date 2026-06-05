---
description: Review a PR (or the local branch diff) by fanning out the review subagents, then synthesize one ranked report
argument-hint: "[pr-number-or-link]  (omit to review the current branch's diff)"
---

Review **$ARGUMENTS** by orchestrating the three read-only review subagents and synthesizing their
findings into a single report. Do **not** edit code — this command only reports.

## 1. Resolve the target

- **Argument given** (PR number or full GitHub PR URL): resolve owner/repo/PR number, then fetch PR
  metadata, changed files, CI checks, and existing review threads/comments via **GitHub MCP**
  (`mcp__github__pull_request_read`, `mcp__github__list_pull_requests`, …). Make the diff locally
  reviewable so the subagents can read full file context — e.g. `gh pr checkout <number>`, or fetch
  the PR ref. Record the base so the diff range is `git diff <base>...HEAD`.
- **No argument**: review the current branch. Use `git status` and `git diff` (and `git diff
  <base>...HEAD` against the main branch) to determine the changed files and diff range. No GitHub
  calls needed.

If resolution fails, stop and explain exactly what failed and the expected argument format.

## 2. Fan out the subagents (in parallel)

Dispatch all three **in a single message** (parallel Task/Agent calls) so they run concurrently, each
in its own context window. Give each the same payload: the **list of changed files** and the **diff
range** (e.g. `<base>...HEAD`), and tell it to review only those changes and return findings in its
standard output format.

- **code-reviewer** — correctness, edge cases, null/async handling, error paths, maintainability.
- **convention-auditor** — NeuroNotes house rules & architecture from `CLAUDE.md`.
- **security-reviewer** — secrets/token handling, untrusted input, injection, auth.

The subagents are read-only (`Read, Grep, Glob, Bash`); they cannot modify code.

## 3. Synthesize one report

Merge the three result sets:

- **Dedupe** findings that overlap (same `file:line` + same root issue) — keep the highest severity
  and note which lenses raised it.
- **Rank** by severity: Critical → Warning → Suggestion.
- Cross-reference any **existing PR review comments** (when reviewing a PR) so you don't repeat
  feedback already left on the thread.

Then output:

1. **Overall status** — `approve` / `request changes` / `needs investigation`, with a one-line reason.
2. **Prioritized findings** — each with `severity · file:line · issue · why it matters · minimal fix`,
   grouped by severity. Attribute the lens (code / convention / security) in brackets.
3. **Open questions** — anything that blocks confidence or needs the author's intent.

Keep it concise and actionable. Ignore pure style/formatting the toolchain already enforces
(`dotnet format`, analyzers, `TreatWarningsAsErrors`).

## Notes

- The subagents are also usable directly without this command — `@code-reviewer`,
  "use the security-reviewer subagent on these changes", or via proactive delegation.
- This command reports only. To act on the findings, follow up with `/fix-review-comments` (for a PR)
  or apply fixes manually.
