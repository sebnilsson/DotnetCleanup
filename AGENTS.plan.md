# DotnetCleanup Improvement Plan

## Architecture & Design

- [ ] **#1 — Decouple `CleanupService` from `FileSystemService`**
  `CleanupService.cs:14` — `FileSystemService` is instantiated directly inside the constructor rather than injected. Extract an interface or inject `FileSystemService` through the constructor to enable independent mocking and testing.

- [ ] **#2 — Make `FileSystemService` directory creation lock instance-scoped**
  `FileSystemService.cs:8` — `s_createDirectoryLock` is `static`, meaning all instances share one process-wide lock. Two independent cleanup operations would contend unnecessarily. Make it an instance field.

- [ ] **#3 — Extract console rendering logic from `CleanupCommand`**
  `CleanupCommand.cs` — The command handler subscribes to 9 events and contains all console rendering logic mixed with confirmation prompt handling. Extract presentation concerns into a separate result renderer/formatter class.

- [ ] **#4 — Audit mutable `PathInfo` usage in `HashSet`**
  `PathInfo.cs` — Objects are mutated via `SetMovePath`, `SetFailedOnMove`, etc., while stored in `CleanupStep`'s `HashSet<PathInfo>`. The comparer uses `InitialValue` (immutable) so this works today, but the pattern is fragile. Add a comment or consider making the mutable state separate.

- [ ] **#5 — Remove unused `Microsoft.Extensions.Logging` dependency**
  `DotnetCleanup.csproj` references `Microsoft.Extensions.Logging` but no `ILogger` is used anywhere.

## Code Quality

- [ ] **#6 — Rename `CleanupResult.GetStep` to `ListStep`**
  `CleanupResult.cs:5` — `GetStep` reads like a method call. `ListStep` is consistent with `MoveStep` and `DeleteStep`.

- [ ] **#7 — Fix singular/plural in "files found" message**
  `CleanupCommand.cs:50` — `"{step.Successes.Count} files found"` should handle singular ("1 file found") vs plural ("2 files found").

- [ ] **#8 — Reconcile inconsistent path normalization direction**
  `PathUtility.cs:9` — `GetNormalizedPath` normalizes to OS `DirectorySeparatorChar`, but `GetRelativePath` converts to forward slashes. The normalization direction is inconsistent across methods.

- [ ] **#9 — Review unnecessary `async` wrapping in `CleanupCommand.ExecuteAsync`**
  `CleanupCommand.cs:11` — The method uses `await` only for the Spectre status spinner; the actual callback is synchronous. Consider whether the async wrapper adds value or just overhead.

- [ ] **#10 — Simplify exception catch filters with `or` pattern**
  `FileSystemService.cs:52-54` — `catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)` can be simplified to `catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)`.

## Robustness & Edge Cases

- [ ] **#11 — Add `MaxDegreeOfParallelism` to `Parallel.ForEach`**
  `CleanupService.cs:95-104, 116-127` — `ParallelOptions` only sets `CancellationToken`. On large repos this could saturate I/O. Consider making it configurable or setting a reasonable default.

- [ ] **#12 — Fix TOCTOU race in `EnsureTempDirectory`**
  `FileSystemService.cs:95-98` — `DirectoryExists` + `CreateDirectory` is racy. `Directory.CreateDirectory` is idempotent, so just call it directly.

- [ ] **#13 — Prevent temp directory name collisions**
  `FileSystemService.cs:93` — Timestamp uses second precision (`yyyyMMdd-HHmmss`). Two runs in the same second collide. Add milliseconds or a random suffix.

- [ ] **#14 — Improve `CancelKeyPress` handler**
  `Program.cs:36-38` — Handler only resets console color. It doesn't set `e.Cancel = true` or trigger a `CancellationTokenSource`. Actual cancellation relies on implicit Spectre behavior.

- [ ] **#15 — Handle orphaned temp directories on failure**
  If the move stage succeeds but delete fails, the temp directory retains file copies with no cleanup or user notification.

## Missing Test Cases

- [ ] **#16 — Add unit tests for `FileSystemService` in isolation**
  All unit tests go through `CleanupService`. Add direct tests for:
  - `ValidateSettings()` (empty includes, missing root path, missing temp path)
  - `GetPaths()` (recursive traversal, file vs directory matching)
  - `MovePath()` (relative path resolution, target path construction)
  - `DeletePath()` (path selection logic — `MovePath` vs `Value`)

- [ ] **#17 — Add tests for file-based operations (not just directories)**
  All unit tests use directories only. Add tests for:
  - File glob matching (e.g., `**/*.log`)
  - File move operations (`MoveFile` path)
  - File delete operations (`DeleteFile` path)
  - Mixed file and directory cleanup

- [ ] **#18 — Add cancellation token tests**
  `CancellationToken` is threaded through all stages but never tested. Add tests for:
  - Cancellation during the List stage
  - Cancellation during the Move stage (partial completion)
  - Cancellation during the Delete stage (partial completion)

- [ ] **#19 — Add unit tests for `PathInfo`**
  No tests exist for:
  - Constructor validation (null/whitespace path, normalization)
  - `SetMovePath` validation
  - Multiple failure state transitions
  - `Parent` property computation
  - `InitialValue` vs `Value` vs `Raw` semantics

- [ ] **#20 — Add unit tests for `PathUtility`**
  Zero tests for:
  - `GetNormalizedPath` (forward slashes, trailing separators, null, whitespace)
  - `GetParentPath` (root paths, single-segment paths, null)
  - `GetRelativePath` (cross-platform behavior)

- [ ] **#21 — Add unit tests for `CleanupStep`**
  Missing coverage for:
  - Thread safety (concurrent `AddSuccess`/`AddFailed` calls)
  - `PathInfoComparer` behavior (duplicate detection, case sensitivity)
  - Adding the same path to both success and failed collections

- [ ] **#22 — Add test for user confirmation rejection**
  When `onConfirmCallback` returns `false`, the service returns early with only List results. This flow is not unit-tested.

- [ ] **#23 — Add test for empty directory tree**
  What happens when the target path exists but contains no matching files/directories? Current tests always set up matching paths.

- [ ] **#24 — Add integration test for error output**
  Integration tests cover success paths and validation errors, but don't verify console output when move/delete operations fail (error messages, exception rendering).

- [ ] **#25 — Add unit tests for `SimpleTypeResolver`**
  The DI container's constructor injection, `IEnumerable<T>` resolution, and `Activator.CreateInstance` fallback are untested.

## Priority Summary

| Priority | Area | Items |
|----------|------|-------|
| **High** | Missing tests | #16–#22 — Core logic lacks isolated unit tests |
| **High** | Design | #1 — Hard coupling prevents testability |
| **Medium** | Robustness | #11, #12, #13 — Parallelism and race conditions |
| **Medium** | Code quality | #5, #6, #10 — Cleanup and naming |
| **Low** | Polish | #7, #8, #14, #15 — UX and edge cases |
