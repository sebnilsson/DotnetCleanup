# Net 10 rewrite - architecture outline

## Target layout
- [x] Keep a single `DotnetCleanup` namespace with a small set of files: `Program.cs`, `CleanupApp.cs`, `CleanupCommand.cs`, `CleanupSettings.cs`, `CleanupService.cs`, `FileSystem.cs`.
- [x] Keep helper types grouped in a small number of focused files (for example `CleanupOptions.cs`, `CleanupResult.cs`, `GlobMatcher.cs`) rather than scattering every type into its own file.
- [x] Keep `IFileSystem` as the only interface boundary; all other types are concrete.

## CLI + composition
- [x] Use Spectre.Console.Cli with `CleanupCommand` as the default command.
- [x] Avoid DI containers; construct objects directly in `Program` or the CLI builder.
- [x] Keep argument normalization (short flags) in the CLI layer.

## Logging + console output
- [x] Use Microsoft.Extensions.Logging (SimpleConsole) for diagnostics.
- [x] Keep all user-facing output inside `CleanupService` for traceability.

## Globbing approach
- [x] Implement a minimal glob matcher supporting `*`, `?`, `**` with exclusion precedence.
- [x] Match include/exclude patterns against root-relative paths only.
