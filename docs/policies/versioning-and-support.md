# Versioning and support policy

Tracked by [#5](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/5).  
Related: naming ADR [#4](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/4), goals [#6](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/6).

## Target framework

| Item | Policy |
|------|--------|
| Library TFM | **`net10.0` only** for the whole solution (src, tests, samples). Authoritative: [`Directory.Build.props`](../../Directory.Build.props). |
| CI / publish SDK | **.NET SDK 10.0.x** in every workflow that uses `actions/setup-dotnet`. |
| Multi-targeting | Out of scope for v2 unless a later ADR reopens it. |
| NuGet audit | Enabled in [`Directory.Build.props`](../../Directory.Build.props). |

## MongoDB driver and server matrix

| Component | Policy |
|-----------|--------|
| `MongoDB.Driver` | Version **pinned centrally** in [`Directory.Packages.props`](../../Directory.Packages.props). Bumps come through Dependabot with maintainer review. Telemetry/ActivitySource validation remains [#32](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/32). |
| MongoDB Server | **Full range published for the pinned driver** in [MongoDB .NET/C# driver compatibility](https://www.mongodb.com/docs/drivers/compatibility/?driver-language=csharp). Read the matrix row for the pin in `Directory.Packages.props`; do not hard-code a server list here. |
| Amazon DocumentDB / Cosmos DB Mongo API | Not a product identity; may work when the driver does, but are not separately certified in v2. |

## Dependency update rules

- Prefer central package management (`Directory.Packages.props`).
- Production dependencies stay minimal (see [v2 goals](../product/v2-goals.md)).
- Security advisories on transitive packages are tracked via Dependabot ([#10](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/10)) and security workflows ([#11](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/11)).

## SemVer

| Change | Version impact |
|--------|----------------|
| Breaking public API / package rename / TFM drop | Major |
| Backward-compatible features | Minor |
| Bug fixes, docs, non-functional packaging | Patch |
| Preview builds (target scheme) | `2.0.0-alpha.N`, then `2.0.0-rc.N` before GA `2.0.0` |

**Target** source of truth after [#30](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/30): git tags + NuGet.org (OIDC).

**Current (interim):** `.github/workflows/nuget-publish.yml` auto-patches from the latest `v*` tag and pushes to GitHub Packages (`nuget.pkg.github.com/Dilcore-Official`). `src/Directory.Build.props` still has a placeholder `Version`; pack version comes from the workflow. Do not copy current tag numbers into this policy.

## v1 → v2 migration policy

**Hard break. No shims.** Package/namespace mapping is canonical in [ADR 0001](../adr/0001-package-naming.md).

The rename is **implemented in this repository**. Consumer-facing migration narrative remains M8 ([#39](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/39)).

- No compatibility packages.
- No type-forward assemblies.
- No `[Obsolete]` aliases retained solely for DocumentDb names.

| v1 | v2 |
|----|----|
| `Dilcore.DocumentDb.Abstractions` | `Dilcore.MongoDB.Abstractions` |
| `Dilcore.DocumentDb.MongoDb.Abstractions` | `Dilcore.MongoDB.Abstractions` |
| `Dilcore.DocumentDb.MongoDb` | `Dilcore.MongoDB` |
| `Dilcore.DocumentDb.MongoDb.Repositories` | `Dilcore.MongoDB` |

## Deprecation policy (after v2 GA)

Applies to **post-v2** removals only:

1. Mark public API `[Obsolete("...", error: false)]` with replacement guidance.
2. Keep the obsolete API for **at least one minor release and at least 90 days**.
3. Remove only in a **major** version.
4. Record removals in [`CHANGELOG.md`](../../CHANGELOG.md).

## SUPPORT summary

- **Supported TFM:** net10.0 (`Directory.Build.props`)
- **Supported driver:** pin in `Directory.Packages.props`
- **Supported servers:** MongoDB’s published matrix for that pin
- **Product identity:** Dilcore MongoDB toolkit (not Amazon DocumentDB)
- **Publish today:** GitHub Packages via `nuget-publish.yml` until NuGet.org OIDC (#30)
