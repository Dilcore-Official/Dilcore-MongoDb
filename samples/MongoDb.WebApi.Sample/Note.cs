using Dilcore.MongoDB.Abstractions;

/// <summary>Minimal document with text and priority. Priority is stored as BSON Int32 via ConfigureConventions.</summary>
record Note(string Text, NotePriority Priority) : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
}