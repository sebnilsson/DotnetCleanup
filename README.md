# dotnet-cleanup

.NET Global Tool for cleaning up build output and dependency folders.

The tool will list the files and folders which will be deleted,
and you will be prompted to confirm. Can be disabled with
the `-y|--yes` command-option.

Deleted files and folders are first moved to a temporary folder
before deletion, so **you can continue working with your projects**,
while the tool keeps cleaning up in background.

## Installation

Download the latest version of the .NET SDK from https://dotnet.microsoft.com/download

Then install the `cleanup` .NET Global Tool, using the command-line:

```
dotnet tool install --global dotnet-cleanup
```

## Usage

```
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

Include and exclude patterns are matched against paths relative to the given `PATH` (defaults to the current path).
Globbing supports `*`, `?`, and `**`. Exclusions take precedence over inclusions.
