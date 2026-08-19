# Release and versioning

Policy: [docs/policies/versioning-and-support.md](../../docs/policies/versioning-and-support.md).

- TFM and SDK: `net10.0` / CI `10.0.x` (see `Directory.Build.props` and workflows).
- Driver pin and server range: `Directory.Packages.props` plus MongoDB’s published C# compatibility matrix — do not copy versions into memories.
- v1→v2 is a hard rename to `Dilcore.MongoDB` / `Dilcore.MongoDB.Abstractions`; no compatibility shims ([ADR 0001](../../docs/adr/0001-package-naming.md)).
- After v2 GA: deprecate at least one minor / 90 days before major removal.
- Interim publish is GitHub Packages via `.github/workflows/nuget-publish.yml` until OIDC NuGet.org (#30). Tags and workflow are the version source of truth, not `src/Directory.Build.props`.
