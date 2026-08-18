using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;

namespace Dilcore.MongoDB.Benchmarks.Models;

public sealed class BenchmarkEntity : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
{
    public Guid Id { get; set; }

    public long ETag { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? Name { get; set; }

    public int Value { get; set; }
}

public sealed class BenchmarkEntityProjection
{
    public Guid Id { get; set; }

    public string? Name { get; set; }
}
