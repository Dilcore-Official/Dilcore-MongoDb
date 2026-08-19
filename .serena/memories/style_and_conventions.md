# Style and conventions

- Product positioning: MongoDB toolkit only. Do not claim provider-neutral DocumentDB or Clean Architecture marketing.
- Prefer existing patterns: FluentResults for operation outcomes; NUnit + Shouldly in tests (not FluentAssertions).
- Keep imports at the top of the file; no inline imports.
- Do not expand public API without updating `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` under the two `src/Dilcore.MongoDB*` projects.
- Formatting source of truth: root `.editorconfig`. Verify with `dotnet format` as documented in CONTRIBUTING.md.
- TFM, nullable, implicit usings, and central package versions live in MSBuild props — do not duplicate pins here.
