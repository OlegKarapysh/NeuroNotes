# CLAUDE.md

Guidance for Claude Code (and other AI agents) working in this repository.
Keep this file accurate — when you change a convention, build command, or module boundary, update it here.

## What NeuroNotes is

A **.NET 10 modular monolith** — a **platform that hosts multiple Telegram bots**, added and managed at runtime
through an operator-only admin API, with no restart or redeploy. The original, still-default experience is a
voice-first personal knowledge base: a user sends a voice or text message → audio is transcribed (Whisper) → an
LLM cleans it up → the user can edit it, ask questions about their notes, or save it as a Markdown note. That
experience is now one **behavior** (`note-capture`) a bot can be assigned; the platform can also load
operator-supplied **behavior extensions** (compiled plugins) to run other kinds of bots side by side.

The long-term product vision (RAG, semantic search, backlinks, versioning, event sourcing) lives in
[README.md](README.md) as functional/non-functional requirements. Most of it is **not built yet** — treat the
README as a roadmap, not a description of current behavior. The multi-bot platform itself is specified in
[specs/001-multi-bot-platform](specs/001-multi-bot-platform) (spec, plan, data model, contracts).

## Commands

All commands run from the repo root. The build requires the **.NET 10 SDK**.

| Task | Command |
|------|---------|
| Restore | `dotnet restore NeuroNotes.slnx` |
| Build (Debug) | `dotnet build NeuroNotes.slnx` |
| Build (Release, as CI does) | `dotnet build NeuroNotes.slnx --configuration Release` |
| Run all tests | `dotnet test --solution NeuroNotes.slnx` |
| Run one module's tests | `dotnet test --project src/<Module>/NeuroNotes.<Module>.UnitTests/NeuroNotes.<Module>.UnitTests.csproj` |
| Format to conventions | `dotnet format NeuroNotes.slnx` |
| Run the bot (Web host) | `dotnet run --project src/NeuroNotes.WebApi` |

> The build uses **`TreatWarningsAsErrors`** — a new warning fails the build. Fix the warning; don't suppress it
> unless it's a known false positive (see `NoWarn` in [Directory.Build.props](Directory.Build.props)).

### Running the bot locally
Every bot's Telegram delivery is owned by the `Platform` module's `BotSupervisor`: in `Development` it runs a
**long-polling** receive loop per bot; in other environments it registers each bot's own **webhook**
(`/telegram-bot/webhook/{botId}`). You need an OpenAI API key and a `Platform:AdminApiKey` in user secrets (see
Configuration); a legacy `Telegram:TelegramBotSecretToken` is optional and only used once, to seed the first bot
(see below). `ffmpeg` must be on `PATH`, and a Whisper model file (`ggml-base.bin`) must be present.
Start the local Postgres with `docker compose up -d` ([docker-compose.yml](docker-compose.yml)): first put
`POSTGRES_PASSWORD=...` in a `.env` file at the repo root (gitignored; see [.env.example](.env.example)), then set
the matching `Persistence:ConnectionString` (incl. that password) in user secrets. Apply migrations with
`dotnet run --project src/NeuroNotes.WebApi -- migrate`. To add further bots at runtime once the host is running,
use the admin API (`POST /admin/bots` — see [contracts/admin-api.md](specs/001-multi-bot-platform/contracts/admin-api.md)),
authenticated with `Platform:AdminApiKey` via `Authorization: Bearer <key>` or `X-Admin-Api-Key`.

## Architecture

A host (`WebApi`) composes the feature modules over an **in-memory MassTransit bus**. The platform hosts many
bots at once; every update is tagged with its owning bot and routed to that bot's assigned **behavior**:

