# Repository Guidelines

## Project Structure & Module Organization
The CLI tool lives in `src/DotnetCleanup/` (command entry point, services, events, and utilities). Tests are in `test/DotnetCleanup.Tests/`. Solutions are `DotnetCleanup.sln` and `DotnetCleanup.slnx`. Tool packages are produced under `src/DotnetCleanup/nupkg/` when packing.

## Build, Test, and Development Commands
- `dotnet restore` - restore NuGet packages for the solution.
- `dotnet build DotnetCleanup.sln` - build all projects.
- `dotnet test DotnetCleanup.sln` - run xUnit tests.
- `dotnet run --project src/DotnetCleanup -- --help` - run the CLI locally; pass tool arguments after `--`.
- `dotnet pack src/DotnetCleanup/DotnetCleanup.csproj` - create the tool package (outputs to `src/DotnetCleanup/nupkg/`).

## Main Flow
- `src/DotnetCleanup/Program.cs` wires Spectre.Console, registers `IFileSystem`, and runs `CleanupCommand`.
- `src/DotnetCleanup/Cli/CleanupCommand.cs` is the application layer; binds CLI settings, subscribes to `CleanupService` events, prompts for confirmation, and renders output.
- `src/DotnetCleanup/CleanupService.cs` is the entry point for the domain layer; validates settings, lists paths, confirms, ensures temp dir, moves, deletes, returns `CleanupResult`, and keeps a single `PathInfo` instance per path through all steps.
- `src/DotnetCleanup/IO/FileSystemService.cs` performs glob matching + traversal and the move/delete operations using `IFileSystem`.
- `src/DotnetCleanup/CleanupSettings.cs` defines options; `src/DotnetCleanup/CleanupStep.cs`, `src/DotnetCleanup/CleanupResult.cs`, and `src/DotnetCleanup/PathInfo.cs` capture per-step results (`Successes` and `Failed`) and track failure stage metadata (`List`, `Move`, `Delete`) on each `PathInfo`.

## Coding Style & Naming Conventions
Follow `.editorconfig`: spaces only, 4-space indentation for C# (UTF-8 BOM), and 2-space indentation for JSON/XML/PS. Use `SpellingExclusions.dic` for accepted terms. Keep `System` using directives first and do not separate using groups. Avoid multiple blank lines and embedded statements on the same line; keep braces around blocks. Nullable reference types are enabled; handle nulls explicitly. Prefer `var`. Naming rules include PascalCase for public members, `s_` prefix for static fields, and `_` prefix for instance fields. Formatting and unused-parameter diagnostics are treated as warnings (e.g., IDE0055, IDE0060). Keep file and class names aligned.

## Testing Guidelines
Tests use xUnit in `test/DotnetCleanup.Tests/`. Add tests alongside new behavior and keep them deterministic. Use descriptive test class and method names; follow existing patterns in the test project. Run tests with `dotnet test DotnetCleanup.sln` before submitting changes.

## Commit & Pull Request Guidelines
Commit messages in this repo are short and descriptive, often sentence-case or version-prefixed (e.g., `0.6.1: Roll Forward support`, `dotnet format`). Keep messages focused on a single change. For pull requests, include a brief summary, link related issues, and note the test command you ran (or why tests were not run). Update README or version metadata if the tool's behavior or packaging changes.

## Tooling & Configuration Notes
The repository targets `net9.0` and `net10.0`.
