# dotnet-cleanup

.NET Core Global Tool for cleaning up solution, project or folder.

The tool will list the files and folders which will be deleted,
and you will be prompted to confirm. Can be disabled with
the `-y|--confirm-cleanup` command-option.

Deleted files and folders are first moved to a temporary folder
before deletion, so **you can continue working with your projects**,
while the tool keeps cleaning up in background.

## Installation

Download the [.NET Core SDK 3.1](https://aka.ms/DotNetCore31) or later.
The install the [`dotnet-cleanup`](https://www.nuget.org/packages/dotnet-cleanup)
.NET Global Tool, using the command-line:

```
dotnet tool install -g dotnet-cleanup
```

## Usage

```
Usage: dotnet cleanup [arguments] [options]

Arguments:
  PATH                  Path to the solution-file, project-file or folder to clean. Defaults to current working directory.

Options:
  -p|--paths            Defines the paths to clean. Defaults to 'bin', 'obj' and 'node_modules'.
  -y|--confirm-cleanup  Confirm prompt for file cleanup automatically.
  -nd|--no-delete       Defines if files should be deleted, after confirmation.
  -nm|--no-move         Defines if files should be moved before deletion, after confirmation.
  -t|--temp-path        Directory in which the deleted items should be moved to before being cleaned up. Defaults to system Temp-folder.
  -v|--verbosity        Sets the verbosity level of the command. Allowed levels are Minimal, Normal, Detailed and Debug.
  -?|-h|--help          Show help information
```

The argument `PATH` can point to a specific `.sln`-file or
a project-file (`.csproj`, `.fsharp`, `.vbproj`).
If a `.sln`-file is specified, all its projects will be cleaned.

If it points to a folder, the folder will be scanned for a single
solution-file and then for a single project-file. If multiple files
are detected an error will be shown and you need to specify the file.

If not solution or project is found, the folder will be cleaned
as a project.

### Example

To cleanup a typical web-project, you can specify the paths
to be cleaned in the projects like this:

```
dotnet cleanup -p "bin" -p "obj"  -p "artifacts" -p "npm_modules"
```
