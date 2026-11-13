# Document policies

**Current.** Documents implement `IDocumentEntity<TId>` for a typed identifier. Concurrency, soft delete, and audit stamps are **opt-in** interfaces. Decision: [ADR 0002](../adr/0002-generic-document-identifier.md).

Getting-started: [MongoDb.WebApi.Sample](../../samples/MongoDb.WebApi.Sample).

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

Enable soft-delete **filters** on the binding (`WithSoftDelete()`). Policy fields (ETag, audit, and a generated Id on create) are staged on the caller entity before the write; on `MongoException`, oversized BSON `FormatException`, or cancellation they are restored (a generated Id is cleared only on cancellation—failed inserts keep the Id so retries stay on the insert path). `ETag` is a non-zero random 64-bit token.

`DeleteAsync` with matching `ETag` soft-deletes when the binding has `WithSoftDelete()`. Restore and purge APIs are tracked in [#19](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/19).

Wrong `ETag` on `StoreAsync` / `ReplaceAsync` / `UpdateSnapshotAsync` / `DeleteAsync(id, eTag)` maps to `ConcurrencyConflictError`. Policy filters and stamps use each document type's BSON element names.
