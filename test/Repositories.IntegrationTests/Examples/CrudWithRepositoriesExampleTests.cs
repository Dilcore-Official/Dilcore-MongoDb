using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/guides/crud-with-repositories.md.</summary>
public class CrudWithRepositoriesExampleTests : ExampleIntegrationTests
{
    public sealed class Product : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = "";
        public int Stock { get; set; }
    }

    public sealed class ProductService(IGenericRepository<Product> products)
    {
        public Task<Result<Product>> SaveAsync(Product product, CancellationToken cancellationToken = default)
            => products.StoreAsync(product, cancellationToken);

        public Task<Result<Product>> FindAsync(Guid id, CancellationToken cancellationToken = default)
            => products.GetAsync(id, cancellationToken);

        public Task<Result<IReadOnlyList<Product>>> GetLowStockAsync(CancellationToken cancellationToken = default)
            => products.GetListAsync(x => x.Stock < 5, cancellationToken);

        public Task<Result<Product>> ReplaceAsync(Product changed, CancellationToken cancellationToken = default)
            => products.ReplaceAsync(changed, cancellationToken);

        public Task<Result<Product>> RestockAsync(Guid id, int amount, CancellationToken cancellationToken = default)
        {
            var filter = Builders<Product>.Filter.Eq(x => x.Id, id);
            var update = Builders<Product>.Update.Inc(x => x.Stock, amount);

            return products.PatchAsync(filter, update, cancellationToken);
        }
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("shop", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<Product>("products", d => d
                    .WithCollectionName("products")
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
            }));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task StoreFindReplacePatchAndList_WorkEndToEnd()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = ActivatorUtilities.CreateInstance<ProductService>(scope.ServiceProvider);
        var ct = CancellationToken.None;

        // Act
        var stored = await service.SaveAsync(new Product { Sku = "a", Stock = 1 }, ct);
        var found = await service.FindAsync(stored.Value.Id, ct);
        var replaced = await service.ReplaceAsync(new Product { Id = stored.Value.Id, Sku = "b", Stock = 2 }, ct);
        var patched = await service.RestockAsync(stored.Value.Id, 10, ct);
        var low = await service.GetLowStockAsync(ct);
        var afterPatch = await service.FindAsync(stored.Value.Id, ct);

        // Assert
        stored.ShouldBeSuccess();
        found.Value.Sku.ShouldBe("a");
        replaced.ShouldBeSuccess();
        patched.ShouldBeSuccess();
        low.Value.ShouldBeEmpty();
        afterPatch.Value.Sku.ShouldBe("b");
        afterPatch.Value.Stock.ShouldBe(12);
    }

    [Test]
    public async Task MissingDocument_ReturnsDocumentNotFoundError()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = ActivatorUtilities.CreateInstance<ProductService>(scope.ServiceProvider);

        // Act
        var found = await service.FindAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        found.HasError<DocumentNotFoundError>().ShouldBeTrue();
    }

    [Test]
    public async Task Replace_OfMissingDocument_ReturnsDocumentNotFoundError()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = ActivatorUtilities.CreateInstance<ProductService>(scope.ServiceProvider);

        // Act
        var replaced = await service.ReplaceAsync(new Product { Id = Guid.NewGuid() }, CancellationToken.None);

        // Assert
        replaced.HasError<DocumentNotFoundError>().ShouldBeTrue();
    }
}