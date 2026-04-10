# dotnet-cleanup

.NET tool for cleaning generated folders from a directory tree.

By default, it targets the common output/dependency folders `**/bin`, `**/obj` and `**/node_modules`.

To maximize the speed of deletion, enabling instant working with your project, the tool moves the deleted files to a temp-folder first, before deleting.

## Install

Requires .NET SDK: https://dotnet.microsoft.com/download

Install the .NET tool:

```bash
dotnet tool install --global dotnet-cleanup
```

## Usage

```bash
USAGE:
    dotnet-cleanup [PATH] [OPTIONS]

EXAMPLES:
    dotnet-cleanup c:\src\project --include **/bin --include **/obj --include **/node_modules --exclude README.md
    dotnet-cleanup -p **/bin -p **/obj -y
    dotnet-cleanup -p **/node_modules --verbosity minimal

ARGUMENTS:
    [PATH]    The starting path for the cleanup. Defaults to current directory

OPTIONS:
    -h, --help                  Prints help information
    -p, --include <PATTERNS>    Glob paths to include in cleanup. Default paths: **/bin, **/obj, **/node_modules
    -x, --exclude <PATTERNS>    Glob paths to exclude from cleanup
    -y, --yes                   Run cleanup skipping confirm prompt
        --noop                  No-op mode: list matching paths without moving or deleting anything. Equivalent to
                                --no-move and --no-delete
        --no-delete             Skip deleting matched paths after moving them to temporary folder. Ignored when --noop
                                is used
        --no-move               Skip moving matched paths to temporary folder before deletion. Ignored when --noop is
                                used
        --temp-path <PATH>      Temporary path to move cleanup files before deletion
    -v, --verbosity <LEVEL>     Sets the verbosity level. Allowed values are minimal (m), normal (n) and detailed (d)
```

### Examples

```bash
# Clean current directory tree (confirmation prompt enabled)
dotnet-cleanup

# Clean a specific folder tree
dotnet-cleanup C:\src\project

# Custom include/exclude patterns
dotnet-cleanup -p "**/bin" -p "**/obj" -p "**/node_modules" -x "**/samples/**"

# Skip confirmation
dotnet-cleanup -y

# List only (do not move or delete)
dotnet-cleanup --noop

# Move to temp folder, but do not delete
dotnet-cleanup --no-delete -y

# Delete in place without temp staging
dotnet-cleanup --no-move -y
```

## Behavior

- Start path defaults to your current working directory.
- Default include patterns are `**/bin`, `**/obj`, and `**/node_modules`.
- Include/exclude patterns are matched relative to the chosen start path.
- Exclude patterns take precedence over include patterns.
- By default, matched paths are moved to a temp staging folder before deletion.
- `--no-delete` keeps moved paths in temp staging and skips deletion.
- `--noop` lists matching paths but skips both move and delete.
- Temp staging defaults to the system temp path; override with `--temp-path`.
- Temp path is only checked when move before delete is enabled (default).
- Verbosity levels: `minimal`, `normal`, `detailed`.
