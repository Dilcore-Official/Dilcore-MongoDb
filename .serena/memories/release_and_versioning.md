# Release and versioning

Policy: [docs/policies/versioning-and-support.md](../../docs/policies/versioning-and-support.md).

- TFM and SDK: [`Directory.Build.props`](../../Directory.Build.props) and `.github/workflows` (`actions/setup-dotnet`). Do not copy the pin into this memory.
- Driver pin and server range: `Directory.Packages.props` plus MongoDB’s published C# compatibility matrix — do not copy versions into memories.
- v1→v2 is a hard rename to `Dilcore.MongoDB` / `Dilcore.MongoDB.Abstractions`; no compatibility shims ([ADR 0001](../../docs/adr/0001-package-naming.md)).
- After v2 GA: deprecate at least one minor / 90 days before major removal.
- Interim publish is GitHub Packages via `.github/workflows/nuget-publish.yml` until OIDC NuGet.org (#30). Tags and workflow are the version source of truth, not `src/Directory.Build.props`.
