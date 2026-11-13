# Security policy

## Supported versions

Security fixes are applied to the actively maintained branch of this repository.
See [docs/policies/versioning-and-support.md](docs/policies/versioning-and-support.md)
for the supported target framework, MongoDB driver pin, and SemVer policy.

| Version | Supported |
|---------|-----------|
| `main` / pre-v2 development | Yes |
| Published v1 NuGet packages | Limited — critical fixes may be backported at maintainer discretion until v2 GA |

## Reporting a vulnerability

**Do not open a public GitHub issue for security reports.**

Use [GitHub private vulnerability reporting](https://github.com/Dilcore-Official/Dilcore-MongoDb/security/advisories/new)
for this repository. Include:

- Affected package(s) and versions when known
- A clear description of the issue and impact
- Steps to reproduce or a proof of concept when possible
- Any known mitigations

We aim to acknowledge reports within **7 days** and to provide a remediation
plan or status update within **30 days**. Timing may vary with severity and
upstream dependency constraints.

## Dependency and automation scope

- Dependabot maintains **NuGet** packages and **GitHub Actions** workflow
  references (see [`.github/dependabot.yml`](.github/dependabot.yml) and
  [#10](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/10)).
- Dependabot does **not** update arbitrary YAML keys or non-ecosystem config
  values; those remain manual.
- Workflow Actions use stable version tags (for example `@v7`); Dependabot
  bumps those tags on the weekly GitHub Actions schedule
  ([#11](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/11)).
- Automated checks include dependency review, CodeQL, OpenSSF Scorecard, and
  workflow YAML validation. Findings appear in pull requests and the repository
  Security tab when enabled.

Production host guidance (TLS, secrets, redaction, least privilege, tenant
namespace isolation): [docs/security/production-mongodb.md](docs/security/production-mongodb.md).

## Coordinated disclosure

Please allow time for a fix and advisory before public disclosure. Credit will
be given to reporters who wish to be named, unless they request anonymity.
