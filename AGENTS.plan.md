# DotnetCleanup Improvement Plan

## Product & CLI correctness

- [x] **#1 - Align package identity, repository metadata, and install docs**
  `src/DotnetCleanup/DotnetCleanup.csproj:9,17-20` + `README.md` - package metadata still points to `cleanup` / `DotnetGuid`, while the README tells users to install `dotnet-cleanup`. Pick one published identity and make the project file and docs consistent.

- [x] **#2 - Fix the misleading `--confirm` alias**
  `src/DotnetCleanup/Cli/CleanupSettings.cs:33-35` - `--confirm` currently sets `SkipConfirm = true`, which is the opposite of what the flag name implies. Remove the alias or rename the option so help text, behavior, and tests agree.

- [x] **#3 - Make the final summary reflect the last executed stage**
  `src/DotnetCleanup/Cli/CleanupCommand.cs:42-50,83` - normal-verbosity output always summarizes `DeleteStep`, so `--noop` and `--no-delete` runs end with `0 succeeded.` even when matches were found or moved. Summarize the effective last stage instead.

- [x] **#4 - Fix singular/plural wording in the list summary**
  `src/DotnetCleanup/Cli/CleanupCommand.cs:50` - `1 files found` should render as `1 file found`.

- [ ] **#17 - Make CLI result wording path-aware**
  `src/DotnetCleanup/Cli/CleanupCommand.cs` - the command still talks about `files` even when directory matches dominate, and `No files found` is misleading when a step contains only failures. Report `paths` or stage-specific wording instead.

- [ ] **#18 - Normalize ineffective option combinations**
  `src/DotnetCleanup/Cli/CleanupSettings.cs` + `test/DotnetCleanup.IntegrationTests/CleanupCommandTests.cs` - `--noop` already implies no move and no delete, so combining it with `--no-move` or `--no-delete` is redundant. Either validate, normalize, or document the precedence explicitly and test it.

## Robustness

- [x] **#5 - Remove temp-directory races and name collisions**
  `src/DotnetCleanup/IO/FileSystemService.cs:89-97` - temp directories use second-precision timestamps and are created via `DirectoryExists` + `CreateDirectory`. Make names collision-resistant and rely on idempotent creation.

- [x] **#6 - Rework directory creation so the static lock is unnecessary**
  `src/DotnetCleanup/IO/FileSystemService.cs:8,123-129` - `s_createDirectoryLock` serializes unrelated cleanup operations across the whole process. Once directory creation is safe, remove the process-wide lock or narrow it to the actual contention point.

- [x] **#7 - Turn Ctrl+C into coordinated cancellation**
  `src/DotnetCleanup/Program.cs:7,35-38` - the `CancelKeyPress` handler only resets console colors. Wire Ctrl+C into a `CancellationTokenSource` and let the active cleanup stage stop cleanly.

- [x] **#8 - Surface staged temp paths when deletes fail**
  `src/DotnetCleanup/Cli/CleanupCommand.cs:55-58,87-100` + `src/DotnetCleanup/PathInfo.cs` - after a successful move, delete failures are reported against `path.Value`, but the leftover data is under `MovePath`. Show the staged path in the error output or summary so users can find the orphaned content.

- [ ] **#19 - Treat disappearing paths as per-path failures**
  `src/DotnetCleanup/IO/FileSystemService.cs` - move/delete operations only translate `UnauthorizedAccessException` and `IOException` into `PathInfo` failures. If a path vanishes after listing, `DirectoryNotFoundException` or `FileNotFoundException` can still fail the whole run instead of being reported on the affected path.

## Design & maintainability

- [x] **#9 - Rename `CleanupResult.GetStep` to `ListStep`**
  `src/DotnetCleanup/CleanupResult.cs:5` + `src/DotnetCleanup/CleanupService.cs` - `GetStep` reads like a method call rather than the list-stage result and is inconsistent with `MoveStep` / `DeleteStep`.

- [x] **#10 - Remove the unused logging dependency**
  `src/DotnetCleanup/DotnetCleanup.csproj:32` - `Microsoft.Extensions.Logging` is referenced but not used anywhere in the project.

- [x] **#11 - Extract console rendering from `CleanupCommand`**
  `src/DotnetCleanup/Cli/CleanupCommand.cs` - event subscriptions, confirmation prompting, and console markup all live in one method. Move rendering and summary formatting into a dedicated component to simplify future changes.

- [ ] **#20 - Stop accumulating event handlers across command executions**
  `src/DotnetCleanup/Cli/CleanupCommand.cs` - `AttachEventHandlers` subscribes to `CleanupService` events on every run and never detaches them. If the same service/command instance is reused, output handlers will stack and duplicate.

- [ ] **#21 - Replace manual parent-path parsing**
  `src/DotnetCleanup/IO/PathUtility.cs` - `GetParentPath` slices strings by the last separator instead of delegating to the platform path APIs. That is fragile for root paths, UNC paths, and separator edge cases.

