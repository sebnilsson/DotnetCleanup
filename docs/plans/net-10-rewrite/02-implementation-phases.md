# Net 10 rewrite - implementation phases

## Phase 0: repo setup
- [x] Decide target frameworks (net10.0 only vs multi-target) and update `DotnetCleanup.csproj` accordingly.
- [x] Remove MediatR, McMaster.Extensions.CommandLineUtils, KeyLocks, and any custom logging packages/usings.

## Phase 1: core flow
- [x] Rebuild `CleanupService.RunAsync` with explicit flow: resolve sources -> match paths -> top-level filter -> display -> confirm -> move -> delete -> summary.
- [x] Group helper types into a small set of focused files alongside `CleanupService`.
- [x] Keep all console output inside `CleanupService`.

## Phase 2: globbing + selection
- [x] Treat `PATH` as the root directory and remove solution/project discovery.
- [x] Implement include/exclude matching with globbing and exclusion precedence.
- [x] Ensure top-level-only cleanup behavior.

## Phase 3: cleanup execution
- [x] Implement move-to-temp behavior with same-drive fallback.
- [x] Implement delete behavior and empty temp-root cleanup.
- [x] Preserve verbosity behavior and exit codes.

## Phase 4: CLI wiring
- [x] Map CLI options in `CleanupSettings` and `CleanupCommand`.
- [x] Keep `-y|--confirm-cleanup`, `-nd|--no-delete`, `-nm|--no-move`, `-t|--temp-path`, `-v|--verbosity`.
- [x] Show the path list at Normal verbosity or higher.

## Phase 5: docs and packaging
- [x] Update README if behavior or defaults change.
- [x] Ensure `dotnet pack` still produces the tool package under `src/DotnetCleanup/nupkg`.
