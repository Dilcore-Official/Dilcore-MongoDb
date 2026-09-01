# Document policies

**Current.** Documents implement `IDocumentEntity<TId>` for a typed identifier. Concurrency, soft delete, and audit stamps are **opt-in** interfaces. Decision: [ADR 0002](../adr/0002-generic-document-identifier.md).

Getting-started: [MongoDb.WebApi.Sample](../../samples/MongoDb.WebApi.Sample). Catalog: [MongoDb.Capabilities.Sample](../../samples/MongoDb.Capabilities.Sample).

## Marker and identifier

```csharp
public interface IDocumentEntity { }

public interface IDocumentEntity<TId> : IDocumentEntity
{
    TId Id { get; set; }
}
```

Repositories stay single-generic (`IGenericRepository<TDocument>`). Identifier type is not a second type argument.

### Guid

```csharp
public sealed class Note : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
    public string Text { get; set; } = "";
}
```

Opt into RFC 9562 UUID v7 per binding:

```csharp
d.WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7)
```

### ObjectId

```csharp
using MongoDB.Bson;

public sealed class LegacyDoc : IDocumentEntity<ObjectId>
{
    public ObjectId Id { get; set; }
    public string Name { get; set; } = "";
}
```

Leave `Id` default and let the driver assign an ObjectId on insert, or set it yourself. Do not call `WithGuidIdGeneration` on non-Guid identifiers.

## Optional policies

```csharp
public interface IHasConcurrencyToken { long ETag { get; set; } }
public interface ISoftDeletable { bool IsDeleted { get; set; } }
public interface IAuditableDocument
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
```

Compose only what you need:

```csharp
public sealed class Order : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
{
    public Guid Id { get; set; }
    public long ETag { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Sku { get; set; } = "";
}
```

Enable soft-delete **filters** on the binding (`WithSoftDelete()`). Policy fields are staged and applied only after an acknowledged write. `ETag` is a non-zero random 64-bit token.

`DeleteAsync` with matching `ETag` soft-deletes when the binding has `WithSoftDelete()`. `RestoreAsync` clears the flag; `PurgeAsync` hard-deletes. See [repositories.md](repositories.md).

Wrong `ETag` maps to `ConcurrencyConflictError`.
