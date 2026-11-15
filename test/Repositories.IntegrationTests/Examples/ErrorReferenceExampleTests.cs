using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/guides/error-reference.md.</summary>
public class ErrorReferenceExampleTests : ExampleIntegrationTests
{
    public sealed class Invoice : IDocumentEntity<Guid>, IHasConcurrencyToken
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public string Memo { get; set; } = "";
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
    public async Task MissingDocument_ReturnsDocumentNotFound()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var invoices = scope.ServiceProvider.GetRequiredService<IGenericRepository<Invoice>>();

        // Act
        var result = await invoices.GetAsync(Builders<Invoice>.Filter.Eq(x => x.Id, Guid.NewGuid()));

        // Assert
        result.Errors.OfType<MongoOperationError>().Single().Code.ShouldBe("document_not_found");
    }

    [Test]
    public async Task StaleEtag_ReturnsConcurrencyConflict()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var invoices = scope.ServiceProvider.GetRequiredService<IGenericRepository<Invoice>>();
        var stored = await invoices.StoreAsync(new Invoice { Memo = "a" });
        var stale = new Invoice { Id = stored.Value.Id, ETag = stored.Value.ETag, Memo = "stale" };
        await invoices.ReplaceAsync(stored.Value);

        // Act
        var result = await invoices.ReplaceAsync(stale);

        // Assert
        result.Errors.OfType<MongoOperationError>().Single().Code.ShouldBe("concurrency_conflict");
    }

    [Test]
    public async Task SameId_ReturnsDuplicateKey()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var invoices = scope.ServiceProvider.GetRequiredService<IGenericRepository<Invoice>>();
        var stored = await invoices.StoreAsync(new Invoice { Memo = "a" });

        // Act
        var result = await invoices.StoreAsync(new Invoice { Id = stored.Value.Id, Memo = "b" });

        // Assert
        result.Errors.OfType<MongoOperationError>().Single().Code.ShouldBe("duplicate_key");
    }

    [Test]
    public async Task OversizedDocument_ReturnsDocumentTooLarge()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var invoices = scope.ServiceProvider.GetRequiredService<IGenericRepository<Invoice>>();

        // Act
        var result = await invoices.StoreAsync(new Invoice { Memo = new string('x', 17 * 1024 * 1024) });

        // Assert
        result.Errors.OfType<MongoOperationError>().Single().Code.ShouldBe("document_too_large");
    }

    [TestCase("document_not_found")]
    [TestCase("concurrency_conflict")]
    [TestCase("duplicate_key")]
    [TestCase("transient_write")]
    [TestCase("write_concern_failure")]
    [TestCase("document_too_large")]
    [TestCase("json_conversion")]
    [TestCase("cross_cluster_operation")]
    [TestCase("transaction_budget_exceeded")]
    [TestCase("bulk_write_partial_failure")]
    public void EveryDocumentedCode_HasAnAdviceEntry(string code)
    {
        // Arrange
        IError error = new FakeError(code);

        // Act
        var advice = ErrorAdvice.GetAdvice(error);

        // Assert
        advice.ShouldNotStartWith("unknown code");
    }

    [Test]
    public void ErrorCodes_MatchTheDocumentedTable()
    {
        // Arrange
        string[] documented =
        [
            "document_not_found", "concurrency_conflict", "duplicate_key", "transient_write",
            "write_concern_failure", "document_too_large", "json_conversion", "cross_cluster_operation",
            "transaction_budget_exceeded", "bulk_write_partial_failure"
        ];

        // Act
        var actual = new[]
        {
            DocumentNotFoundError.ErrorCode, ConcurrencyConflictError.ErrorCode, DuplicateKeyError.ErrorCode,
            TransientWriteError.ErrorCode, WriteConcernFailureError.ErrorCode, DocumentTooLargeError.ErrorCode,
            JsonConversionError.ErrorCode, CrossClusterOperationError.ErrorCode,
            TransactionBudgetExceededError.ErrorCode, BulkWritePartialFailureError.ErrorCode
        };

        // Assert
        actual.ShouldBe(documented);
    }

    private sealed class FakeError(string code) : MongoOperationError("fake")
    {
        public override string Code { get; } = code;
    }

    public static class ErrorAdvice
    {
        public static string GetAdvice(IError error)
        {
            if (error is not MongoOperationError typed)
            {
                return "unexpected: log it and return a generic failure";
            }

            return typed.Code switch
            {
                DocumentNotFoundError.ErrorCode => "not found: do not retry",
                ConcurrencyConflictError.ErrorCode => "stale copy: reload, reapply, retry once",
                DuplicateKeyError.ErrorCode => "already exists: do not retry",
                TransientWriteError.ErrorCode => "outcome unknown: reconcile, or retry an idempotent write",
                WriteConcernFailureError.ErrorCode => "unconfirmed: the write may have been applied",
                DocumentTooLargeError.ErrorCode => "too big: shrink the document, do not retry",
                JsonConversionError.ErrorCode => "bad JSON: reject the input",
                CrossClusterOperationError.ErrorCode => "programming error: one transaction, one cluster",
                TransactionBudgetExceededError.ErrorCode => "too much work in one transaction: split it",
                BulkWritePartialFailureError.ErrorCode => "some items failed: inspect Items",
                _ => "unknown code: treat as unexpected"
            };
        }
    }
}