- [ ] **#22 - Decide whether cleanup result ordering should be deterministic**
  `src/DotnetCleanup/CleanupStep.cs` + `src/DotnetCleanup/CleanupService.cs` - successes and failures are stored in `HashSet<PathInfo>` and populated from parallel stages, so output order is inherently unstable. Either keep that as an explicit non-goal or switch to deterministic ordering for easier testing and supportability.

## Test gaps

- [x] **#12 - Add isolated `FileSystemService` tests**
  No tests currently target `FileSystemService` directly. Cover `ValidateSettings`, path enumeration, move target calculation, delete target selection, and temp-directory creation.

- [x] **#13 - Add file-based cleanup tests**
  `test/DotNetCleanup.Tests/CleanupServiceTests.cs` currently focuses on directory cleanup. Add coverage for file glob matches, file moves, file deletes, and mixed file/directory runs.

- [ ] **#14 - Add cancellation-path tests**
  `src/DotnetCleanup/CleanupService.cs:44-46,88-114` threads a `CancellationToken` through list, move, and delete, but there are no tests for cancellation before or during each stage.

- [ ] **#15 - Add focused helper tests**
  `PathInfo`, `PathUtility`, `CleanupStep`, and `SimpleTypeResolver` have no direct tests. Add validation, equality, normalization, and resolution coverage.

- [x] **#16 - Add user-flow and output regression tests**
  Add tests for confirmation rejection, empty result sets, corrected singular/plural messaging, corrected summary behavior for `--noop` / `--no-delete`, and delete-failure output that surfaces staged temp paths.

- [ ] **#23 - Add disappearing-path regression tests**
  `test/DotNetCleanup.Tests/CleanupServiceTests.cs` + `test/DotnetCleanup.IntegrationTests/CleanupCommandTests.cs` - add cases where a path is listed successfully but disappears before move or delete so the intended error handling stays covered.

- [ ] **#24 - Add mid-traversal enumeration exception tests**
  `test/DotNetCleanup.Tests/FileSystemServiceTests.cs` + `test/DotNetCleanup.Tests/CleanupServiceTests.cs` - add unit tests for exceptions thrown after traversal has already started, such as failures while enumerating later child folders or files, to verify partial results and failure reporting stay correct.

## CI/CD

- [ ] **#25 - Fix workflow branch trigger**
  `.github/workflows/publish.yml:8` — triggers on `main` but the primary branch is `master`. The publish workflow will never run on push.

- [ ] **#26 - Add test coverage reporting**
  No coverage metrics in CI — add `coverlet` or similar to expose coverage percentages in build output or PR checks.

- [ ] **#27 - Add cross-platform CI matrix**
  CI runs on `ubuntu-latest` only. Path comparison logic (`CleanupStep.cs:30-32`) uses platform-specific `StringComparer` that is untested on Windows in CI. Add a Windows runner to the matrix.

## Dependencies

- [ ] **#28 - Replace alpha test dependency**
  `test/Directory.Packages.props:7` — `Spectre.Console.Cli.Testing` is `1.0.0-alpha.0.12`. Pin or upgrade when a stable version becomes available to avoid breaking changes.

## Performance

- [ ] **#29 - Set `MaxDegreeOfParallelism` on `Parallel.ForEach`**
  `src/DotnetCleanup/CleanupService.cs:89-98` and `:111-121` — uses default parallelism (all processors). Cap at a reasonable limit (e.g., 4–8) to avoid I/O saturation on small directory trees.

- [ ] **#30 - Cache constructor resolution in `SimpleTypeResolver`**
  `src/DotnetCleanup/Spectre/SimpleTypeResolver.cs:44-70` — reflects over constructors on every `Resolve()` call. Memoize the winning constructor per type.

## Code quality

- [ ] **#31 - Fix naming inconsistency in test projects**
  Source uses `DotnetCleanup` but test projects use `DotNetCleanup` (capitalized "N") — `DotNetCleanup.Testing`, `DotNetCleanup.Tests`. Align casing.

- [ ] **#32 - Add early termination for systemic failures**
  If all moves or deletes fail (e.g., permissions), the tool continues attempting every remaining path. Consider aborting after a threshold of consecutive failures.

## Documentation

- [ ] **#33 - Document symlink/junction skip behavior**
  `src/DotnetCleanup/IO/FileSystem.cs:10` — `AttributesToSkip = FileAttributes.ReparsePoint` silently skips symlinks and junctions. Document this in the README so users know these are intentionally excluded.

## Features (nice to have)

- [ ] **#34 - Add disk space estimation in `--noop` mode**
  Dry-run doesn't show how much space would be freed. Useful for large monorepos.

- [ ] **#35 - Add JSON output format**
  Only console output is supported. A `--output json` flag would enable scripting and automation.

- [ ] **#36 - Add logging/audit support**
  No persistent log of what was cleaned — events only flow to the CLI console. A file-based audit trail would help in CI or scheduled cleanup scenarios.

## Notes

Current tests already cover many `CleanupService` stage transitions, skip modes, and failure propagation. The remaining gaps are narrower than the original plan suggested: helper-level coverage, file-based paths, cancellation, and CLI/output regressions.
