$project = "./src/DotnetCleanup/DotnetCleanup.csproj"

dotnet restore $project
dotnet build $project --no-restore -c Release
dotnet pack $project --no-build --no-restore -c Release --output '../..'
