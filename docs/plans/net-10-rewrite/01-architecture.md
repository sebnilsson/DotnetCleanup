# Net 10 rewrite - architecture outline

## Target layout
- [ ] Keep a single `DotnetCleanup` namespace with a small set of files: `Program.cs`, `CleanupApp.cs`, `CleanupCommand.cs`, `CleanupSettings.cs`, `CleanupService.cs`, `FileSystem.cs`.
- [ ] Keep helper types grouped in a small number of focused files (for example `CleanupOptions.cs`, `CleanupResult.cs`, `GlobMatcher.cs`) rather than scattering every type into its own file.
- [ ] Keep `IFileSystem` as the only interface boundary; all other types are concrete.

## CLI + composition
- [ ] Use Spectre.Console.Cli with `CleanupCommand` as the default command.
- [ ] Avoid DI containers; construct objects directly in `Program` or the CLI builder.
- [ ] Keep argument normalization (short flags) in the CLI layer.

## Logging + console output
- [ ] Use Microsoft.Extensions.Logging (SimpleConsole) for diagnostics.
- [ ] Keep all user-facing output inside `CleanupService` for traceability.

## Globbing approach
- [ ] Implement a minimal glob matcher supporting `*`, `?`, `**` with exclusion precedence.
- [ ] Match include/exclude patterns against root-relative paths only.
