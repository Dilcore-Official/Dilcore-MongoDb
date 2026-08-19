# Task completion checklist

1. Build and run the suites that match the change (architecture/unit without Docker; DI/repository integration only when Docker is available).
2. If public API changed, update `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` under the affected `src/Dilcore.MongoDB*` project.
3. Keep docs in the same change: README, ROADMAP.md (repo root), ADRs, `docs/api`, `docs/policies`, `docs/product`, samples, and CONTRIBUTING when the claim they make is no longer true.
4. Prefer small diffs that match the request; reuse existing helpers.
5. Do not commit `.serena/project.local.yml`, secrets, or machine-specific paths.
6. Link the related GitHub issue and fill the PR template.
