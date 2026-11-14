using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using FluentResults;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/guides/optimistic-concurrency.md.</summary>
public class OptimisticConcurrencyExampleTests : ExampleIntegrationTests
{
    public sealed class Invoice : IDocumentEntity<Guid>, IHasConcurrencyToken
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public decimal Total { get; set; }
    }

    public sealed class InvoiceService(IGenericRepository<Invoice> invoices)
    {
        public async Task<Result<Invoice>> ReviseAsync(Guid id, decimal total, CancellationToken cancellationToken)
        {
            var current = await invoices.GetAsync(id, cancellationToken);
            if (current.IsFailed)
                return current;

            current.Value.Total = total;

            return await invoices.ReplaceAsync(current.Value, cancellationToken);
        }

        public Task<Result<bool>> DeleteAsync(Invoice seen, CancellationToken cancellationToken)
            => invoices.DeleteAsync(seen.Id, seen.ETag, cancellationToken);
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("billing", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<Invoice>("invoices", d => d
                    .WithCollectionName("invoices")
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
            }));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task Revise_WithCurrentEtag_Succeeds()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var invoices = scope.ServiceProvider.GetRequiredService<IGenericRepository<Invoice>>();
        var service = ActivatorUtilities.CreateInstance<InvoiceService>(scope.ServiceProvider);
        var stored = await invoices.StoreAsync(new Invoice { Total = 1m });

        // Act
        var revised = await service.ReviseAsync(stored.Value.Id, 5m, CancellationToken.None);

        // Assert
        revised.ShouldBeSuccess();
        revised.Value.ETag.ShouldNotBe(stored.Value.ETag);
        revised.Value.ETag.ShouldNotBe(0);
    }

    [Test]
    public async Task Replace_WithStaleEtag_ReturnsConcurrencyConflictError()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var invoices = scope.ServiceProvider.GetRequiredService<IGenericRepository<Invoice>>();
        var service = ActivatorUtilities.CreateInstance<InvoiceService>(scope.ServiceProvider);
        var stored = await invoices.StoreAsync(new Invoice { Total = 1m });
        var stale = new Invoice { Id = stored.Value.Id, ETag = stored.Value.ETag, Total = 2m };
        (await service.ReviseAsync(stored.Value.Id, 3m, CancellationToken.None)).ShouldBeSuccess();

        // Act
        var result = await invoices.ReplaceAsync(stale);

        // Assert
        result.HasError<ConcurrencyConflictError>().ShouldBeTrue();
    }

    [Test]
    public async Task Delete_WithStaleEtag_ReturnsConcurrencyConflictError()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var invoices = scope.ServiceProvider.GetRequiredService<IGenericRepository<Invoice>>();
        var service = ActivatorUtilities.CreateInstance<InvoiceService>(scope.ServiceProvider);
        var stored = await invoices.StoreAsync(new Invoice { Total = 1m });
        var seen = new Invoice { Id = stored.Value.Id, ETag = stored.Value.ETag };
        (await service.ReviseAsync(stored.Value.Id, 3m, CancellationToken.None)).ShouldBeSuccess();

        // Act
        var result = await service.DeleteAsync(seen, CancellationToken.None);

        // Assert
        result.HasError<ConcurrencyConflictError>().ShouldBeTrue();
    }

    [Test]
    public async Task Delete_WithCurrentEtag_Succeeds()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var invoices = scope.ServiceProvider.GetRequiredService<IGenericRepository<Invoice>>();
        var service = ActivatorUtilities.CreateInstance<InvoiceService>(scope.ServiceProvider);
        var stored = await invoices.StoreAsync(new Invoice { Total = 1m });

        // Act
        var result = await service.DeleteAsync(stored.Value, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
    }
}