# Release and versioning

- Policy: docs/policies/versioning-and-support.md
- TFM net10.0; CI SDK 10.0.x
- MongoDB.Driver pin currently 3.5.2; server support = driver-supported range (4.2–8.0 for 3.5.x)
- v1→v2 is a hard break rename to Dilcore.MongoDB; no compatibility packages
- After v2 GA: deprecate at least one minor / 90 days before major removal
- Current publish workflow still auto-patches to GitHub Packages (aytymchuk feed) — transitional until OIDC NuGet.org (#30)
- Package metadata RepositoryUrl still stale until packaging milestone
