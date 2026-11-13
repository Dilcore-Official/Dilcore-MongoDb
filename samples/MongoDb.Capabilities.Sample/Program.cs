// Capability catalog: replica-set MongoDB so multi-document transactions work.
// Getting-started (standalone, two bindings): samples/MongoDb.WebApi.Sample
// Production hosts should inject a connection string instead of starting Testcontainers here.
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Provisioning;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.Json;
using MongoDb.Capabilities.Sample.Documents;
using MongoDb.Capabilities.Sample.Endpoints;
using MongoDb.Capabilities.Sample.Provisioning;
using MongoDB.Driver;
using Testcontainers.MongoDb;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var mongoDbContainer = new MongoDbBuilder("mongo:7.0")
    .WithReplicaSet("rs0")
    .Build();

await mongoDbContainer.StartAsync();
var connectionString = mongoDbContainer.GetConnectionString();

builder.Services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("CapabilitiesDB", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<Order>("orders", d => d
            .WithCollectionName("orders")
            .WithSoftDelete()
            .WithBulkRepository()
            .WithProjectionRepository()
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7)
            .WithIndexes(new CreateIndexModel<Order>(
                Builders<Order>.IndexKeys.Ascending(x => x.Sku),
                new CreateIndexOptions { Name = "orders_sku" }))
            .WithCollectionItemsTimeToLive(TimeSpan.FromDays(1), x => x.ExpiresAt));
        db.AddDocumentBinding<Payment>("payments", d => d
            .WithCollectionName("payments")
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
        db.AddDocumentBinding<Note>("notes", d => d
            .WithCollectionName("notes"));
    }));

// Extra provisioning steps are not declared on bindings; register them in DI after AddMongoDb.
builder.Services.AddSingleton<IProvisioningStep, SampleProvisioningStep>();

// JsonDocumentStore is not auto-registered. Construct it from the same factory + converter as typed docs.
builder.Services.AddScoped(sp => new JsonDocumentStore(
    sp.GetRequiredService<IMongoDbCollectionFactory>(),
    sp.GetRequiredService<IBsonJsonConverter>(),
    new MongoDatabaseKey("CapabilitiesDB")));

var app = builder.Build();

app.Lifetime.ApplicationStopping.Register(() => mongoDbContainer.DisposeAsync().AsTask().GetAwaiter().GetResult());

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
    var dryRun = await provisioner.DryRunAsync();
    if (dryRun.IsFailed)
    {
        throw new InvalidOperationException(string.Join("; ", dryRun.Errors.Select(e => e.Message)));
    }

    var applied = await provisioner.ApplyAsync();
    if (applied.IsFailed)
    {
        throw new InvalidOperationException(string.Join("; ", applied.Errors.Select(e => e.Message)));
    }
}

app.MapPolicyEndpoints();
app.MapPaginationEndpoints();
app.MapBulkEndpoints();
app.MapTransactionEndpoints();
app.MapJsonEndpoints();
app.MapEscapeHatchEndpoints();
app.MapProvisioningEndpoints();

app.Run();
