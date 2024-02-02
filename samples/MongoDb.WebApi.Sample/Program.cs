using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Extensions;
using Dilcore.DocumentDb.MongoDb.Repositories;
using Dilcore.DocumentDb.MongoDb.Repositories.Abstractions;
using Testcontainers.MongoDb;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var mongoDbContainer =
    new MongoDbBuilder().Build();

await mongoDbContainer.StartAsync();

var connectionString = mongoDbContainer.GetConnectionString();

builder.Services.AddMongoDb(configure => configure.UseConnectionString(connectionString), dbContainer =>
{
    dbContainer.AddDatabase("SampleDB",
        db =>
        {
            db.AddGenericRepository<WeatherForecast>(
                registerRepositoryAction: register => register.WithBulkRepository(), options =>
                {
                    options.WithCollectionName("weatherForecasts")
                        .WithDatabaseName("SampleDB");
                });
        });
});

var app = builder.Build();

async void Callback() => await mongoDbContainer.DisposeAsync();

app.Lifetime.ApplicationStopping.Register(Callback);

// Configure the HTTP request pipeline.
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
    .WithName("GetWeatherForecast")
    .WithOpenApi();
app.MapPost("/weather-forecasts", async (IGenericRepository<WeatherForecast> genericRepository, WeatherForecast weatherForecast) =>
    {
        var result = await genericRepository.StoreAsync(weatherForecast);

        if (result.IsSuccess)
        {
            return Results.Ok(result.ValueOrDefault); 
        }

        return Results.BadRequest(result.Errors);
    })
    .WithName("CreateWeatherForecast")
    .WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary) : IDocumentEntity
{
    public Guid Id { get; set; }
    public long ETag { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime UpdateAt { get; set; }
    
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}