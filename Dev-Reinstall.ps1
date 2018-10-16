dotnet tool uninstall -g dotnet-cleanup

dotnet pack 'src/DotnetCleanup/DotnetCleanup.csproj' --output ./

dotnet tool install -g dotnet-cleanup --add-source 'src/DotnetCleanup/'
