using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.Repositories;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;
using Testcontainers.MongoDb;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var mongoDbContainer =
    new MongoDbBuilder("mongo:7.0").Build();

await mongoDbContainer.StartAsync();

var connectionString = mongoDbContainer.GetConnectionString();

builder.Services.AddMongoDb(mongo => mongo
    .ConfigureConventions(c => c.UseEnumRepresentation(BsonType.Int32))
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

var forecast = Enumerable.Range(1, 12).Select(index =>
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
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToHttpResult(result);
    })
    .WithName("GetWeatherForecast");

app.MapGet("/weather-forecasts/page", async (
        IGenericRepository<WeatherForecast> genericRepository,
        string? cursor,
        int? size) =>
    {
        var result = await genericRepository.GetPageAsync(new KeysetPageRequest<WeatherForecast>
        {
            Filter = Builders<WeatherForecast>.Filter.Empty,
            Sort = Builders<WeatherForecast>.Sort.Ascending(x => x.Date),
            PageSize = size is > 0 and <= 100 ? size.Value : 5,
            Cursor = cursor
        });

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToHttpResult(result);
    })
    .WithName("GetWeatherForecastPage");

app.MapPost("/weather-forecasts", async (IGenericRepository<WeatherForecast> genericRepository, WeatherForecast weatherForecast) =>
    {
        var result = await genericRepository.StoreAsync(weatherForecast);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToHttpResult(result);
    })
    .WithName("CreateWeatherForecast");

app.MapPut("/weather-forecasts/{id:guid}", async (
        Guid id,
        IGenericRepository<WeatherForecast> genericRepository,
        WeatherForecast weatherForecast) =>
    {
        weatherForecast.Id = id;
        var result = await genericRepository.ReplaceAsync(weatherForecast);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToHttpResult(result);
    })
    .WithName("ReplaceWeatherForecast");

app.MapDelete("/weather-forecasts/{id:guid}", async (
        Guid id,
        long? eTag,
        IGenericRepository<WeatherForecast> genericRepository) =>
    {
        var result = eTag is { } token
            ? await genericRepository.DeleteAsync(id, token)
            : await genericRepository.DeleteAsync(x => x.Id == id);

        return result.IsSuccess
            ? Results.NoContent()
            : ToHttpResult(result);
    })
    .WithName("DeleteWeatherForecast");

app.MapGet("/notes", async (IGenericRepository<Note> notes) =>
    {
        var result = await notes.GetListAsync();
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToHttpResult(result);
    })
    .WithName("GetNotes");

app.MapPost("/notes", async (IGenericRepository<Note> notes, Note note) =>
    {
        var result = await notes.StoreAsync(note);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToHttpResult(result);
    })
    .WithName("CreateNote");

app.Run();

static IResult ToHttpResult(ResultBase result)
{
    if (result.HasError<DocumentNotFoundError>())
    {
        return Results.NotFound(result.Errors);
    }

    if (result.HasError<ConcurrencyConflictError>() || result.HasError<DuplicateKeyError>())
    {
        return Results.Conflict(result.Errors);
    }

    if (result.HasError<TransientWriteError>())
    {
        return Results.Json(result.Errors, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.BadRequest(result.Errors);
}

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

/// <summary>Minimal document with text and priority. Priority is stored as BSON Int32 via ConfigureConventions.</summary>
record Note(string Text, NotePriority Priority) : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
}

enum NotePriority
{
    Low = 0,
    High = 1
}
