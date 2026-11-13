# Namespace resolution

**Current.** Physical database and collection names are resolved by a scoped ordered pipeline of `INamespaceSegmentContributor` implementations. The library has **no first-class tenant types**. Apps own prefix resolvers; missing prefixes should fail closed in your resolver (`Result.Fail`).

## Static prefix (registration-time)

```csharp
.AddDatabase("UserDB", db =>
{
    db.OnCluster("primary");
    db.WithNamespacePrefix("catalog"); // → catalog_UserDB
})
```

Document bindings can also call `WithNamespacePrefix`.

## Async prefix resolver

For prefixes loaded from storage, HTTP, or other async work, register `INamespacePrefixResolver` on a **database** or **document binding**. Dilcore registers the type as scoped.

```csharp
public sealed class TenantDatabasePrefixResolver : INamespacePrefixResolver
{
    private readonly ITenantStore _store; // your app service — not a Dilcore type

    public TenantDatabasePrefixResolver(ITenantStore store) => _store = store;

    public async Task<Result<string?>> ResolveAsync(
        NamespaceResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _store.GetCurrentAsync(cancellationToken);
        if (tenant is null)
            return Result.Fail<string?>("Tenant context is required.");

        return Result.Ok<string?>(tenant.Id);
    }
}

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("UserDB", db =>
    {
        db.OnCluster("primary");
        db.WithNamespacePrefix("catalog");
        db.WithNamespacePrefixResolver<TenantDatabasePrefixResolver>();
        db.AddDocumentBinding<User>("users", d => d
            .WithCollectionName("users")
            .WithNamespacePrefixResolver<FeatureCollectionPrefixResolver>());
    }));
```

Combined example: async `tenantA` + static `catalog` + logical `UserDB` → `tenantA_catalog_UserDB`.

## Cross-cutting contributor

For prefixes that apply across many databases/bindings without per-builder registration, implement `INamespaceSegmentContributor` and register it with DI (`AddScoped` / `TryAddEnumerable`). Prefer `WithNamespacePrefixResolver<T>` when the prefix is scoped to one database or binding.

## How resolution works

1. Contributors run in `Order` ascending (descriptor async resolvers at 90, static `WithNamespacePrefix` at 100) and may each emit a segment (or `null` to skip).
2. Segments are joined with `_` and validated as a physical MongoDB name.
3. When a database/binding has an async prefix resolver, that resolution is not cached within the scope (so a changed tenant context cannot reuse a stale physical name).
