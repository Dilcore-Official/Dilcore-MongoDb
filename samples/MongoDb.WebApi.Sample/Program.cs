// Getting-started sample: one cluster, two document bindings, CRUD, and startup provisioning.
// Production hosts should inject a connection string instead of starting Testcontainers here (see D22).
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Provisioning;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.NewtonsoftJson;
using Dilcore.MongoDB.Repositories;
using Dilcore.MongoDB.SystemTextJson;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using Testcontainers.MongoDb;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var mongoDbContainer =
    new MongoDbBuilder("mongo:9.0").Build();

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
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7)
            .WithIndexes(new CreateIndexModel<WeatherForecast>(
                Builders<WeatherForecast>.IndexKeys.Ascending(x => x.Date),
                new CreateIndexOptions { Name = "weather_date" })));
        // Minimal document: identifier only.
        db.AddDocumentBinding<Note>("notes", d => d
            .WithCollectionName("notes"));
    }));

// Optional JSON adapters — each package registers its own store facade after AddMongoDb.
builder.Services.AddSystemTextJsonDocuments();
builder.Services.AddNewtonsoftJsonDocuments();

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

var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
var provisioned = await provisioner.ApplyAsync();
if (provisioned.IsFailed)
{
    throw new InvalidOperationException(string.Join("; ", provisioned.Errors.Select(e => e.Message)));
}

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

var sampleDatabase = new MongoDatabaseKey("SampleDB");
var canonicalJson = new JsonBsonConversionOptions
{
    OutputMode = JsonBsonOutputMode.CanonicalExtendedJson
};

app.MapPost("/json/{collection}", async (
        string collection,
        HttpRequest request,
        ISystemTextJsonDocumentStoreFactory storeFactory) =>
    {
        using var document = await JsonDocument.ParseAsync(request.Body);
        var store = storeFactory.Create(sampleDatabase);
        var result = await store.InsertAsync(collection, document, canonicalJson);
        return result.IsSuccess
            ? Results.Ok(JsonDocument.Parse(
                store.Converter.ToJson(result.Value, canonicalJson).Value).RootElement.Clone())
            : ToHttpResult(result);
    })
    .WithName("InsertJsonDocument");

app.MapGet("/json/{collection}/{id}", async (
        string collection,
        string id,
        ISystemTextJsonDocumentStoreFactory storeFactory) =>
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return Results.BadRequest("id must be a 24-char ObjectId hex string.");
        }

        var store = storeFactory.Create(sampleDatabase);
        var result = await store.GetJsonDocumentByIdAsync(collection, objectId, canonicalJson);
        if (result.IsFailed)
        {
            return ToHttpResult(result);
        }

        using (result.Value)
        {
            return Results.Ok(result.Value.RootElement.Clone());
        }
    })
    .WithName("GetJsonDocument");

app.MapPost("/ns-json/{collection}", async (
        string collection,
        HttpRequest request,
        INewtonsoftJsonDocumentStoreFactory storeFactory) =>
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        var token = JToken.Parse(body);
        var store = storeFactory.Create(sampleDatabase);
        var result = await store.InsertAsync(collection, token, canonicalJson);
        if (result.IsFailed)
        {
            return ToHttpResult(result);
        }

        var json = store.Converter.ToJson(result.Value, canonicalJson);
        return json.IsSuccess
            ? Results.Content(json.Value, "application/json")
            : ToHttpResult(json);
    })
    .WithName("InsertNewtonsoftJsonDocument");

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