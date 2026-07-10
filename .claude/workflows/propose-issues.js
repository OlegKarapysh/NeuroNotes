export const meta = {
  name: 'propose-issues',
  description: 'Gather maximum context (code, roadmap, all issues/PRs, board, plans, git history, uncommitted artifacts) and return ranked GitHub issue candidates, each with its open questions and assumptions to clarify',
  phases: [
    { title: 'Scan', detail: 'code + roadmap + recent activity + issues/PRs/board (parallel)' },
    { title: 'Dedupe', detail: 'fetch existing open+closed issues from GitHub' },
    { title: 'Synthesize', detail: 'merge, dedupe, rank, surface open questions + assumptions' },
  ],
}

// ---- Schemas -------------------------------------------------------------

// One candidate. openQuestions/assumptions feed the skill's clarify phase so
// nothing gets created on a guess — every unknown becomes a question to the user.
const CANDIDATE = {
  type: 'object',
  additionalProperties: false,
  required: ['title', 'motivation', 'scope', 'acceptanceCriteria', 'roadmapRef', 'evidence', 'priority', 'estimate', 'openQuestions', 'assumptions'],
  properties: {
    title: { type: 'string', description: 'Concise, action-oriented issue title' },
    motivation: { type: 'string', description: 'Problem / why this matters now' },
    scope: { type: 'string', description: 'Proposed behavior or scope, 1-3 sentences' },
    acceptanceCriteria: { type: 'array', items: { type: 'string' }, description: 'Checklist of done conditions' },
    roadmapRef: { type: 'string', description: 'e.g. FR#12, NFR#3, TIER3-PLAN D1, docs/<file>, or "codebase-gap"' },
    evidence: { type: 'array', items: { type: 'string' }, description: 'file:line refs, doc anchors, commit/PR/issue numbers backing this' },
    priority: { type: 'string', enum: ['high', 'medium', 'low'] },
    estimate: { type: 'string', enum: ['S', 'M', 'L', 'XL'] },
    openQuestions: { type: 'array', items: { type: 'string' }, description: 'Decisions ONLY the maintainer can make: scope in/out, tradeoffs, product/UX choices, priority. Do not guess these.' },
    assumptions: { type: 'array', items: { type: 'string' }, description: 'Assumptions this candidate rests on that must be confirmed before creating.' },
  },
}

const SCAN_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  required: ['candidates'],
  properties: { candidates: { type: 'array', items: CANDIDATE } },
}

const EXISTING_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  required: ['issues', 'source'],
  properties: {
    source: { type: 'string', enum: ['github-mcp', 'gh-cli', 'unavailable'] },
    issues: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        required: ['number', 'title', 'state'],
        properties: {
          number: { type: 'number' },
          title: { type: 'string' },
          state: { type: 'string' },
          labels: { type: 'array', items: { type: 'string' } },
        },
      },
    },
  },
}

const FINAL = {
  type: 'object',
  additionalProperties: false,
  required: ['candidates', 'note'],
  properties: {
    note: { type: 'string', description: 'Truncation / coverage caveats, or empty string' },
    candidates: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        required: [...CANDIDATE.required, 'action', 'duplicateOf', 'rank'],
        properties: {
          ...CANDIDATE.properties,
          rank: { type: 'number', description: '1 = strongest / best next issue' },
          action: { type: 'string', enum: ['create', 'update'], description: '"update" when it maps onto an existing open issue' },
          duplicateOf: { type: 'number', description: 'Existing issue number this overlaps, or 0 if none' },
        },
      },
    },
  },
}

// ---- Scan targets --------------------------------------------------------

const focus = (typeof args === 'string' && args.trim()) ? `\n\nUser focus/hint for THIS run — prioritize candidates matching it: ${args.trim()}` : ''

const COMMON = `You are proposing GitHub issues for the NeuroNotes repo (a .NET 10 modular monolith Telegram bot; read CLAUDE.md and README.md for context).
Return ONLY concrete, actionable work that is NOT yet implemented, each backed by evidence (file:line, doc anchor, or issue/PR/commit number).
For EVERY candidate you MUST populate:
- openQuestions: decisions only the maintainer can make (scope boundaries, tradeoffs, priority, product/UX choices). Never guess these.
- assumptions: things you assumed that need confirming before an issue is created.
Do NOT propose vague "improve X" items, pure style nits, or already-done work. Prefer fewer high-value candidates. If nothing qualifies, return an empty candidates array.${focus}`

