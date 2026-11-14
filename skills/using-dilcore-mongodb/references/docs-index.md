# Documentation index

Canonical documentation for the Dilcore MongoDB packages. Prefer these pages over memory. Replace `main` with a version tag (for example `v2.0.0`) to read the docs of a released version.

Base URL: `https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/`

| Topic | Path |
|-------|------|
| Overview, packages, quick start | `README.md` |
| Application host package (DI, repositories, provisioning) | `src/Dilcore.MongoDB/README.md` |
| Contracts package | `src/Dilcore.MongoDB.Abstractions/README.md` |
| System.Text.Json adapters | `src/Dilcore.MongoDB.SystemTextJson/README.md` |
| Newtonsoft.Json adapters | `src/Dilcore.MongoDB.NewtonsoftJson/README.md` |
| Repositories, paging, bulk, projections, typed errors | `docs/guides/repositories.md` |
| Document policies, identifiers, ETag, soft delete, audit | `docs/guides/document-policies.md` |
| Namespace resolution and tenancy | `docs/guides/namespace-resolution.md` |
| Provisioning, indexes, TTL, custom steps | `docs/guides/provisioning.md` |
| Transactions and budgets | `docs/guides/transactions.md` |
| JSON adapters and conversion options | `docs/guides/json-adapters.md` |
| Package selection | `docs/product/package-selection.md` |
| Driver escape hatches | `docs/product/driver-escape-hatches.md` |
| BSON conventions and rollout | `docs/adr/0003-serialization-conventions.md` |
| Production MongoDB posture (TLS, secrets, least privilege) | `docs/security/production-mongodb.md` |
| Versioning and support | `docs/policies/versioning-and-support.md` |
| What is planned but not shipped | `ROADMAP.md` |
| Runnable samples | `samples/MongoDb.WebApi.Sample`, `samples/MongoDb.Capabilities.Sample` |

The exact public members of each package are listed in `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` next to each `src/*` project.