```
Telegram → PollingBotReceiver | WebhookBotReceiver     // per bot; Platform.Infrastructure
        → publish BotUpdate(botId, update)
        → BotUpdateRouter (IConsumer<BotUpdate>)        // resolves the bot's behavior via IBehaviorCatalog
        → IBotBehavior.HandleUpdateAsync                // e.g. the built-in NoteCaptureBehavior, or a loaded extension
              → CommandDispatcher                       // (note-capture only) state machine: routes & validates
              → sends a Command (ProcessVoice/ProcessText/PreviewNote/ConfirmNote/EditTranscription/PushNoteToGitHub/ConnectGitHub)
                — each command carries the owning BotId (IBotScopedMessage)
              → <Command>Handler (IConsumer<TCommand>)  // does the work, replies to the user via that bot's client
```

A MassTransit consume filter (`BotScopeFilter<T>`, applied to every `IBotScopedMessage`) sets the current bot in a
scoped `IBotContext` **before** the consumer is constructed, so a plain constructor-injected `ITelegramBotClient`
in any command handler resolves to *that* bot's client — handlers did not need to change to become bot-aware.

### Modules

Each feature module is split into projects with a strict dependency direction:

- **`*.Public`** — interfaces and shared contracts only. No logic. Other modules depend **only** on this.
- **`*.Application`** — business logic (services, command handlers, domain types).
- **`*.Infrastructure`** — external concerns + DI wiring (`ServiceInstaller`), config options.
- **`*.Persistence`** (storage-owning modules only) — the module's own EF Core `DbContext`, entities,
  repositories, and migrations. Infrastructure-only; depends **solely** on the module's `*.Public`. Wired in via
  `Add<Module>Persistence()`, which the module's `Add<Module>Module()` calls. **There is no shared persistence
  module** — each module owns its data (see below).

| Module | What it does | Notable types |
|--------|--------------|---------------|
| [AudioProcessing](src/AudioProcessing) | OGG→WAV (FFmpeg) then speech-to-text (Whisper.net, local `ggml-base.bin`) | `VoiceTranscriber`, `VoiceEnhanceTranscriber`, `WhisperSpeechRecognizer`, `FFmpegAudioConverter` |
| [AiAssistant](src/AiAssistant) | LLM features via **Semantic Kernel + OpenAI**; owns its `ai_assistant` schema; every store call is `(botId, userId, …)`-scoped | `SpeechTextEnhancer` (clean transcripts), `NoteAssistant` (Q&A over notes), `NoteTextEditor`, `NoteService`; `AiAssistantDbContext`, `PostgresNoteStore`, `PostgresTagStore` |
| [GitHub](src/GitHub) | Commits notes as Markdown files to a user's GitHub repo via **Octokit**; owns its `github` schema; settings are `(botId, userId)`-scoped | `GitHubRepositoryReference`, `OctokitGitHubAccountLinker`, `OctokitGitHubNotePublisher`; `GitHubDbContext`, `PostgresUserGitHubSettingsStore` |
| [TelegramBot](src/TelegramBot) | The **note-capture behavior**: chat state machine, menus, and the command handlers that back it; owns its `telegram_bot` schema (`(botId, chatId)`-scoped) | `NoteCaptureBehavior` (the `IBotBehavior` bridging into...), `CommandDispatcher`, `ChatState`/`ChatStateCommandsMap`, `MenuKeyboardFactory`, the `Commands/*` handlers (each command is `IBotScopedMessage`). `*.Public` holds the `ChatState` enum + `IChatStateStore`/`ILastTranscriptionStore` |
| [Platform](src/Platform) | Hosts the bot fleet: durable bot registry, per-bot receivers/clients, behavior catalog + plugin loading, admin API; owns its `platform` schema | `BotRegistrationService`, `BotSupervisor`, `BotUpdateRouter`, `BehaviorCatalog`, `ExtensionAssemblyLoader`, `AdminEndpoints`; `PlatformDbContext`, `PostgresBotRegistry`. `*.Public` holds `IBotBehavior`/`IBotUpdateContext` (the plugin SDK), `IBotRegistry`, `BotUpdate`/`IBotScopedMessage` |
| [WebApi](src/NeuroNotes.WebApi) | Host: composes modules, wires MassTransit (incl. the bot-scope filter), registers the built-in behavior + reloads saved extensions, maps the admin API and per-bot webhook, applies every module's migrations + the legacy-bot seed on `migrate` | `Program.cs`, `ServiceInstaller` |

