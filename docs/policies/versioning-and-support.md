# Versioning and support policy

Tracked by [#5](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/5).  
Related: naming ADR [#4](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/4), goals [#6](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/6).

## Target framework

| Item | Policy |
|------|--------|
| Library TFM | **`net10.0` only** for the whole solution (src, tests, samples) |
| CI / publish SDK | **.NET SDK 10.0.x** (aligned in `.github/workflows/ci.yml` and `nuget-publish.yml`) |
| Multi-targeting | Out of scope for v2 unless a later ADR reopens it |

## MongoDB driver and server matrix

| Component | Supported range |
|-----------|-----------------|
| `MongoDB.Driver` | Currently pinned to **3.5.2** in `Directory.Packages.props`; upgrade deliberately via Dependabot / M6 work |
| MongoDB Server | **Full range supported by the pinned driver**. For MongoDB.Driver 3.5.x that is **MongoDB Server 4.2 through 8.0** per [MongoDB .NET/C# driver compatibility](https://www.mongodb.com/docs/drivers/compatibility/?driver-language=csharp) |
| Amazon DocumentDB / Cosmos DB Mongo API | Not a product identity; may work when the driver does, but are not separately certified in v2 |

When the driver pin advances (for example to 3.10+), update this matrix to match the driver’s published server floor.

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
| Preview builds | `2.0.0-alpha.N`, then `2.0.0-rc.N` before GA `2.0.0` |

Source of truth for published versions will be release tags and NuGet packages (OIDC NuGet.org publishing lands in [#30](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/30)). Fixed `Version=1.0.0` in props and auto-patch main publishes are transitional defects until that work completes.

## v1 → v2 migration policy

**Hard break. No shims.**

Because there are no known consumers:

- No compatibility packages.
- No type-forward assemblies.
- No `[Obsolete]` aliases retained solely for DocumentDb names.

Document a package/namespace mapping in the M8 migration guide ([#39](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/39)):

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
4. Record removals in `CHANGELOG.md` (lands in [#8](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/8)).

## SUPPORT summary

- **Supported TFM:** net10.0
- **Supported driver:** MongoDB.Driver 3.5.2 (current pin)
- **Supported servers:** MongoDB 4.2–8.0 (driver-supported range)
- **Product identity:** Dilcore MongoDB toolkit (not Amazon DocumentDB)
