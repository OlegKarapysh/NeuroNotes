---
description: Review a PR (or the local branch diff) with the three review subagents, synthesize one ranked report, and post it back to the PR when one was requested
argument-hint: "[pr-number-or-link]  (omit to review the current branch's diff; add 'report-only' to skip posting)"
---

Review **$ARGUMENTS** by orchestrating the three read-only review subagents, synthesizing their findings
into one ranked report, and — when a PR was requested and exists — **posting that report back to the PR**.
Never edit code: this command reviews and comments only.

## 1. Resolve the target

- **Argument given** (PR number or full GitHub PR URL): resolve owner/repo/PR number, then fetch PR
  metadata, changed files, CI checks, and existing review threads/comments via **GitHub MCP**
  (`mcp__github__pull_request_read`, `mcp__github__list_pull_requests`, …). Record **owner, repo, PR
  number, head SHA, and PR state** — Step 4 needs them. Make the diff locally reviewable so the
  subagents can read full file context — e.g. `gh pr checkout <number>`, or fetch the PR ref. Record
  the base so the diff range is `git diff <base>...HEAD`. This is a **PR target** → Step 4 applies.
- **No argument**: review the current branch. Use `git status` and `git diff` (and `git diff
  <base>...HEAD` against the main branch) to determine the changed files and diff range. No GitHub
  calls needed. This is a **local target** → skip Step 4 (nothing to post to).

If resolution fails, stop and explain exactly what failed and the expected argument format.
If the arguments contain `report-only` (or `--no-post`), still do Steps 1–3 but skip Step 4.

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

Then output (to chat):

1. **Overall status** — `approve` / `request changes` / `needs investigation`, with a one-line reason.
2. **Prioritized findings** — each with `severity · file:line · issue · why it matters · minimal fix`,
   grouped by severity. Attribute the lens (code / convention / security) in brackets.
3. **Open questions** — anything that blocks confidence or needs the author's intent.

Keep it concise and actionable. Ignore pure style/formatting the toolchain already enforces
(`dotnet format`, analyzers, `TreatWarningsAsErrors`).

## 4. Publish the review to the PR (PR target only)

Run this **only** when Step 1 resolved an **existing, open PR** and `report-only` was not requested.
Skip it for a local-branch review (there is no PR to post to) or a closed/merged PR (note that in chat
instead). Push the findings to the PR via **GitHub MCP** using the pending-review flow so the feedback
lives on the thread, not just in chat:

1. **Create a pending review** — `mcp__github__pull_request_review_write` with `method: create` and
   **no `event`** (omitting `event` leaves the review pending). Pass `commitID` = the PR head SHA so
   comments anchor to the reviewed commit.
2. **Add one inline comment per finding that maps to the diff** — `mcp__github__add_comment_to_pending_review`
   with `path`, `line` (the finding's line), `side: RIGHT`, `subjectType: LINE`, and a body of
   `severity · [lens] · issue — why it matters — minimal fix`. Inline comments **must** target a line
   present in the PR diff; if a finding is on an unchanged line or a file outside the diff, do **not**
   force it inline — fold it into the summary body. If an inline add fails, drop that comment to the
   summary body rather than aborting.
3. **Submit** — `mcp__github__pull_request_review_write` with `method: submit_pending`, `event: COMMENT`,
   and `body` = the full ranked report from Step 3 (overall status + any findings that couldn't be
   inlined + open questions). Always submit (or `delete_pending` on error) — never leave a dangling
   pending review.

Rules:
- **`event: COMMENT` only** — never auto-`APPROVE` or `REQUEST_CHANGES`. State the recommendation in
  the body and let a human set the PR status.
- **Don't duplicate.** Using the existing review threads fetched in Step 1, skip any finding already
  raised. If every finding is already on the thread (or there are no findings), post nothing and say so.
- **Fail soft.** If GitHub MCP is unavailable or posting fails (e.g. the token lacks PR-write scope),
  report the failure and fall back to the chat-only report — never silently drop the findings.
- After posting, include the **review URL** in your chat summary.

## Notes

- The subagents are also usable directly without this command — `@code-reviewer`,
  "use the security-reviewer subagent on these changes", or via proactive delegation.
- This command never edits code. It posts review **comments** to the PR (Step 4); to act on the
  findings, follow up with `/fix-review-comments` (for a PR) or apply fixes manually.
