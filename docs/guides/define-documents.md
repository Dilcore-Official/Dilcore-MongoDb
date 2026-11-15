# Define document types

Declare documents with a typed identifier (`Guid` or `ObjectId`) and opt in to policies. Package: `Dilcore.MongoDB` (the contracts live in `Dilcore.MongoDB.Abstractions`).

## Setup

```csharp
using System;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")!;
var services = new ServiceCollection();

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("app", db =>
    {
        db.OnCluster("primary");

        // Time-ordered UUID v7 ids, generated on insert. Guid documents only.
        db.AddDocumentBinding<Note>("notes", d => d
            .WithCollectionName("notes")
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));

        // No id generation call: the driver assigns the ObjectId on insert.
        db.AddDocumentBinding<LegacyDoc>("legacy", d => d.WithCollectionName("legacy"));
    }));

// A Guid document that also stamps created and updated times.
public sealed class Note : IDocumentEntity<Guid>, IAuditableDocument
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Text { get; set; } = "";
}

// An ObjectId document.
public sealed class LegacyDoc : IDocumentEntity<ObjectId>
{
    public ObjectId Id { get; set; }
    public string Name { get; set; } = "";
}
```

## Use

```csharp
using Dilcore.MongoDB.Abstractions.Repositories;
using FluentResults;

public sealed class NoteService(IGenericRepository<Note> notes)
{
    public async Task<Result<Note>> AddAsync(string text, CancellationToken cancellationToken = default)
    {
        // The Id stays empty: the repository generates a v7 Guid and stamps CreatedAt / UpdatedAt.
        var note = new Note { Text = text };

        return await notes.StoreAsync(note, cancellationToken);
    }
}
```

The marker is `IDocumentEntity`; `IDocumentEntity<TId>` adds `TId Id { get; set; }`. Repositories stay single-generic (`IGenericRepository<TDocument>`): the identifier type is not a second type argument. The decision is recorded in [ADR 0002](../adr/0002-generic-document-identifier.md).

Optional policy interfaces (namespace `Dilcore.MongoDB.Abstractions.Policies`) compose on the document:

| Interface | Members | Guide |
|-----------|---------|-------|
| `IHasConcurrencyToken` | `long ETag` | [Optimistic concurrency](optimistic-concurrency.md) |
| `ISoftDeletable` | `bool IsDeleted` | [Soft delete](soft-delete.md) |
| `IAuditableDocument` | `DateTime CreatedAt`, `DateTime UpdatedAt` | this page |

Audit fields and a generated `Id` are staged on the entity you pass in before the write. If the write fails with a `MongoException`, an oversized BSON document or cancellation, they are restored. Field names follow the document's BSON mapping, so `[BsonElement]` renames are honored.

## Handle failures

| Situation | Result |
|-----------|--------|
| `WithGuidIdGeneration` on a non-`Guid` document | `InvalidOperationException` at registration |
| Document larger than 16 MiB | failed `Result` with `DocumentTooLargeError` |

```csharp
using Dilcore.MongoDB.Abstractions.Results;

var result = await notes.StoreAsync(note, cancellationToken);
if (result.HasError<DocumentTooLargeError>())
{
    // The BSON document is over MongoDB's 16 MiB limit. Store less data per document.
}
```

## Pitfalls

- `WithGuidIdGeneration` is only for `Guid` identifiers. Never use it with `ObjectId`.
- Add only the policy interfaces you need. Nothing is added implicitly.
- Do not call `.Value` on a result you have not checked.

## See also

- [Register clusters and databases](register-clusters-and-databases.md)
- [CRUD with repositories](crud-with-repositories.md)
- [ADR 0002: generic document identifier](../adr/0002-generic-document-identifier.md)
- Runnable example: [DefineDocumentsExampleTests](../../test/Repositories.IntegrationTests/Examples/DefineDocumentsExampleTests.cs)
