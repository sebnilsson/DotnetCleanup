# Net 10 rewrite - context and goals

## Required behavior inventory
- CLI argument `PATH` points to the root folder to scan (defaults to current working directory).
- Include patterns are globbing patterns relative to the root (defaults to `bin`, `obj`, `node_modules`).
- Exclude patterns are relative to the root and always take precedence over includes.
- Globbing supports `*`, `?`, and `**`.
- Only top-level matches are cleaned (no nested children if a parent is already selected).
- If verbosity is Normal or higher, list the paths to clean.
- Prompt for confirmation unless `-y|--confirm-cleanup` is set.
- Move matched paths to a temp folder before deletion (unless `--no-move`).
- Delete matched paths after move (unless `--no-delete`).
- If temp path is on a different drive, use a temp folder under the cleanup path's drive.
- Exit code is `0` on success, `1` if errors occur.

## Goals
- [ ] Keep the main flow explicit in `CleanupService.RunAsync`, with all console output there.
- [ ] Simplify architecture and namespaces; avoid unnecessary small files.
- [ ] Keep interfaces only for disk I/O to enable unit testing.
- [ ] Remove MediatR, McMaster.Extensions.CommandLineUtils, KeyLocks, and custom logging.
- [ ] Use Spectre.Console.Cli for CLI parsing and Microsoft.Extensions.Logging for logging.
- [ ] Add or expand integration tests for CLI behavior.

## Non-goals
- Keep existing CLI option names and defaults unless Spectre forces a change.
- Do not expand functionality beyond cleanup and reporting.
