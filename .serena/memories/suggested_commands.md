# Suggested commands

- `dotnet restore Dilcore.MongoDB.sln`
- `dotnet build Dilcore.MongoDB.sln -c Release`
- `dotnet test Dilcore.MongoDB.sln -c Release` (needs Docker for integration/DI acceptance)
- `dotnet test test/UnitTests test/ArchitectureTests -c Release` (no Docker)
- `dotnet test test/IntegrationTests -c Release` (DI acceptance; needs Docker)
- `dotnet pack Dilcore.MongoDB.sln -c Release -o ./nupkgs`
- `./scripts/verify-roadmap-coverage.sh`
