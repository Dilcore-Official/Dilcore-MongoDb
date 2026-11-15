# Soft delete and restore documents

Hide deleted documents from reads and bring them back with `ISoftDeletable`. Package: `Dilcore.MongoDB`.

## Setup

```csharp
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;

var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")!;
var services = new ServiceCollection();

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("crm", db =>
    {
        db.OnCluster("primary");

        // The document opts in with ISoftDeletable. The binding turns on the read filters.
        db.AddDocumentBinding<Customer>("customers", d => d
            .WithCollectionName("customers")
            .WithSoftDelete()
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
    }));

public sealed class Customer : IDocumentEntity<Guid>, ISoftDeletable
{
    public Guid Id { get; set; }
    public bool IsDeleted { get; set; }   // managed by the repository
    public string Name { get; set; } = "";
}
```

## Use

```csharp
using Dilcore.MongoDB.Abstractions.Repositories;
using FluentResults;
using MongoDB.Driver;

public sealed class CustomerService(IGenericRepository<Customer> customers)
{
    // Marks the document deleted. Reads, counts and HasAny no longer see it.
    public Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Customer>.Filter.Eq(x => x.Id, id);

        return customers.DeleteAsync(filter, cancellationToken);
    }

    // Clears the flag.
    public Task<Result<bool>> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Customer>.Filter.Eq(x => x.Id, id);

        return customers.RestoreAsync(filter, cancellationToken);
    }

    // Permanent. Deletes every match, including documents that are not soft-deleted.
    public Task<Result<long>> PurgeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Customer>.Filter.Eq(x => x.Id, id);

        return customers.PurgeAsync(filter, cancellationToken);
    }
}
```

## Handle failures

```csharp
using Dilcore.MongoDB.Abstractions.Results;

var deleted = await customers.DeleteAsync(filter, cancellationToken);
if (deleted.HasError<DocumentNotFoundError>())
{
    // Nothing matched, or it is already soft-deleted.
}
```

- `DeleteAsync` with no match returns `DocumentNotFoundError`.
- With `IHasConcurrencyToken` on the document, `DeleteAsync(id, eTag)` with a stale token returns `ConcurrencyConflictError`.
- `WithSoftDelete()` on a type that does not implement `ISoftDeletable` throws `InvalidOperationException` at registration.

## Pitfalls

- Implementing `ISoftDeletable` is not enough: call `WithSoftDelete()` on the binding to enable the filters.
- `PurgeAsync` is permanent and ignores the soft-delete flag. Use it only when permanent deletion is the intent.
- `RestoreAsync` only finds soft-deleted documents.

## See also

- [Optimistic concurrency](optimistic-concurrency.md)
- [Create, read and update documents](crud-with-repositories.md)
- [Handle typed errors](handle-errors.md)
- Runnable example: [SoftDeleteExampleTests](../../test/Repositories.IntegrationTests/Examples/SoftDeleteExampleTests.cs)
