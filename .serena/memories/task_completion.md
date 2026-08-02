# Task completion checklist

1. `dotnet build --configuration Release`
2. Run relevant unit tests; run integration tests only when Docker is available
3. If public API changed, update PublicAPI.*.txt baselines
4. Keep docs/ROADMAP links accurate for Dilcore-Official/Dilcore-MongoDb
5. Do not commit .serena/project.local.yml, secrets, or machine-specific paths
6. Prefer small diffs that match the request; reuse existing helpers
