using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
{
    public Guid Id { get; set; }
    public long ETag { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}