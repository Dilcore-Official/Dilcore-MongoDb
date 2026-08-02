# Style and conventions

- C# with nullable enabled, implicit usings, LangVersion latest, net10.0.
- Central package versions in Directory.Packages.props.
- Prefer existing patterns: FluentResults for operation outcomes; NUnit + Shouldly in tests (user preference mentions FluentAssertions for new assertions when introduced).
- Keep imports at top; no inline imports.
- Exhaustive switch over unions/enums with never default when adding TS (N/A for C# here).
- Do not expand public API without updating PublicAPI.Shipped.txt / Unshipped.txt baselines under src/*.
- Product positioning: MongoDB toolkit only — do not claim provider-neutral DocumentDB or Clean Architecture marketing.
