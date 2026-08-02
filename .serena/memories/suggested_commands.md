# Suggested commands

## Build / test
- `dotnet restore`
- `dotnet build --configuration Release`
- `dotnet test test/UnitTests --configuration Release`
- `dotnet test --configuration Release` (needs Docker for Testcontainers integration tests)

## Pack (local)
- `dotnet pack Dilcore.DocumentDb.sln -c Release -o ./nupkgs`

## Roadmap verification
- `./scripts/verify-roadmap-coverage.sh`

## System utilities (Darwin)
- `git`, `ls`, `cd`, `rg`/`grep`, `find`, `gh`, `jq` for GitHub issues and roadmap verification
