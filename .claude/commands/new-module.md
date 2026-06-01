---
description: Scaffold a new feature module (Public/Application/Infrastructure) following project conventions
argument-hint: <ModuleName>
---

Scaffold a new feature module named **$ARGUMENTS**, following the modular-monolith conventions in `CLAUDE.md`.
Mirror the structure of an existing module such as `src/AudioProcessing`.

Create three projects under `src/$ARGUMENTS/` with a strict dependency direction:

1. `NeuroNotes.$ARGUMENTS.Public` — interfaces and shared contracts only, **no logic**. Reference `FluentResults`
   if its methods return `Result<T>`. This is the only project other modules may depend on.
2. `NeuroNotes.$ARGUMENTS.Application` — services / domain logic. References only `*.Public`.
3. `NeuroNotes.$ARGUMENTS.Infrastructure` — external concerns + DI wiring + config options.

Requirements:

- **Central Package Management**: `.csproj` files reference packages **without** a `Version` attribute. Add any
  new versions to `Directory.Packages.props`. (Shared MSBuild settings come from `Directory.Build.props`.)
- **DI**: add a `ServiceInstaller` using C# **extension members** with a single `Add$($ARGUMENTS)Module(...)` entry
  point, and compose it in `src/NeuroNotes.WebApi/Program.cs`.
- **Options pattern** for config: `services.AddOptions<TOptions>().BindConfiguration(TOptions.SectionName)
  .ValidateDataAnnotations().ValidateOnStart();`. Options are `record`s with a `const string SectionName` and
  `[Required]`/`[Range]` data annotations.
- Add a per-module `GlobalUsings.cs` for common usings.
- **Tests**: add a `NeuroNotes.$ARGUMENTS.UnitTests` project (xUnit v3 on Microsoft.Testing.Platform —
  reference `xunit.v3.mtp-v2`, set `<OutputType>Exe</OutputType>`; match the other test projects).
- **Solution**: register every new project (including the test project) in `NeuroNotes.slnx`.
- **Verify**: `dotnet build NeuroNotes.slnx` and `dotnet test --solution NeuroNotes.slnx` — keep everything green
  (`TreatWarningsAsErrors` is on).

Match the house C# style: file-scoped namespaces, `sealed`, primary constructors, `var`, Allman braces.
