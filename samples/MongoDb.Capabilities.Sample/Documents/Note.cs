using Dilcore.MongoDB.Abstractions;

namespace MongoDb.Capabilities.Sample.Documents;

/// <summary>Minimal document: typed identifier only, no optional policies.</summary>
public sealed class Note : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }

    public string Text { get; set; } = "";
}
