# CLAUDE.md

Guidance for Claude Code (and other AI agents) working in this repository.
Keep this file accurate — when you change a convention, build command, or module boundary, update it here.

## What NeuroNotes is

A **.NET 10 modular monolith** — a voice-first personal knowledge base delivered through a **Telegram bot**.
Core flow: a user sends a voice or text message → audio is transcribed (Whisper) → an LLM cleans it up →
the user can edit it, ask questions about their notes, or save it as a Markdown note.

The long-term product vision (RAG, semantic search, tagging, backlinks, versioning, event sourcing) lives in
[README.md](README.md) as functional/non-functional requirements. Most of it is **not built yet** — treat the
README as a roadmap, not a description of current behavior.

## Commands

All commands run from the repo root. The build requires the **.NET 10 SDK**.

| Task | Command |
|------|---------|
| Restore | `dotnet restore NeuroNotes.slnx` |
| Build (Debug) | `dotnet build NeuroNotes.slnx` |
| Build (Release, as CI does) | `dotnet build NeuroNotes.slnx --configuration Release` |
| Run all tests | `dotnet test --solution NeuroNotes.slnx` |
| Run one module's tests | `dotnet test --project tests/NeuroNotes.<Module>.UnitTests.csproj` |
| Format to conventions | `dotnet format NeuroNotes.slnx` |
| Run the bot (Web host) | `dotnet run --project src/NeuroNotes.WebApi` |

> The build uses **`TreatWarningsAsErrors`** — a new warning fails the build. Fix the warning; don't suppress it
> unless it's a known false positive (see `NoWarn` in [Directory.Build.props](Directory.Build.props)).

### Running the bot locally
In `Development` the bot uses **long polling** (`TelegramPollingService`); in other environments it registers a
**webhook** (`TelegramWebhookService`). You need a Telegram bot token and an OpenAI API key in user secrets
(see Configuration). `ffmpeg` must be on `PATH`, and a Whisper model file (`ggml-base.bin`) must be present.
Start the local Postgres with `docker compose up -d` ([docker-compose.yml](docker-compose.yml)): first put
`POSTGRES_PASSWORD=...` in a `.env` file at the repo root (gitignored; see [.env.example](.env.example)), then set
the matching `Persistence:ConnectionString` (incl. that password) in user secrets. Apply migrations with
`dotnet run --project src/NeuroNotes.WebApi -- migrate`.

## Architecture

A host (`WebApi`) composes the feature modules over an **in-memory MassTransit bus**. The Telegram update flow:

```
Telegram → TelegramUpdateHandler → publish Update
        → CommandDispatcher (IConsumer<Update>)         // state machine: routes & validates
        → sends a Command (ProcessVoice/ProcessText/CreateNote/EditTranscription/PushNoteToGitHub/ConnectGitHub)
        → <Command>Handler (IConsumer<TCommand>)        // does the work, replies to the user
```

### Modules

Each feature module is split into three projects with a strict dependency direction:

- **`*.Public`** — interfaces and shared contracts only. No logic. Other modules depend **only** on this.
- **`*.Application`** — business logic (services, command handlers, domain types).
- **`*.Infrastructure`** — external concerns + DI wiring (`ServiceInstaller`), config options.

