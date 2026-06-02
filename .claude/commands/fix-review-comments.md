---
description: Address pull request review comments with minimal verified fixes
argument-hint: <pr-number-or-link>
---

Fix review feedback for pull request **$ARGUMENTS**.

Treat the argument as either:
- a PR number (for this repository), or
- a full GitHub PR URL.

Workflow:

1. Resolve owner/repo/PR number from the argument.
2. Read review threads/comments and identify unresolved, actionable feedback.
3. For each valid comment:
   - reproduce and understand the issue
   - make the smallest complete code/test change needed
   - keep unrelated code untouched
4. Validate changes with targeted tests/build for affected areas.
5. Summarize what was fixed, what was intentionally not changed, and why.
6. If a comment is unclear or invalid, explain the reason and note the exact thread/comment.

Always prioritize correctness and security over cosmetic edits.
