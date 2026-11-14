using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/guides/soft-delete.md.</summary>
public class SoftDeleteExampleTests : ExampleIntegrationTests
{
    public sealed class Customer : IDocumentEntity<Guid>, ISoftDeletable
    {
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; }
        public string Name { get; set; } = "";
    }

    public sealed class CustomerService(IGenericRepository<Customer> customers)
    {
        public Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
            => customers.DeleteAsync(Builders<Customer>.Filter.Eq(x => x.Id, id), cancellationToken);

        public Task<Result<bool>> RestoreAsync(Guid id, CancellationToken cancellationToken)
            => customers.RestoreAsync(Builders<Customer>.Filter.Eq(x => x.Id, id), cancellationToken);

        public Task<Result<long>> PurgeAsync(Guid id, CancellationToken cancellationToken)
            => customers.PurgeAsync(Builders<Customer>.Filter.Eq(x => x.Id, id), cancellationToken);
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("crm", db =>
            {
                db.OnCluster("primary");

                db.AddDocumentBinding<Customer>("customers", d => d
                    .WithCollectionName("customers")
                    .WithSoftDelete()
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
            }));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task DeleteHidesRestoreBringsBackPurgeRemoves()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var customers = scope.ServiceProvider.GetRequiredService<IGenericRepository<Customer>>();
        var service = ActivatorUtilities.CreateInstance<CustomerService>(scope.ServiceProvider);
        var stored = await customers.StoreAsync(new Customer { Name = "a" });
        var id = stored.Value.Id;
        var ct = CancellationToken.None;

        // Act
        var deleted = await service.DeleteAsync(id, ct);
        var hidden = await customers.GetAsync(id);
        var deletedAgain = await service.DeleteAsync(id, ct);
        var restored = await service.RestoreAsync(id, ct);
        var visible = await customers.GetAsync(id);
        var purged = await service.PurgeAsync(id, ct);
        var gone = await customers.HasAnyAsync(Builders<Customer>.Filter.Empty);

        // Assert
        deleted.ShouldBeSuccess();
        hidden.HasError<DocumentNotFoundError>().ShouldBeTrue();
        deletedAgain.HasError<DocumentNotFoundError>().ShouldBeTrue();
        restored.ShouldBeSuccess();
        visible.ShouldBeSuccess();
        purged.Value.ShouldBe(1);
        gone.Value.ShouldBeFalse();
    }

    [Test]
    public void WithSoftDelete_OnNonSoftDeletableType_ThrowsAtRegistration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("crm", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<Plain>("plain", d => d.WithCollectionName("plain").WithSoftDelete());
            }));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    public sealed class Plain : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
    }
}