| Module | What it does | Notable types |
|--------|--------------|---------------|
| [AudioProcessing](src/AudioProcessing) | OGG→WAV (FFmpeg) then speech-to-text (Whisper.net, local `ggml-base.bin`) | `VoiceTranscriber`, `VoiceEnhanceTranscriber`, `WhisperSpeechRecognizer`, `FFmpegAudioConverter` |
| [AiAssistant](src/AiAssistant) | LLM features via **Semantic Kernel + OpenAI** | `SpeechTextEnhancer` (clean transcripts), `NoteAssistant` (Q&A over notes), `NoteTextEditor`, `NoteService` |
| [GitHub](src/GitHub) | Commits notes as Markdown files to a user's GitHub repo via **Octokit** | `GitHubRepositoryReference`, `OctokitGitHubAccountLinker`, `OctokitGitHubNotePublisher` |
| [TelegramBot](src/TelegramBot) | Update routing, command dispatch, chat state machine, menus | `CommandDispatcher`, `ChatState`/`ChatStateCommandsMap`, `MenuKeyboardFactory`, the `Commands/*` handlers. `*.Public` holds the `ChatState` enum + `IChatStateStore`/`ILastTranscriptionStore` so the Persistence module can implement them |
| [Persistence](src/Persistence) | Postgres persistence via **EF Core (Npgsql)**: entities, migrations, and the repositories behind other modules' store interfaces. Infrastructure-only — depends solely on `*.Public` projects | `NeuroNotesDbContext`, `PostgresNoteStore`, `PostgresTagStore`, `PostgresUserGitHubSettingsStore`, `PostgresChatStateStore`, `PostgresLastTranscriptionStore` |
| [WebApi](src/NeuroNotes.WebApi) | Host: composes modules, wires MassTransit, maps the Telegram webhook endpoint | `Program.cs`, `ServiceInstaller`, `Telegram/TelegramEndpoints` |

### State: durable data in Postgres, chat session state in memory
**Durable user data lives in Postgres** via the Persistence module: notes (`PostgresNoteStore`), tags
(`PostgresTagStore`), GitHub repository links (`PostgresUserGitHubSettingsStore`), per-chat conversation state
(`PostgresChatStateStore`) and the last transcription (`PostgresLastTranscriptionStore`) — all registered as
**scoped** EF Core repositories. Schema changes go through EF migrations:
`dotnet dotnet-ef migrations add <Name> --project src/Persistence/NeuroNotes.Persistence.Infrastructure/NeuroNotes.Persistence.Infrastructure.csproj`
(the `dotnet-ef` local tool is pinned in `.config/dotnet-tools.json`). The deploy workflow applies migrations with
a one-off `migrate` container before each rollout; locally use `dotnet run --project src/NeuroNotes.WebApi -- migrate`.

**Only `PendingGitHubLinkStore` stays in memory** — a singleton in-memory collection holding the half-finished
GitHub-link input between the two onboarding prompts; it's transient scratch state and is fine to lose on restart.
GitHub **access tokens are stored in plaintext in the database** (the bot deletes the token message from the chat
after reading it); encrypted storage is a roadmap item.

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
- **Telegram commands are MassTransit messages.** A command is a `sealed record`; its handler is a
  `sealed class ... : IConsumer<TCommand>`. Both usually live in one file under `TelegramBot.Application/Commands/`.
- **C# style:** file-scoped namespaces, `sealed` by default, primary constructors, `var`, expression-bodied
  members where they fit, Allman braces, private fields `_camelCase`. Per-module `GlobalUsings.cs` holds the common
  usings — prefer adding there over repeating `using`s. See [.editorconfig](.editorconfig).
- **Nullable reference types are enabled** solution-wide; keep code null-clean.

### Adding a new Telegram command (the most common task)
1. Add `MyThingCommand` (record) + `MyThingCommandHandler` (`IConsumer`) in `TelegramBot.Application/Commands/`.
2. Register the endpoint in `MapTelegramCommandEndpoints` (`TelegramBot.Infrastructure/ServiceInstaller.cs`) —
   the queue name is `nameof(MyThingCommandHandler).ToKebabCase()`.
3. Allow it from the right state(s) in `Menus/ChatStateCommandsMap.cs`.
4. Dispatch to it from `CommandDispatcher` (via `DispatchIfAllowed`), adding a menu button in `MenuButtons` /
   `MenuKeyboardFactory` if the user triggers it from the keyboard.
5. Add unit tests in that module's test project, `tests/TelegramBot/NeuroNotes.TelegramBot.UnitTests` (the
   state-map and keyboard parts are pure and easy to test).

## Tests

Tests live under `tests/`, **one project per module** (`NeuroNotes.<Module>.UnitTests`), each referencing **only**
its own module's project(s).

