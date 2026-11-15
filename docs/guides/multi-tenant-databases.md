# Give each tenant its own database

Route requests to a per-tenant database with a scoped prefix resolver that fails closed. Package: `Dilcore.MongoDB`.

## Setup

The library has no tenant type. Your application owns the resolver and the tenant source.

```csharp
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")!;
var services = new ServiceCollection();

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("shop", db =>
    {
        db.OnCluster("primary");

        // Registered as scoped, so the resolver sees the current request's tenant.
        db.WithNamespacePrefixResolver<TenantDatabasePrefixResolver>();
        db.AddDocumentBinding<Order>("orders", d => d
            .WithCollectionName("orders")
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
    }));

// Your app's service. It knows the current tenant, for example from the request.
public interface ITenantAccessor
{
    Task<string?> GetCurrentTenantIdAsync(CancellationToken cancellationToken = default);
}

public sealed class TenantDatabasePrefixResolver(ITenantAccessor tenants) : INamespacePrefixResolver
{
    public async Task<Result<string?>> ResolveAsync(
        NamespaceResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = await tenants.GetCurrentTenantIdAsync(cancellationToken);

        // Fail closed. Never fall back to "" or a default tenant: that would use a shared database.
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return Result.Fail<string?>("Tenant context is required.");
        }

        return Result.Ok<string?>(tenantId);
    }
}

public sealed class Order : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = "";
}
```

## Use

```csharp
using Dilcore.MongoDB.Abstractions.Repositories;
using FluentResults;

public sealed class OrderService(IGenericRepository<Order> orders)
{
    // For tenant "tenantA" this writes to database "tenantA_shop".
    public Task<Result<Order>> CreateAsync(Order order, CancellationToken cancellationToken = default)
        => orders.StoreAsync(order, cancellationToken);
}
```

Prefixes combine in order. An async tenant prefix `tenantA`, a static `WithNamespacePrefix("catalog")` and the logical database `UserDB` resolve to `tenantA_catalog_UserDB`. A binding can add its own `WithNamespacePrefixResolver<T>()` for collection names.

## Handle failures

```csharp
var created = await service.CreateAsync(order, cancellationToken);
if (created.IsFailed)
{
    // No tenant (the resolver failed closed), an unknown database key, or an invalid physical name.
    // These are plain FluentResults errors, not MongoOperationError types. Do not retry.
}
```

Test the no-tenant case explicitly. It must return a failed `Result` and must not touch any database.

## Pitfalls

- Never return an empty or default prefix when the tenant is unknown. Return `Result.Fail`.
- Prefix resolvers are scoped. Do not cache the resolved prefix in a singleton.
- Resolve repositories from the request scope, so they pick up that request's tenant.
- Keep named bindings distinct for tenant-sensitive collections. JSON adapters and typed repositories resolve the same physical collection.

## See also

- [Prefix database and collection names](namespace-prefixes.md)
- [JSON documents with System.Text.Json](json-system-text-json.md)
- [Production MongoDB posture](../security/production-mongodb.md)
- Runnable example: [MultiTenantDatabasesExampleTests](../../test/IntegrationTests/Examples/MultiTenantDatabasesExampleTests.cs)
