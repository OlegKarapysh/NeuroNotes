---
description: Suggest, create, or update GitHub issues from discussion context
argument-hint: <discussion-summary-or-follow-up>
---

Use the **manage-github-issues** skill with context:

**$ARGUMENTS**

Expected behavior:

1. Search existing issues for similar topics first.
2. Prefer updating a highly similar issue to avoid duplicates.
3. If no similar issue exists, create a new issue with clear acceptance criteria.
4. Execute issue mutations by trying GitHub MCP first and automatically falling back to gh CLI.
