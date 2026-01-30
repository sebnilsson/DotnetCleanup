# Net 10 rewrite - testing plan

## Unit tests (pure logic with IFileSystem fakes)
- [x] Root path handling and default include patterns.
- [x] Glob matching for `*`, `?`, `**` with exclude precedence.
- [x] Top-level-only filtering.
- [x] Move/delete decision matrix for `--no-move` and `--no-delete`.
- [x] Summary messages for success/error counts and verbosity.

## Integration tests (CLI + real file system)
- [x] Add or refresh `test/DotnetCleanup.IntegrationTests` for Spectre CLI runs.
- [x] Verify `cleanup --help` and option parsing.
- [x] Verify `-y` skips the prompt and default behavior prompts.
- [x] Verify move + delete behavior in a temp workspace.
- [x] Verify exit codes on success and failure.

## Manual verification
- [x] Run `dotnet test test/DotNetCleanup.Tests/DotNetCleanup.Tests.csproj`.
- [x] Run `dotnet test test/DotnetCleanup.IntegrationTests/DotnetCleanup.IntegrationTests.csproj`.
- [x] Run `dotnet run --project src/DotnetCleanup -- --help`.
