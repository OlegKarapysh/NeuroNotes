---
name: manage-github-issues
description: >
  Turn project discussions into actionable GitHub issues: detect similar existing issues first,
  then create or update issues without asking which integration to use. Prefer GitHub MCP and
  automatically fall back to gh CLI when needed.
argument-hint: <discussion-summary-or-follow-up>
---

# Manage GitHub Issues from Discussion

Use this skill when a user discusses ideas, bugs, TODOs, follow-ups, or roadmap items that should
be tracked as GitHub issues in **OlegKarapysh/NeuroNotes**.

## Goal

1. Suggest issue tracking when the conversation implies actionable follow-up work.
2. Before creating anything, check for similar existing issues.
3. If similar issues exist, suggest updating an existing issue instead of creating a duplicate.
4. If no good match exists, create a new issue.
5. Perform issue mutations using tool fallback automatically: **GitHub MCP first, gh CLI second**.

## Workflow

### 0. Confirm intent level

- If the user clearly asked to create or update an issue, execute directly.
- If the user is only discussing ideas, suggest creating/updating an issue and present the best
  candidate action.

### 1. Search for similar issues before creation (required)

Search open issues in this repository using multiple focused queries from the discussed topic:

- Key nouns / feature names
- Error text / command names / module names
- Short phrase from the proposed title

Use GitHub MCP issue search/list tools first. If unavailable, use `gh issue list --search`.

Classify matches:

- **High similarity**: same problem/feature scope and expected outcome
- **Partial similarity**: related area but materially different outcome
- **No useful match**

### 2. Decide create vs update

- **High similarity found** → suggest updating that issue (or do it immediately if user requested
  action), and explain why it is the best existing thread.
- **Only partial similarity** → suggest either creating a new issue with links to related ones, or
  updating one if the user wants to consolidate.
- **No useful match** → create a new issue.

### 3. Execute using integration fallback (do not ask which to use)

Try in this order:

1. **GitHub MCP** (`mcp__github__*` issue tools)
2. **gh CLI** (`gh issue create`, `gh issue edit`, `gh issue comment`)

Rules:

- Do not ask the user whether to use MCP or gh.
- If MCP write fails (missing capability/auth/error), automatically try gh CLI.
- Stop after the first successful mutation to avoid duplicates.
- If both fail, report both failures concisely with next-step guidance.

### 4. Issue quality requirements

When creating/updating, include concrete and reusable structure:

- Problem / motivation
- Proposed behavior or scope
- Acceptance criteria (checklist)
- Extra context (links, constraints, related issue numbers)

Keep titles concise and action-oriented.

### 5. Response format

Always end with:

- What action was taken (suggested / created / updated)
- Similar issues considered (with brief rationale)
- Final issue link(s) or issue number(s)
- Any fallback used (MCP vs gh CLI)
