# Net 10 rewrite - implementation phases

## Phase 0: repo setup
- [ ] Decide target frameworks (net10.0 only vs multi-target) and update `DotnetCleanup.csproj` accordingly.
- [ ] Remove MediatR, McMaster.Extensions.CommandLineUtils, KeyLocks, and any custom logging packages/usings.

## Phase 1: core flow
- [ ] Rebuild `CleanupService.RunAsync` with explicit flow: resolve sources -> match paths -> top-level filter -> display -> confirm -> move -> delete -> summary.
- [ ] Keep options/records close to `CleanupService` to reduce file count.
- [ ] Keep all console output inside `CleanupService`.

## Phase 2: globbing + selection
- [ ] Treat `PATH` as the root directory and remove solution/project discovery.
- [ ] Implement include/exclude matching with globbing and exclusion precedence.
- [ ] Ensure top-level-only cleanup behavior.

## Phase 3: cleanup execution
- [ ] Implement move-to-temp behavior with same-drive fallback.
- [ ] Implement delete behavior and empty temp-root cleanup.
- [ ] Preserve verbosity behavior and exit codes.

## Phase 4: CLI wiring
- [ ] Map CLI options in `CleanupSettings` and `CleanupCommand`.
- [ ] Keep `-y|--confirm-cleanup`, `-nd|--no-delete`, `-nm|--no-move`, `-t|--temp-path`, `-v|--verbosity`.
- [ ] Show the path list at Normal verbosity or higher.

## Phase 5: docs and packaging
- [ ] Update README if behavior or defaults change.
- [ ] Ensure `dotnet pack` still produces the tool package under `src/DotnetCleanup/nupkg`.
