using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/guides/handle-errors.md.</summary>
public class HandleErrorsExampleTests : ExampleIntegrationTests
{
    public sealed class Order : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = "";
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("shop", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<Order>("orders", d => d
                    .WithCollectionName("orders")
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
            }));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task StatusMapping_CoversSuccessAndNotFound()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredService<IGenericRepository<Order>>();

        // Act
        var stored = await orders.StoreAsync(new Order { Sku = "a" });
        var missing = await orders.GetAsync(Builders<Order>.Filter.Eq(x => x.Id, Guid.NewGuid()));

        // Assert
        stored.ToStatusCode().ShouldBe(200);
        missing.ToStatusCode().ShouldBe(404);
        missing.Errors.OfType<MongoOperationError>().Single().Code.ShouldBe("document_not_found");
    }

    [Test]
    public void StatusMapping_CoversConflictTransientAndUnexpected()
    {
        // Arrange
        var conflict = Result.Fail<Order>(new ConcurrencyConflictError("stale"));
        var duplicate = Result.Fail<Order>(new DuplicateKeyError("dup"));
        var transient = Result.Fail<Order>(new TransientWriteError("retry"));
        var other = Result.Fail<Order>("boom");

        // Act
        var codes = new[] { conflict, duplicate, transient, other }.Select(r => r.ToStatusCode()).ToArray();

        // Assert
        codes.ShouldBe([409, 409, 503, 500]);
    }
}

public static class ResultStatus
{
    public static int ToStatusCode<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return 200;

        if (result.HasError<DocumentNotFoundError>())
            return 404;

        if (result.HasError<ConcurrencyConflictError>() || result.HasError<DuplicateKeyError>())
            return 409;

        if (result.HasError<TransientWriteError>())
            return 503;

        return 500;
    }
}