### State: durable data in Postgres (per-module, per-bot), chat session state in memory
**Durable user data lives in Postgres**, but **each module owns its own `DbContext` and its own Postgres schema**
on the single shared `neuronotes` database — there is no shared persistence module. The contexts:
`PlatformDbContext` (schema `platform`: `PostgresBotRegistry` — the durable bot fleet + Data Protection key ring),
`AiAssistantDbContext` (schema `ai_assistant`: `PostgresNoteStore`, `PostgresTagStore`), `GitHubDbContext`
(schema `github`: `PostgresUserGitHubSettingsStore`), and `TelegramBotDbContext` (schema `telegram_bot`:
`PostgresChatStateStore`, `PostgresLastTranscriptionStore`). Each schema gets its own `__EFMigrationsHistory`
table (EF places it in the context's default schema), so module migration histories never collide. All
repositories are registered **scoped**, and each context is also registered as a base `DbContext` so the host's
`migrate` command can apply every module's migrations via `GetServices<DbContext>()`.

**Every tenant-owned row carries a `BotId`.** `ChatState`/`LastTranscription` use a composite `(BotId, ChatId)`
key; `UserGitHubSettings` uses `(BotId, UserId)`; `Note`/`Tag` add a `BotId` column (indexed/unique alongside
`UserId`). Store interfaces take an explicit leading `long botId` parameter (never an ambient/ EF global-filter
lookup), so a module's `*.Persistence` project still depends **solely** on its own `*.Public` — `Platform.Public`
is never referenced from another module's persistence layer. This is what makes one bot's data invisible to
another bot even though every bot's rows live in the same shared database.

Schema changes go through **per-context** EF migrations — point `--project`/`--startup-project` at the owning
module's `*.Persistence` project and name its `--context`, e.g. for AiAssistant:
```
dotnet dotnet-ef migrations add <Name> \
  --project src/AiAssistant/NeuroNotes.AiAssistant.Persistence/NeuroNotes.AiAssistant.Persistence.csproj \
  --startup-project src/AiAssistant/NeuroNotes.AiAssistant.Persistence/NeuroNotes.AiAssistant.Persistence.csproj \
  --context AiAssistantDbContext
```
(swap in `NeuroNotes.GitHub.Persistence`/`GitHubDbContext`, `NeuroNotes.TelegramBot.Persistence`/
`TelegramBotDbContext`, or `NeuroNotes.Platform.Persistence`/`PlatformDbContext` for the other modules; the
`dotnet-ef` local tool is pinned in `.config/dotnet-tools.json`). The deploy workflow applies migrations with a
one-off `migrate` container before each rollout; locally use `dotnet run --project src/NeuroNotes.WebApi -- migrate`.
Both apply **all** modules' migrations in one pass against the single connection string
(`Persistence:ConnectionString`), followed by a one-time **legacy-bot seed**: if `Telegram:TelegramBotSecretToken`
is set and no bot is registered yet, it's validated, encrypted, and inserted as the first `BotRegistration`
(behavior `note-capture`), and every pre-existing row (added with a temporary `BotId = 0` default by each
module's migration) is backfilled to that bot's id via `ExecuteUpdateAsync` — see `SeedLegacyBotAsync` in
`WebApi/Program.cs`. This runs at most once (it no-ops once any bot is registered), so existing users, chat
state, notes, tags, and GitHub links carry over unchanged (no restart-time cost for later `migrate` runs).

**Bot tokens are encrypted at rest** via `ITokenProtector` (ASP.NET Core Data Protection, key ring persisted in
`platform.DataProtectionKeys`) — never stored in plaintext, never logged. **Two singleton in-memory stores hold
transient between-prompt scratch state** (both fine to lose on restart, both now keyed by `(botId, chatId)`):
`PendingGitHubLinkStore` holds the half-finished GitHub-link input between the two onboarding prompts, and
`PendingNoteStore` holds the generated note being previewed between the preview step and the user's confirmation
(see the note-creation flow below). GitHub **access tokens are still stored in plaintext in the database** (the
bot deletes the token message from the chat after reading it) — that pre-existing gap is unchanged and MUST NOT
be widened; `ITokenProtector` is the seam through which it can later be closed.

### Multi-bot platform: registering, behaviors, and plugins
The operator manages the bot fleet through an **authenticated admin API** (`Platform.Infrastructure.AdminEndpoints`,
mapped at `/admin/*`, separate from the end-user Telegram surface) — register/list/get/disable/enable/rotate a
bot's token/remove, plus list/upload behaviors. Every request needs the configured `Platform:AdminApiKey` via
`Authorization: Bearer <key>` or `X-Admin-Api-Key` (SEC-005). `BotRegistrationService` validates a candidate bot
(behavior exists in the catalog, Telegram accepts the token via `IBotTokenValidator`) **before** it ever touches
the registry or starts a receiver, so a bot is either fully registered and running or not registered at all.

A bot's **behavior** (`IBotBehavior`, in `Platform.Public` — the plugin SDK) determines what it does; the built-in
`note-capture` behavior (`NoteCaptureBehavior`) bridges into the TelegramBot module's existing command flow. The
operator can load an entirely new behavior type at runtime by uploading a compiled assembly to
`POST /admin/behaviors`: `ExtensionAssemblyLoader` loads it into its own **collectible `AssemblyLoadContext`**
(sharing `NeuroNotes.Platform.Public`/`Telegram.Bot` with the host so types unify) and discovers its
`IBotBehavior` implementations; `BehaviorCatalog.Register` rejects a contract-version mismatch or a colliding
`Key` without affecting anything already running (FR-006). `PluginStore` persists the uploaded `.dll` under
`Platform:PluginsDirectory` so it reloads automatically on the next startup (see `Program.cs`).

`BotSupervisor` starts/stops each bot's receiver (long-polling in `Development`, a per-bot webhook — with a
deterministically-derived secret token, no extra storage — otherwise) and is the only thing `BotRegistrationService`
touches to bring a bot up or down (via the `IBotLifecycle` seam, keeping `Application` free of a direct
`Infrastructure` reference). `BotHealthTracker` flips a repeatedly-failing bot's status to `Failing` after a few
consecutive errors — the receiver keeps running at its normal cadence (no backoff, no auto-disable, FR-022) — and
clears it back to `Active` on the next success; a `Disabled` bot is never touched. A sample plugin lives at
[samples/EchoBehavior](samples/EchoBehavior) for trying the upload flow end-to-end.

**Note creation is a preview → confirm flow.** From `HasTranscription`/`AwaitingEditPrompt`, **📝 Create note**
sends `PreviewNoteCommand`, which calls `INoteService.GenerateNote` (LLM formatting + auto-tagging, **no save**),
caches the result in `PendingNoteStore`, and moves the chat to `ChatState.PreviewingNote`. There the user taps
**✅ Confirm & save** (`ConfirmNoteCommand` → `INoteService.SaveNote`, sends the `.md`, reports the applied tags,
→ `Initial`), **✏️ Edit text** (→ `AwaitingEditPrompt`), or **❌ Cancel** (→ `HasTranscription`). Confirm persists
exactly what was previewed; if the cache was lost it regenerates from the stored transcription.
`GenerateNote`/`SaveNote` are split precisely so preview and save can't diverge — `PushNoteToGitHub` uses both
(generate → save → publish).

**Notes are auto-tagged with the user's existing tags.** As part of `GenerateNote`, `TagSuggester` picks the
fitting tags from the user's tags (`ITagStore`) via **structured JSON output** — existing tags only, never
invents one — and `NoteService` writes them into the note's YAML front matter (`tags:`). So the preview already
shows the tags, and they flow into the saved `.md` and the GitHub push. On save, `PostgresNoteStore` also records
a durable note↔tag association in the `ai_assistant.NoteTags` join table. Tagging is **best-effort**: any
LLM/parse failure just yields a note with no tags, never a failed creation.

## Conventions (match these — the codebase is consistent)

- **Result, not exceptions, for expected failures.** Public methods that can fail return `FluentResults.Result<T>`.
  Check `.IsFailed` / `.Errors.First().Message`; return `new Error("...")` on failure. Reserve `throw` for
  programmer errors / invariants.
- **DI registration uses C# extension members**, not classic extension methods:
  ```csharp
  public static class ServiceInstaller
  {
      extension(IServiceCollection services)
      {
          public IServiceCollection AddXyzModule() { /* ... */ return services; }
      }
  }
  ```
  Each module exposes one `Add<Module>Module(...)` entry point, composed in `WebApi/Program.cs`.
- **Options pattern** for all config: `services.AddOptions<TOptions>().BindConfiguration(TOptions.SectionName)
  .ValidateDataAnnotations().ValidateOnStart();`. Options are `record`s with a `const string SectionName` and
  `[Required]`/`[Range]` data annotations.
- **Telegram commands are MassTransit messages.** A command is a `sealed record` implementing
  `IBotScopedMessage` (a leading `long BotId` property, from `Platform.Public`); its handler is a
  `sealed class ... : IConsumer<TCommand>`. Both usually live in one file under `TelegramBot.Application/Commands/`.
- **C# style:** file-scoped namespaces, `sealed` by default, primary constructors, `var`, expression-bodied
  members where they fit, Allman braces, private fields `_camelCase`. Per-module `GlobalUsings.cs` holds the common
  usings — prefer adding there over repeating `using`s. See [.editorconfig](.editorconfig).
- **Nullable reference types are enabled** solution-wide; keep code null-clean.

### Adding a new Telegram command (the most common task)
1. Add `MyThingCommand(long BotId, Message Message, …) : IBotScopedMessage` (record) + `MyThingCommandHandler`
   (`IConsumer`) in `TelegramBot.Application/Commands/`. Any store/service call inside the handler passes
   `context.Message.BotId` through as the leading argument (see `INoteStore`/`ITagStore`/`IChatStateStore`/etc.).
2. Register the endpoint in `MapTelegramCommandEndpoints` (`TelegramBot.Infrastructure/ServiceInstaller.cs`) —
   the queue name is `nameof(MyThingCommandHandler).ToKebabCase()`.
3. Allow it from the right state(s) in `Menus/ChatStateCommandsMap.cs`.
4. Dispatch to it from `CommandDispatcher` (via `DispatchIfAllowed`, passing the ambient `botId` through), adding
   a menu button in `MenuButtons` / `MenuKeyboardFactory` if the user triggers it from the keyboard.
5. Add unit tests in that module's test project, `src/TelegramBot/NeuroNotes.TelegramBot.UnitTests` (the
   state-map and keyboard parts are pure and easy to test).