- **Stack: xUnit v3 on [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro) (MTP).**
  Test projects reference `xunit.v3.mtp-v2` and are executables (`<OutputType>Exe</OutputType>`).
  `dotnet test` runs in **MTP mode**, enabled by [global.json](global.json) (`"runner": "Microsoft.Testing.Platform"`).
  Because of MTP mode, pass the target explicitly: `dotnet test --solution NeuroNotes.slnx` or
  `dotnet test --project <test>.csproj` (the legacy positional `dotnet test <path>` no longer applies).
- Keep tests **pure** — no network, LLM, Whisper, filesystem, or real database. Replace collaborators with small
  hand-written fakes implementing the module's interfaces. The repo has no mocking library on purpose; don't add
  one unless a test genuinely needs it. One sanctioned exception: `NeuroNotes.Persistence.UnitTests` uses the
  **EF Core in-memory provider** to exercise the repositories (still no I/O).
- Add a feature to a module → add its tests to that module's project. New module → new test project, registered
  in `NeuroNotes.slnx`. (`/new-module` scaffolds this.)

## Reviewing code (`.claude/agents/`)

Three **read-only** review subagents live in [.claude/agents](.claude/agents) — each reviews a diff through one
lens and never edits code:

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

## Configuration & secrets

Config binds from `appsettings.json` + environment-specific files + **user secrets** (dev) + environment variables
(prod, double-underscore form, e.g. `AiAssistant__OpenAiApiKey`). Never commit real secrets — `appsettings.json`
ships placeholders like `"take from user secrets"`.

| Section | Keys |
|---------|------|
| `Telegram` | `TelegramBotSecretToken`, `WebhookUrl` |
| `AiAssistant` | `OpenAiApiKey`, `DefaultModelId` |
| `AudioConversion` | `FFmpegPath`, `TimeoutSeconds` |
| `SpeechRecognition` | `ModelFileName` |
| `Persistence` | `ConnectionString` (Postgres; no password is committed — set it in user secrets for dev, and via the `Persistence__ConnectionString` env var / `DB_CONNECTION_STRING` CI secret in prod) |
| `GitHub` | `ProductHeader`, `DefaultBranch`, `NotesFolder` (all optional; each user's repo + token are supplied at runtime through the bot, not config) |

Set dev secrets with: `dotnet user-secrets set "AiAssistant:OpenAiApiKey" "sk-..." --project src/NeuroNotes.WebApi`
(and the same for `Telegram:TelegramBotSecretToken` and `Persistence:ConnectionString`).

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
Postgres runs as a long-lived Docker container on the droplet, joined to a shared `neuronotes-net` Docker network
(the deploy workflow creates the network and attaches the migrate + app containers to it) with a named volume and
**no published port** — it's reachable only by the other containers on that network. Provision it **once**:
```bash
docker run -d --name neuronotes-postgres --restart unless-stopped --network neuronotes-net \
  -e POSTGRES_DB=neuronotes -e POSTGRES_USER=neuronotes -e POSTGRES_PASSWORD='<strong-password>' \
  -v neuronotes-pgdata:/var/lib/postgresql/data postgres:17-alpine
```
Then set the `DB_CONNECTION_STRING` Actions secret to
`Host=neuronotes-postgres;Port=5432;Database=neuronotes;Username=neuronotes;Password=<strong-password>` (the host is
the container name). Each deploy runs the one-off `migrate` container against it before (re)starting the app; the
named volume keeps data across redeploys.

## Gotchas

- Touching anything that affects `dotnet restore` at the repo root (the `Directory.*.props` files) also affects the
  Docker build — the Dockerfile copies the full source before `restore` for this reason.
- Tests in `tests/` are pure and make **no** network/LLM/Whisper calls — keep them that way.
- `SpeechTextEnhancer` is deliberately a *post-processor*, not a chatbot: its system prompt forbids answering the
  text. Preserve that behavior if you edit the prompt.
