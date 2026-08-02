# Suggested commands

## Build / test
- `dotnet restore`
- `dotnet build --configuration Release`
- `dotnet test test/UnitTests --configuration Release`
- `dotnet test --configuration Release` (needs Docker for Testcontainers integration tests)

## Pack (local)
- `dotnet pack src/Dilcore.DocumentDb.*/Dilcore.DocumentDb.*.csproj -c Release -o ./nupkgs`

## Roadmap verification
- `./scripts/verify-roadmap-coverage.sh`

## System utilities (Darwin)
- `git`, `ls`, `cd`, `rg`/`grep`, `find`, `gh` for GitHub issues
