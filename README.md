# dotnet-cleanup

.NET Global Tool for cleaning up build output and dependency folders.

The tool will list the files and folders which will be deleted,
and you will be prompted to confirm. Can be disabled with
the `-y|--confirm-cleanup` command-option.

Deleted files and folders are first moved to a temporary folder
before deletion, so **you can continue working with your projects**,
while the tool keeps cleaning up in background.

## Installation

Download the .NET SDK 10.0 or later.
Then install the `cleanup` .NET Global Tool, using the command-line:

```
dotnet tool install -g cleanup
```

## Usage

```
Usage: cleanup [arguments] [options]

Arguments:
  PATH                  Root directory to scan. Defaults to current working directory.

Options:
  -p|--paths            Defines the paths to clean (supports globbing). Defaults to 'bin', 'obj' and 'node_modules'.
  -x|--exclude          Defines the paths to exclude from cleanup (supports globbing). Exclusions take precedence.
  -y|--confirm-cleanup  Confirm prompt for file cleanup automatically.
  -nd|--no-delete       Defines if files should be deleted, after confirmation.
  -nm|--no-move         Defines if files should be moved before deletion, after confirmation.
  -t|--temp-path        Directory in which the deleted items should be moved to before being cleaned up. Defaults to system Temp-folder.
  -v|--verbosity        Sets the verbosity level of the command. Allowed levels are Minimal, Normal, Detailed and Debug.
  -h|--help             Show help information
```

Include and exclude patterns are matched against paths relative to the root directory.
Globbing supports `*`, `?`, and `**`. Exclusions take precedence over inclusions.
Patterns without separators are treated as recursive matches (equivalent to `**/pattern`).

### Example

Clean typical build output and dependency folders:

```
cleanup -p "**/bin" -p "**/obj" -p "**/node_modules"
```

Exclude nested paths:

```
cleanup -p "**/bin" -p "**/obj" -x "**/obj"
```
