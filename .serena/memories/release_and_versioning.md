# Release and versioning

Policy: [docs/policies/versioning-and-support.md](../../docs/policies/versioning-and-support.md).

- TFM and SDK: [`Directory.Build.props`](../../Directory.Build.props) and `.github/workflows` (`actions/setup-dotnet`). Do not copy the pin into this memory.
- Driver pin and server range: `Directory.Packages.props` plus MongoDB’s published C# compatibility matrix — do not copy versions into memories.
- v1→v2 is a hard rename to `Dilcore.MongoDB` / `Dilcore.MongoDB.Abstractions`; no compatibility shims ([ADR 0001](../../docs/adr/0001-package-naming.md)).
- After v2 GA: deprecate at least one minor / 90 days before major removal.
- Publish is NuGet.org via OIDC trusted publishing in `.github/workflows/nuget-publish.yml`, triggered by a `v*` tag on `main` (#30). The tag is the version source of truth, not `src/Directory.Build.props`. Release steps live in CONTRIBUTING.md, "Releasing".