## Tests

Tests live under each module's own `src/<Module>/` directory, **one project per module**
(`NeuroNotes.<Module>.UnitTests`), each referencing **only** its own module's project(s).

- **Stack: xUnit v3 on [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro) (MTP).**
  Test projects reference `xunit.v3.mtp-v2` and are executables (`<OutputType>Exe</OutputType>`).
  `dotnet test` runs in **MTP mode**, enabled by [global.json](global.json) (`"runner": "Microsoft.Testing.Platform"`).
  Because of MTP mode, pass the target explicitly: `dotnet test --solution NeuroNotes.slnx` or
  `dotnet test --project <test>.csproj` (the legacy positional `dotnet test <path>` no longer applies).
- Keep tests **pure** — no network, LLM, Whisper, filesystem, or real database. Replace collaborators with small
  hand-written fakes implementing the module's interfaces. The repo has no mocking library on purpose; don't add
  one unless a test genuinely needs it. One sanctioned exception: repository tests use the **EF Core in-memory
  provider** to exercise a module's `*.Persistence` stores (still no I/O) — each storage-owning module's test
  project (e.g. `NeuroNotes.AiAssistant.UnitTests`) has its own `InMemoryDbContextFactory` for this.
- Add a feature to a module → add its tests to that module's project. New module → new test project, registered
  in `NeuroNotes.slnx`. (`/new-module` scaffolds this.)

