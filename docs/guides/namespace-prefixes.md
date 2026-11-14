# Prefix database and collection names

Add static or per-environment prefixes to physical database and collection names. Package: `Dilcore.MongoDB`.

## Setup

```csharp
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;

public sealed class User : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("UserDB", db =>
    {
        db.OnCluster("primary");

        // Physical database name: catalog_UserDB
        db.WithNamespacePrefix("catalog");

        // A binding can add its own prefix to the collection name: feature_users
        db.AddDocumentBinding<User>("users", d => d
            .WithCollectionName("users")
            .WithNamespacePrefix("feature"));
    }));
```

## Use

Resolve the physical names through the same pipeline the repositories use:

```csharp
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using MongoDB.Driver;

public sealed class NameInspector(IMongoDbCollectionFactory collections)
{
    public async Task<string?> CollectionNameAsync(CancellationToken cancellationToken)
    {
        var collection = await collections.GetCollectionAsync<User>(
            new MongoDocumentBindingKey("users"), cancellationToken);

        return collection.IsSuccess
            ? $"{collection.Value.Database.DatabaseNamespace.DatabaseName}.{collection.Value.CollectionNamespace.CollectionName}"
            : null;
    }
}
```

Prefixes that apply across many databases without per-builder registration come from a cross-cutting `INamespaceSegmentContributor` (namespace `Dilcore.MongoDB.Abstractions.Namespace`), registered in DI as scoped:

```csharp
using Dilcore.MongoDB.Abstractions.Namespace;
using FluentResults;

public sealed class EnvironmentSegmentContributor : INamespaceSegmentContributor
{
    public int Order => 50;   // runs before the static prefix (100)

    public Task<Result<string?>> ContributeAsync(
        NamespaceResolutionRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Ok<string?>("staging"));
}

services.AddScoped<INamespaceSegmentContributor, EnvironmentSegmentContributor>();
```

A contributor runs for every database and collection name, so the example above turns `UserDB` into `staging_catalog_UserDB` and `users` into `staging_feature_users`. Return `null` for requests you do not want to prefix (check `request.Target`).

How resolution works:

1. Contributors run in ascending `Order` (async prefix resolvers at 90, static `WithNamespacePrefix` at 100). Each may emit a segment, or `null` to skip.
2. Segments are joined with `_` and the logical name is appended, then validated as a physical MongoDB name.
3. Names from an async prefix resolver are not cached within the scope, so a changed tenant context cannot reuse a stale name.

Prefer `WithNamespacePrefixResolver<T>` when the prefix belongs to one database or binding: [Multi-tenant database names](multi-tenant-databases.md).

## Handle failures

A contributor that returns a failed `Result` makes the repository or collection call return a failed `Result`. A segment that produces an invalid MongoDB name does the same. Neither is a `MongoOperationError`; check `IsFailed` and log the errors.

## Pitfalls

- A prefix of `""` or whitespace is rejected at registration. Do not use an empty prefix to mean "no prefix".
- Contributors are scoped. Do not cache their results in a singleton.
- Changing a prefix changes the physical name, so existing data stays under the old name.

## See also

- [Multi-tenant database names](multi-tenant-databases.md)
- [Driver escape hatches](../product/driver-escape-hatches.md)
- [Production MongoDB posture](../security/production-mongodb.md)
- Runnable example: [NamespacePrefixesExampleTests](../../test/IntegrationTests/Examples/NamespacePrefixesExampleTests.cs)
