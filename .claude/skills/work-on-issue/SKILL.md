---
name: work-on-issue
description: >
  Take a GitHub issue end-to-end: fetch it via the GitHub MCP server, open a draft PR whose
  description is the implementation plan, move the issue to "In Progress" on the GitHub project
  board, implement it following repo conventions, verify with a full build + test run, commit,
  push, and mark the PR ready for review. Use this whenever the user asks to work on, implement,
  fix, pick up, or resolve a GitHub issue by number (e.g. "work on issue 42", "implement #17",
  "/work-on-issue 42"), or asks to turn an issue into a pull request.
argument-hint: <issue-number>
---

# Work on a GitHub Issue

Drive one GitHub issue from "picked up" to "PR ready for review" in **OlegKarapysh/NeuroNotes**.

The issue number is **required**: `$ARGUMENTS` must contain it. If it doesn't, stop and ask the
user which issue to work on — never guess or auto-pick, because starting work moves the issue on
the project board and opens a PR, which are visible side effects the user must intend.

All GitHub interaction goes through the **GitHub MCP server** (`mcp__github__*` tools). The exact
tool names vary by server version, so load what you need with ToolSearch (e.g.
`select:mcp__github__get_issue` or a keyword search like `+github pull request create`) and match
on capability, not memorized names. Local git operations (branch, commit) use the normal `git` CLI.

## 0. Preflight

Cheap checks first, so a missing prerequisite fails before anything visible happens:

1. **GitHub MCP available?** Run a trivial read (e.g. get the authenticated user or the issue
   itself). If no `mcp__github__*` tools exist or auth fails, stop and tell the user: the server
   is configured in [.mcp.json](.mcp.json) and reads a PAT from the `GITHUB_PAT` environment
   variable (needs `repo` + `project` scopes) — they likely need to set it and restart the session.
2. **Clean working tree.** `git status --porcelain` must be empty. Uncommitted work would get
   tangled into the issue branch; ask the user what to do with it rather than stashing silently.
3. **Fresh base.** `git checkout main && git pull` so the branch starts from the latest main.

## 1. Fetch and understand the issue

Get the issue (title, body, labels, comments — comments often contain decisions that supersede
the body). If the issue is closed or already has a linked open PR, stop and tell the user instead
of duplicating work.

## 2. Plan before touching code

Explore the codebase enough to write a concrete plan: which modules, which files, what new types,
what tests. CLAUDE.md describes the architecture (modular monolith, MassTransit commands, the
`*.Public`/`*.Application`/`*.Infrastructure` split) — for a new Telegram command or module, the
`/new-command` and `/new-module` skills document the exact file-by-file recipe; follow the same
steps even if you don't invoke them.

Write the plan as the future PR description, in this shape:

```markdown
Closes #<issue-number>

## Summary
<one paragraph: what this PR does and why>

## Implementation plan
- [ ] <step — file/module level, concrete>
- [ ] <step>
- [ ] Build + tests green (`dotnet build`, `dotnet test`)
```

`Closes #<n>` matters — it links the PR to the issue so GitHub closes it on merge and shows the
connection on the board.

## 3. Branch and open the draft PR

A PR needs at least one commit, so bootstrap the branch before implementing:

1. Branch: `git checkout -b feat/issue-<n>-<short-slug>` (use `fix/` for bug issues).
2. Empty commit: `git commit --allow-empty -m "chore: start work on #<n>"`.
3. Push: `git push -u origin <branch>`.
4. Create the PR via GitHub MCP: **draft**, base `main`, title from the issue (imperative,
   `<type>: <summary> (#<n>)`), body = the plan from step 2.

Opening it as a draft with the plan up front means the user can read (and object to) the approach
while implementation is still cheap to change.

## 4. Move the issue to "In Progress"

The board is a **Projects v2 board owned by the `OlegKarapysh` user account**. Via GitHub MCP
project tools:

1. List the user's projects and find the one containing this issue (list its items, or add the
   issue if it belongs there but is missing).
2. Set the item's **Status** field to **In Progress** (match the option name case-insensitively).

If no project or matching item is found, don't fail the whole run — note it in the final summary
and continue. Board hygiene is secondary to shipping the fix.

## 5. Implement

Do the work the plan describes, following CLAUDE.md conventions: `Result<T>` for expected
failures, options pattern for config, sealed records + `IConsumer` handlers for Telegram commands,
file-scoped namespaces, primary constructors. Add unit tests in the module's own test project —
pure tests, hand-written fakes, no mocking library.

If implementation reveals the plan was wrong, update the PR description so it stays truthful —
it's the reviewer's map of the change.

## 6. Verify

From the repo root, all three must pass (the build treats warnings as errors — fix them, don't
suppress):

```
dotnet build NeuroNotes.slnx
dotnet test --solution NeuroNotes.slnx
dotnet format NeuroNotes.slnx --verify-no-changes
```

If `--verify-no-changes` fails, run `dotnet format NeuroNotes.slnx` and include the result in the
commit. Don't proceed past a red build or failing test — fix or, if truly blocked, report honestly
and leave the PR as a draft.

## 7. Commit and push

1. Stage and commit with a conventional message describing the change (not "address issue"):
   `feat: add tag support to notes (#42)`. Body: a short summary of what changed and why.
2. `git push`.
3. Tick the completed checkboxes in the PR description (update the PR body via MCP).
4. Mark the PR **ready for review** via GitHub MCP — only if build and tests are green.

## 8. Report

End with a short summary: issue link, PR link, board status change, build/test results, and
anything skipped or left for the reviewer to decide.