## Subagents (`.claude/agents/`)

Specialized subagents live in [.claude/agents](.claude/agents).

Three **read-only** review subagents — each reviews a diff through one lens and never edits code:

- **`code-reviewer`** — correctness, edge cases, null/async handling, error paths, maintainability.
- **`convention-auditor`** — the house rules in this file (FluentResults, extension-member DI, options pattern,
  Public/Application/Infrastructure layering, MassTransit command shape, pure xUnit-v3-on-MTP tests, Central
  Package Management).
- **`security-reviewer`** — secret/token handling (incl. the plaintext in-memory GitHub token), untrusted Telegram
  input, injection, webhook/Octokit auth.

Invoke one directly (`@code-reviewer`, or "use the security-reviewer subagent"), or run **`/review-pr [pr]`** —
it fans all three out in parallel over a PR (via GitHub MCP) or the local branch diff and synthesizes one
severity-ranked report. They deliberately skip style/formatting (the formatter, analyzers, and
`TreatWarningsAsErrors` already enforce it).

One **test-writing** subagent (read/write, scoped to each module's own `*.UnitTests` project under `src/`):

- **`test-creator`** — writes **pure** xUnit-v3-on-MTP tests for a given module/type using hand-written fakes
  (no mocking library, no network/LLM/Whisper/filesystem/real DB), places them in the module's own test project
  (scaffolding + registering it in `NeuroNotes.slnx` when needed), and verifies them with `dotnet test
  --solution`/`--project`. Delegate test-writing to it; it never touches a module's non-test code.

## Issue-workflow skills (`.claude/skills/`)

Skills that drive the GitHub issue lifecycle end-to-end. All GitHub writes go through the GitHub MCP server
first with the `gh` CLI as fallback:

- **`propose-issues`** (proactive) — decides what to build **next** with minimal guesswork. Trigger with
  **`/next-issue [focus]`** (single best next issue — the default mode) or **`/propose-issues [focus]`**
  (groom a ranked batch). It runs the [propose-issues workflow](.claude/workflows/propose-issues.js), which
  fans out parallel scans over module code, README FR#/NFR#, **all** open+closed issues, open PRs, the project
  board, internal plans (`.claude/TIER3-PLAN.md` + agent memory), and recent git history / uncommitted
  artifacts, then dedupes candidates against existing issues (update instead of duplicate; drop closed matches)
  and ranks them. Before drafting it asks the maintainer about **every** open question and assumption, and it
  creates only after an approved full draft — delegating the actual write to `manage-github-issues`.
- **`manage-github-issues`** (reactive) — the write/dedupe primitive: turns the current discussion into a new
  or updated issue, checking for similar existing issues first.
- **`work-on-issue`** (**`/work-on-issue <n>`**) — takes one issue from picked-up to PR-ready-for-review:
  draft PR whose description is the plan, board → In Progress, implement, verify (build + tests + format),
  commit, push, mark ready.

## Configuration & secrets

Config binds from `appsettings.json` + environment-specific files + **user secrets** (dev) + environment variables
(prod, double-underscore form, e.g. `AiAssistant__OpenAiApiKey`). Never commit real secrets — `appsettings.json`
ships placeholders like `"take from user secrets"`.

| Section | Keys |
|---------|------|
| `Telegram` | `TelegramBotSecretToken` — **optional**, legacy: only used once by `migrate` to seed the pre-platform bot (see State above). A fresh deployment can omit it entirely and register every bot via the admin API |
| `Platform` | `AdminApiKey` (required — authenticates `/admin/*`, SEC-005), `PluginsDirectory` (default `plugins`), `WebhookBaseUrl` (required outside `Development`; each bot's webhook is `{WebhookBaseUrl}/{botId}`) |
| `AiAssistant` | `OpenAiApiKey`, `DefaultModelId` |
| `AudioConversion` | `FFmpegPath`, `TimeoutSeconds` |
| `SpeechRecognition` | `ModelFileName` |
| `Persistence` | `ConnectionString` (Postgres; no password is committed — set it in user secrets for dev; in prod the deploy workflow builds it from the `POSTGRES_PASSWORD` Actions secret) |
| `GitHub` | `ProductHeader`, `DefaultBranch`, `NotesFolder` (all optional; each user's repo + token are supplied at runtime through the bot, not config) |

Set dev secrets with: `dotnet user-secrets set "AiAssistant:OpenAiApiKey" "sk-..." --project src/NeuroNotes.WebApi`
(and the same for `Platform:AdminApiKey`, `Telegram:TelegramBotSecretToken`, and `Persistence:ConnectionString`).

## Build infrastructure

- **Central Package Management**: every NuGet version lives in [Directory.Packages.props](Directory.Packages.props).
  `.csproj` files reference packages **without** a `Version=` attribute. Add/update versions there.
- **Shared MSBuild props**: [Directory.Build.props](Directory.Build.props) sets nullable, implicit usings, analyzers,
  and `TreatWarningsAsErrors` for every project.
- **CI**: [pr-build.yml](.github/workflows/pr-build.yml) builds, runs tests, and verifies formatting on PRs; [deploy.yml](.github/workflows/deploy.yml)
  builds a Docker image, pushes to GHCR, and deploys to a DigitalOcean droplet on push to `main`, applying EF
  migrations via a one-off `migrate` container before starting the app. The Docker image
  is built only from `WebApi` and its references ([Dockerfile](src/NeuroNotes.WebApi/Dockerfile)); it downloads the
  Whisper model and installs `ffmpeg`.

### Production database (DigitalOcean droplet)
Production runs as a **Docker Compose stack** ([docker-compose.prod.yml](docker-compose.prod.yml)) on the droplet:
a `postgres` service (named `pgdata` volume, **no published port**), a one-off `migrate` service (applies EF
migrations then exits), and the `app`. `depends_on` ordering guarantees Postgres becomes healthy → `migrate`
completes → `app` starts. The deploy workflow SCPs the compose file to `/opt/neuronotes`, writes root-only
`db.env`/`conn.env`/`app.env` secret files (consumed via `env_file:`; `migrate` gets only `conn.env`, the app gets
`conn.env` + `app.env`), and runs `docker compose -f docker-compose.prod.yml up -d` — no manual `docker run`
or network setup. The only database secret to set is **`POSTGRES_PASSWORD`** (Actions secret); the connection
string (`Host=postgres;…`) is built from it. The named `pgdata` volume keeps data across redeploys, and Postgres is
reachable only by the other services on the Compose-created network.

## Gotchas

- Touching anything that affects `dotnet restore` at the repo root (the `Directory.*.props` files) also affects the
  Docker build — the Dockerfile copies the full source before `restore` for this reason.
- Tests (in each module's `*.UnitTests` project under `src/<Module>/`) are pure and make **no**
  network/LLM/Whisper calls — keep them that way.
- `SpeechTextEnhancer` is deliberately a *post-processor*, not a chatbot: its system prompt forbids answering the
  text. Preserve that behavior if you edit the prompt.
- **Behavior extensions are trusted, operator-only code** (registration is gated by `Platform:AdminApiKey`) —
  the platform does not sandbox them; it only contains a *faulty* one (bad load, version mismatch, colliding
  key) without crashing. Don't add sandboxing complexity here; vetting an extension's source is the operator's job.
- [samples/EchoBehavior](samples/EchoBehavior) is a demo `IBotBehavior` plugin for trying the upload flow — it's
  not part of any module and nothing else depends on it; safe to ignore unless you're testing plugin loading.
