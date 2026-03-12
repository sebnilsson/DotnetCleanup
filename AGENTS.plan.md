# DotnetCleanup Improvement Plan

## Product & CLI correctness

- [ ] **#1 - Align package identity, repository metadata, and install docs**
  `src/DotnetCleanup/DotnetCleanup.csproj:9,17-20` + `README.md` - package metadata still points to `cleanup` / `DotnetGuid`, while the README tells users to install `dotnet-cleanup`. Pick one published identity and make the project file and docs consistent.

- [ ] **#2 - Fix the misleading `--confirm` alias**
  `src/DotnetCleanup/Cli/CleanupSettings.cs:33-35` - `--confirm` currently sets `SkipConfirm = true`, which is the opposite of what the flag name implies. Remove the alias or rename the option so help text, behavior, and tests agree.

- [ ] **#3 - Make the final summary reflect the last executed stage**
  `src/DotnetCleanup/Cli/CleanupCommand.cs:42-50,83` - normal-verbosity output always summarizes `DeleteStep`, so `--noop` and `--no-delete` runs end with `0 succeeded.` even when matches were found or moved. Summarize the effective last stage instead.

- [ ] **#4 - Fix singular/plural wording in the list summary**
  `src/DotnetCleanup/Cli/CleanupCommand.cs:50` - `1 files found` should render as `1 file found`.

## Robustness

- [ ] **#5 - Remove temp-directory races and name collisions**
  `src/DotnetCleanup/IO/FileSystemService.cs:89-97` - temp directories use second-precision timestamps and are created via `DirectoryExists` + `CreateDirectory`. Make names collision-resistant and rely on idempotent creation.

- [ ] **#6 - Rework directory creation so the static lock is unnecessary**
  `src/DotnetCleanup/IO/FileSystemService.cs:8,123-129` - `s_createDirectoryLock` serializes unrelated cleanup operations across the whole process. Once directory creation is safe, remove the process-wide lock or narrow it to the actual contention point.

- [ ] **#7 - Turn Ctrl+C into coordinated cancellation**
  `src/DotnetCleanup/Program.cs:7,35-38` - the `CancelKeyPress` handler only resets console colors. Wire Ctrl+C into a `CancellationTokenSource` and let the active cleanup stage stop cleanly.

- [ ] **#8 - Surface staged temp paths when deletes fail**
  `src/DotnetCleanup/Cli/CleanupCommand.cs:55-58,87-100` + `src/DotnetCleanup/PathInfo.cs` - after a successful move, delete failures are reported against `path.Value`, but the leftover data is under `MovePath`. Show the staged path in the error output or summary so users can find the orphaned content.

## Design & maintainability

- [ ] **#9 - Rename `CleanupResult.GetStep` to `ListStep`**
  `src/DotnetCleanup/CleanupResult.cs:5` + `src/DotnetCleanup/CleanupService.cs` - `GetStep` reads like a method call rather than the list-stage result and is inconsistent with `MoveStep` / `DeleteStep`.

- [ ] **#10 - Remove the unused logging dependency**
  `src/DotnetCleanup/DotnetCleanup.csproj:32` - `Microsoft.Extensions.Logging` is referenced but not used anywhere in the project.

- [ ] **#11 - Extract console rendering from `CleanupCommand`**
  `src/DotnetCleanup/Cli/CleanupCommand.cs` - event subscriptions, confirmation prompting, and console markup all live in one method. Move rendering and summary formatting into a dedicated component to simplify future changes.

## Test gaps

- [ ] **#12 - Add isolated `FileSystemService` tests**
  No tests currently target `FileSystemService` directly. Cover `ValidateSettings`, path enumeration, move target calculation, delete target selection, and temp-directory creation.

- [ ] **#13 - Add file-based cleanup tests**
  `test/DotNetCleanup.Tests/CleanupServiceTests.cs` currently focuses on directory cleanup. Add coverage for file glob matches, file moves, file deletes, and mixed file/directory runs.

- [ ] **#14 - Add cancellation-path tests**
  `src/DotnetCleanup/CleanupService.cs:44-46,88-114` threads a `CancellationToken` through list, move, and delete, but there are no tests for cancellation before or during each stage.

- [ ] **#15 - Add focused helper tests**
  `PathInfo`, `PathUtility`, `CleanupStep`, and `SimpleTypeResolver` have no direct tests. Add validation, equality, normalization, and resolution coverage.

- [ ] **#16 - Add user-flow and output regression tests**
  Add tests for confirmation rejection, empty result sets, corrected singular/plural messaging, corrected summary behavior for `--noop` / `--no-delete`, and delete-failure output that surfaces staged temp paths.

## Notes

Current tests already cover many `CleanupService` stage transitions, skip modes, and failure propagation. The remaining gaps are narrower than the original plan suggested: helper-level coverage, file-based paths, cancellation, and CLI/output regressions.
