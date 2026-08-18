using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.Repositories;
using Testcontainers.MongoDb;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var mongoDbContainer =
    new MongoDbBuilder("mongo:7.0").Build();

await mongoDbContainer.StartAsync();

var connectionString = mongoDbContainer.GetConnectionString();

builder.Services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("SampleDB", db =>
    {
        db.OnCluster("primary");
        // Fully composed document: concurrency + soft delete + audit timestamps.
        db.AddDocumentBinding<WeatherForecast>("weather", d => d
            .WithCollectionName("weatherForecasts")
            .WithSoftDelete()
            .WithBulkRepository()
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
        // Minimal document: identifier only.
        db.AddDocumentBinding<Note>("notes", d => d
            .WithCollectionName("notes"));
    }));

var app = builder.Build();

async void Callback() => await mongoDbContainer.DisposeAsync();

app.Lifetime.ApplicationStopping.Register(Callback);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

#region Seed Data

var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
using var scope = scopeFactory.CreateScope();

var repository = scope.ServiceProvider.GetRequiredService<IGenericBulkRepository<WeatherForecast>>();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
    .ToArray();

await repository.BulkStoreAsync(forecast);
#endregion


app.MapGet("/weather-forecasts", async (IGenericRepository<WeatherForecast> genericRepository) =>
    {
        var result = await genericRepository.GetListAsync();

        if (result.IsSuccess)
        {
            return Results.Ok(result.ValueOrDefault);
        }

        return Results.BadRequest(result.Errors);
    })
    .WithName("GetWeatherForecast");
app.MapPost("/weather-forecasts", async (IGenericRepository<WeatherForecast> genericRepository, WeatherForecast weatherForecast) =>
    {
        var result = await genericRepository.StoreAsync(weatherForecast);

        if (result.IsSuccess)
        {
            return Results.Ok(result.ValueOrDefault);
        }

        return Results.BadRequest(result.Errors);
    })
    .WithName("CreateWeatherForecast");

app.MapGet("/notes", async (IGenericRepository<Note> notes) =>
    {
        var result = await notes.GetListAsync();
        return result.IsSuccess ? Results.Ok(result.ValueOrDefault) : Results.BadRequest(result.Errors);
    })
    .WithName("GetNotes");

app.MapPost("/notes", async (IGenericRepository<Note> notes, Note note) =>
    {
        var result = await notes.StoreAsync(note);
        return result.IsSuccess ? Results.Ok(result.ValueOrDefault) : Results.BadRequest(result.Errors);
    })
    .WithName("CreateNote");

app.Run();

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

/// <summary>Minimal document: identifier only, no optional policies.</summary>
record Note(string Text) : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
}
