---
description: Groom the backlog — propose a ranked batch of issues with maximum context, clarify each, then create the approved ones
argument-hint: <optional focus, e.g. "NFRs only" or "TelegramBot">
---

Use the **propose-issues** skill in **batch** mode with focus:

**$ARGUMENTS**

Expected behavior (same clarify-first, never-guess rules as `/next-issue`, applied to many):

1. Run the `propose-issues` Workflow to gather **maximum context** (code, README FR#/NFR#, all
   open+closed issues and PRs, project board, plans/memory, recent git history, uncommitted/fresh
   artifacts) and return a ranked, deduped candidate list.
2. Present the ranked candidates — nothing created yet.
3. For each candidate you choose to act on, **clarify everything** (structured + open-chat) until no
   open question or assumption remains.
4. Draft each issue, get your approval, then create it via **manage-github-issues** (GitHub MCP first,
   `gh` CLI fallback; update an existing issue instead of duplicating).

> For a single decisive "what should I do next" pick, use `/next-issue` instead.
