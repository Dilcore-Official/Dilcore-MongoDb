using Dilcore.MongoDB.Abstractions;

namespace MongoDb.Capabilities.Sample.Documents;

/// <summary>Second collection used with <see cref="Order"/> inside one multi-document transaction.</summary>
public sealed class Payment : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public decimal Amount { get; set; }
}