const SCAN_TARGETS = [
  { key: 'AudioProcessing', prompt: `${COMMON}\n\nScope: the AudioProcessing module (src/AudioProcessing). Gaps vs README audio requirements, unhandled error paths, TODO/FIXME markers, missing tests, provider/scaling seams (see docs/scalability-overview-*.md).` },
  { key: 'AiAssistant', prompt: `${COMMON}\n\nScope: the AiAssistant module (src/AiAssistant) + Persistence. Most roadmap value lives here (RAG, semantic search, tagging, backlinks, versioning). Cross-reference README FR# against actual code; flag unimplemented roadmap features and gaps in NoteService/PostgresNoteStore/PostgresTagStore.` },
  { key: 'GitHub', prompt: `${COMMON}\n\nScope: the GitHub module (src/GitHub). Plaintext-token storage roadmap item, error handling/retries in Octokit publishers, missing tests.` },
  { key: 'TelegramBot', prompt: `${COMMON}\n\nScope: the TelegramBot module (src/TelegramBot). Command/state machine, commands implied by the roadmap but missing, untrusted-input handling, menu/UX gaps.` },
  { key: 'WebApi-Infra', prompt: `${COMMON}\n\nScope: host + cross-cutting (src/NeuroNotes.WebApi, Dockerfile, CI, migrations, config). Operational/NFR gaps: observability, health checks, resilience, deployment, scalability.` },
  { key: 'roadmap-FR', prompt: `${COMMON}\n\nScope: README.md "## Functional Requirements". For each FR# not yet built, propose a decomposed, buildable issue; reference the FR number in roadmapRef and cite where in code it is (or isn't) implemented.` },
  { key: 'roadmap-NFR', prompt: `${COMMON}\n\nScope: README.md "## Non-Functional Requirements". Propose issues for unmet NFRs (performance, security, reliability, observability, extensibility); reference the NFR number.` },
  { key: 'internal-plans', prompt: `${COMMON}\n\nScope: internal plans — .claude/TIER3-PLAN.md, CLAUDE.md "not built yet" notes, and the persistent agent memory for this repo: glob \`~/.claude/projects/*NeuroNotes*/memory/*.md\` under the home directory (e.g. the agent-tooling-adoption plan) and read what matches; skip silently if absent. Propose issues for still-pending workstreams; reference the plan section or memory file.` },
  // NEW: freshest signal — recent commits, uncommitted work, and new artifacts often point at the real next step.
  { key: 'recent-activity', prompt: `${COMMON}\n\nScope: the CURRENT working state, which is the freshest signal of intent. Use Bash to run: \`git log --oneline -20\`, \`git status --porcelain\`, and \`git diff --stat HEAD~5\`. Read the docs/ folder (especially any scalability/design docs) and any uncommitted or newly added artifacts (e.g. *.json reports, new *.md). Propose issues that continue or formalize whatever is actively in progress. Cite commit hashes / file paths as evidence.` },
  // NEW: open PRs + project board — avoid proposing something already in flight.
  { key: 'prs-and-board', prompt: `${COMMON}\n\nScope: work already IN FLIGHT. Load GitHub tools via ToolSearch (query "select:mcp__github__list_pull_requests,mcp__github__list_issues,mcp__github__projects_list") and list OPEN pull requests and the project board for OlegKarapysh/NeuroNotes; fall back to \`gh pr list\` / \`gh project\` via Bash. Do NOT re-propose work covered by an open PR. Instead, propose concrete FOLLOW-UPS that those PRs imply (e.g. tests, docs, a deferred acceptance-criterion). Cite PR numbers as evidence.` },
]

// ---- Run -----------------------------------------------------------------

phase('Scan')
const scans = await parallel(SCAN_TARGETS.map(t => () =>
  agent(t.prompt, { label: `scan:${t.key}`, phase: 'Scan', schema: SCAN_SCHEMA })
))
const rawCandidates = scans.filter(Boolean).flatMap(s => s.candidates || [])
log(`Scan produced ${rawCandidates.length} raw candidates across ${SCAN_TARGETS.length} context areas.`)

phase('Dedupe')
const existing = await agent(
  `Fetch EXISTING issues for the GitHub repo OlegKarapysh/NeuroNotes so we can avoid proposing duplicates.
Prefer the GitHub MCP server: load tools with ToolSearch (query "select:mcp__github__list_issues,mcp__github__search_issues"), then list issues with state=all, paginating in batches of ~50 up to ~200 issues. Use minimal output (number, title, state, labels).
If the GitHub MCP tools are unavailable or error, fall back to Bash: \`gh issue list --repo OlegKarapysh/NeuroNotes --state all --json number,title,state,labels --limit 200\`.
Set "source" to which path worked ("github-mcp", "gh-cli", or "unavailable" if both fail). Return the issues array (empty if none / unavailable).`,
  { label: 'fetch-existing', phase: 'Dedupe', schema: EXISTING_SCHEMA }
)
log(`Existing issues fetched via ${existing?.source ?? 'unknown'}: ${existing?.issues?.length ?? 0}.`)

phase('Synthesize')
const final = await agent(
  `You are consolidating proposed GitHub issue candidates for OlegKarapysh/NeuroNotes.

RAW CANDIDATES (may overlap each other):
${JSON.stringify(rawCandidates, null, 2)}

EXISTING ISSUES (source: ${existing?.source ?? 'unavailable'}):
${JSON.stringify(existing?.issues ?? [], null, 2)}

Do the following:
1. Merge near-duplicate candidates into one (union their acceptanceCriteria, evidence, openQuestions, and assumptions).
2. For each survivor, compare to EXISTING issues. If it clearly overlaps an OPEN existing issue → action="update", duplicateOf=<number>. If it matches a CLOSED issue (already done/rejected) → DROP it. Otherwise action="create", duplicateOf=0.
3. Rank by value/effort and urgency given the current working state; set rank (1 = best next issue).
4. Keep the strongest ~15-25; if you drop lower-value ones, say so in "note".
5. PRESERVE every candidate's openQuestions and assumptions (deduped) — the caller will ask the maintainer about them before creating anything. Do NOT invent answers.

Return the final ranked list.`,
  { label: 'synthesize', phase: 'Synthesize', schema: FINAL }
)

return { ...final, existingSource: existing?.source ?? 'unavailable', rawCount: rawCandidates.length }
