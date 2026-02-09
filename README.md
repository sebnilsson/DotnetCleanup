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
    [PATH]    The starting path for the cleanup

OPTIONS:
    -h, --help                  Prints help information
    -p, --include <PATTERNS>    Glob paths to include in cleanup. Default paths: **/bin, **/obj, **/node_modules
    -x, --exclude <PATTERNS>    Glob paths to exclude from cleanup
    -y, --yes                   Run cleanup skipping confirm prompt
        --noop                  Skip deleting files
        --no-move               Skip moving files to temporary folder before deletion
        --temp-path <PATH>      Temporary path to move cleanup files before deletion
    -v, --verbosity <LEVEL>     Sets the verbosity level. Allowed values are minimal (m), normal (n) and detailed (d)
```

### Examples

```bash
# Clean current directory tree (confirmation prompt enabled)
cleanup

# Clean a specific folder tree
cleanup C:\src\project

# Custom include/exclude patterns
cleanup -p "**/bin" -p "**/obj" -p "**/node_modules" -x "**/samples/**"

# Skip confirmation
cleanup -y

# List and move, but do not delete
cleanup --noop

# Delete in place without temp staging
cleanup --no-move -y
```

## Behavior

- Start path defaults to your current working directory.
- Default include patterns are `**/bin`, `**/obj`, and `**/node_modules`.
- Include/exclude patterns are matched relative to the chosen start path.
- Exclude patterns take precedence over include patterns.
- By default, matched paths are moved to a temp staging folder before deletion.
- Temp staging defaults to the system temp path; override with `--temp-path`.
- Verbosity levels: `minimal`, `normal`, `detailed`.
