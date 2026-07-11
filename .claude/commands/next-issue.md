---
description: Decide the single best next GitHub issue with maximum context, clarify every detail, then create it after you approve a draft
argument-hint: <optional focus, e.g. "AiAssistant RAG" or "scalability">
---

Use the **propose-issues** skill in **next** mode with focus:

**$ARGUMENTS**

Expected behavior (minimize guesswork, maximize accuracy):

1. Run the `propose-issues` Workflow to gather **maximum context** — module code, README FR#/NFR#, all
   open+closed issues and PRs, the project board, TIER3-PLAN + memory, recent git history, and
   uncommitted/fresh artifacts.
2. Present the single highest-value next issue (plus runner-ups) — nothing created yet.
3. **Clarify everything**: ask structured (multiple-choice) questions for scope/priority/labels/
   milestone/design decisions/board placement, and open-chat questions to confirm each assumption.
   Do not proceed while any unknown remains.
4. Draft the full issue, iterate until you approve it, then create it via **manage-github-issues**
   (GitHub MCP first, `gh` CLI fallback; update an existing issue instead of duplicating).
