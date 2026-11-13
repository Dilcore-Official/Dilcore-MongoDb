using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;

namespace MongoDb.Capabilities.Sample.Documents;

/// <summary>
/// Fully composed document: identifier plus concurrency, soft delete, and audit policies.
/// </summary>
public sealed class Order : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
{
    public Guid Id { get; set; }

    public long ETag { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string Sku { get; set; } = "";

    public int Quantity { get; set; }

    /// <summary>TTL index target; MongoDB expires documents after <c>ExpiresAt</c> plus the binding TTL.</summary>
    public DateTime ExpiresAt { get; set; }
}
