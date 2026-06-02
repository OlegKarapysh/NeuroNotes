---
description: Review a pull request and summarize actionable feedback
argument-hint: <pr-number-or-link>
---

Review pull request **$ARGUMENTS**.

Treat the argument as either:
- a PR number (for this repository), or
- a full GitHub PR URL.

Do the following:

1. Resolve owner/repo/PR number from the argument.
2. Fetch PR details, changed files, checks, and review threads/comments via GitHub MCP tools.
3. Focus only on meaningful issues:
   - bugs and incorrect behavior
   - security risks
   - regressions in tests/build
   - major maintainability problems that can cause defects
4. Ignore minor style or formatting nits unless they hide a real defect.
5. Produce a concise review report with:
   - overall status (approve / request changes / needs investigation)
   - prioritized findings with file paths and rationale
   - concrete, minimal fix suggestions
   - open questions that block confidence

If data retrieval fails, explain exactly what failed and what input format is expected.
