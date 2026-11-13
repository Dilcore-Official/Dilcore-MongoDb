# Style and conventions

- Product positioning: MongoDB toolkit only. Do not claim provider-neutral DocumentDB or Clean Architecture marketing.
- Prefer existing patterns: FluentResults for operation outcomes; NUnit + Shouldly in tests (not FluentAssertions).
- Change C# with Serena, per the tool workflow in CONTRIBUTING.md; keep imports at the top of the file; no inline imports.
- Do not expand public API without updating `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` under the affected `src/Dilcore.MongoDB*` project.
- Formatting source of truth: root `.editorconfig`. C# files have no trailing newline. Verify with `dotnet format` as documented in CONTRIBUTING.md.
- Public XML docs are required (`CS1591` on `src/`). Document interface members on the interface; use `<inheritdoc />` on the implementation.
- `CancellationToken` parameters are optional and last, and are forwarded into cancellable calls.
- DI registration changes need a lifetime test (`ValidateScopes` and `ValidateOnBuild`). See `.cursor/rules/csharp-code-style.mdc` and `.cursor/rules/csharp-testing-di.mdc`.
- Remove unused `using` directives (`IDE0005` is an error). Remove a NuGet package the project does not use, and a central `PackageVersion` no project references.
- TFM, nullable, implicit usings, and central package versions live in MSBuild props — do not duplicate pins here.
