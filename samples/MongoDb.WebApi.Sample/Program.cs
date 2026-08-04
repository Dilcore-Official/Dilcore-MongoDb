using Dilcore.MongoDB.Abstractions;
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
    .AddDatabase("SampleDB", db => db.OnCluster("primary"))
    .AddDocumentBinding<WeatherForecast>("weather", d => d
        .InDatabase("SampleDB")
        .WithCollectionName("weatherForecasts")
        .WithBulkRepository()));

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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary) : IDocumentEntity
{
    public Guid Id { get; set; }
    public long ETag { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
