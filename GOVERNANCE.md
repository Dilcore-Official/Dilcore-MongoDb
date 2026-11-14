# Governance

## Maintainers

| Maintainer | GitHub |
|------------|--------|
| Arsen | [@aytymchuk](https://github.com/aytymchuk) |

Maintainers have write access to the repository, merge authority for pull
requests that pass required checks, and responsibility for releases, security
triage, and roadmap prioritization.

## Decision making

- Day-to-day technical decisions are made by the maintainer(s) via pull request
  review.
- Product and breaking-change decisions for v2 are tracked as roadmap issues and
  Architecture Decision Records under `docs/adr/`.
- Community feedback is welcome through Issues and Discussions; consensus is
  preferred, but the maintainer has final say when trade-offs are required.

## Releases

The maintainer owns releases. A release is a version tag on `main`; the publish workflow tests the commit,
publishes to NuGet.org through OIDC trusted publishing after approval of the `nuget` GitHub environment, and
creates the GitHub Release. Steps and one-time setup are in
[CONTRIBUTING.md](CONTRIBUTING.md#releasing). Package descriptions and tags follow
[docs/product/package-descriptions.md](docs/product/package-descriptions.md).

## Contributions

Contributions are welcome under the [MIT License](LICENSE). See
[CONTRIBUTING.md](CONTRIBUTING.md) for workflow, coding expectations, and pull
request requirements. All participants must follow the
[Code of Conduct](CODE_OF_CONDUCT.md).

## Escalation

Security reports follow [SECURITY.md](SECURITY.md). Conduct issues may be
reported to [@aytymchuk](https://github.com/aytymchuk) as described in the Code
of Conduct.
