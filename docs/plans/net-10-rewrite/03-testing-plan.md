# Net 10 rewrite - testing plan

## Unit tests (pure logic with IFileSystem fakes)
- [ ] Root path handling and default include patterns.
- [ ] Glob matching for `*`, `?`, `**` with exclude precedence.
- [ ] Top-level-only filtering.
- [ ] Move/delete decision matrix for `--no-move` and `--no-delete`.
- [ ] Summary messages for success/error counts and verbosity.

## Integration tests (CLI + real file system)
- [ ] Add or refresh `test/DotnetCleanup.IntegrationTests` for Spectre CLI runs.
- [ ] Verify `cleanup --help` and option parsing.
- [ ] Verify `-y` skips the prompt and default behavior prompts.
- [ ] Verify move + delete behavior in a temp workspace.
- [ ] Verify exit codes on success and failure.

## Manual verification
- [ ] Run `dotnet test DotnetCleanup.sln`.
- [ ] Run `dotnet run --project src/DotnetCleanup -- --help`.
