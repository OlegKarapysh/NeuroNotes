---
description: Scaffold a new Telegram bot command (record + MassTransit consumer) following project conventions
argument-hint: <CommandName> "<what the command does>"
---

Add a new Telegram command to the **TelegramBot** module, following the conventions in `CLAUDE.md` and matching
the existing handlers in `src/TelegramBot/NeuroNotes.TelegramBot.Application/Commands/`.

Command to add: **$ARGUMENTS**

Do all of the following, then build and test until green:

1. **Command + handler** — in one file under `TelegramBot.Application/Commands/`:
   - `sealed record <Name>Command(...)` carrying the data the handler needs (e.g. the Telegram `Message`).
   - `sealed class <Name>CommandHandler(...) : IConsumer<<Name>Command>` that does the work and replies via
     `ITelegramBotClient`. Use the `FluentResults` pattern for expected failures (check `.IsFailed`, reply with
     `.Errors.First().Message`); reserve exceptions for programmer errors.
   - Model it on `CreateNoteCommand.cs` / `EditTranscriptionCommand.cs`.
2. **Endpoint mapping** — add to `MapTelegramCommandEndpoints` in
   `TelegramBot.Infrastructure/ServiceInstaller.cs`, using queue name `nameof(<Name>CommandHandler).ToKebabCase()`.
3. **State machine** — allow the command from the correct `ChatState`(s) in `Menus/ChatStateCommandsMap.cs`.
4. **Dispatch** — route to it from `CommandDispatcher` via `DispatchIfAllowed`. If it's triggered by a keyboard
   button, add the label to `Menus/MenuButtons.cs` and wire it into `Menus/MenuKeyboardFactory.cs`.
5. **Tests** — add unit tests to the TelegramBot module's test project (the state-map and keyboard parts are pure).
6. **Verify** — `dotnet build NeuroNotes.slnx` and `dotnet test --solution NeuroNotes.slnx`. The build uses
   `TreatWarningsAsErrors`, so fix every warning.

Match the house C# style: file-scoped namespaces, `sealed`, primary constructors, `var`, expression-bodied members
where they fit, Allman braces. Prefer adding shared `using`s to the module's `GlobalUsings.cs`.
