using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Abstractions.Transactions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

public partial class TransactionRunnerTests
{
    [Test]
    [CancelAfter(120_000)]
    public async Task Transaction_LargeSingleDocument_CommitsAndReadsBack()
    {
        // Arrange
        var (runner, provider) = CreateHost("large-one-db");
        var payload = new string('x', 10 * 1024 * 1024);
        var cluster = new MongoClusterKey("primary");

        // Act
        var result = await runner.WithTransactionAsync<Guid>(cluster, async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            var stored = await orders.StoreAsync(new Order { Name = "large", Payload = payload }, ct);
            stored.ShouldBeSuccess();
            return Result.Ok(stored.Value.Id);
        });
        Result<Order>? loaded = null;
        if (result.IsSuccess)
        {
            using var scope = provider.CreateScope();
            var orders = scope.ServiceProvider.GetRequiredKeyedService<IGenericRepository<Order>>("orders");
            var filter = Builders<Order>.Filter.Eq(x => x.Name, "large");
            loaded = await orders.GetAsync(filter);
        }

        // Assert
        result.ShouldBeSuccess();
        result.Value.ShouldNotBe(Guid.Empty);
        loaded!.ShouldBeSuccess();
        loaded!.Value.Payload!.Length.ShouldBe(payload.Length);
    }

    [Test]
    [CancelAfter(180_000)]
    public async Task Transaction_ManyDocumentsAcrossTwoCollections_CommitsAtomically()
    {
        // Arrange
        var (runner, provider) = CreateHost("many-db");
        const int perCollection = 250;
        var cluster = new MongoClusterKey("primary");

        // Act
        var result = await runner.ExecuteAsync(cluster, async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            var payments = tx.Repositories.GetRepository<Payment>("payments");
            for (var i = 0; i < perCollection; i++)
            {
                (await orders.StoreAsync(new Order { Name = "o" + i }, ct)).ShouldBeSuccess();
                (await payments.StoreAsync(new Payment { Name = "p" + i }, ct)).ShouldBeSuccess();
            }

            return Result.Ok(true);
        });
        long? orderCount = null;
        long? paymentCount = null;
        if (result.IsSuccess)
        {
            orderCount = await CountAsync<Order>(provider, "orders");
            paymentCount = await CountAsync<Payment>(provider, "payments");
        }

        // Assert
        result.ShouldBeSuccess();
        orderCount.ShouldBe(perCollection);
        paymentCount.ShouldBe(perCollection);
    }

    [Test]
    public async Task Transaction_FailureAfterWritesOnBothCollections_RollsBothBack()
    {
        // Arrange
        var (runner, provider) = CreateHost("rollback-both-db");
        var cluster = new MongoClusterKey("primary");

        // Act
        var result = await runner.ExecuteAsync(cluster, async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            var payments = tx.Repositories.GetRepository<Payment>("payments");
            (await orders.StoreAsync(new Order { Name = "o" }, ct)).ShouldBeSuccess();
            (await payments.StoreAsync(new Payment { Name = "p" }, ct)).ShouldBeSuccess();
            return Result.Fail<bool>("business rule");
        });
        long? orderCount = null;
        long? paymentCount = null;
        if (result.IsFailed)
        {
            orderCount = await CountAsync<Order>(provider, "orders");
            paymentCount = await CountAsync<Payment>(provider, "payments");
        }

        // Assert
        result.ShouldBeFailure();
        orderCount.ShouldBe(0);
        paymentCount.ShouldBe(0);
    }

    [Test]
    [CancelAfter(180_000)]
    public async Task Transaction_BulkStoreThenAbort_PersistsNothing()
    {
        // Arrange
        var (runner, provider) = CreateHost("bulk-abort-db");
        var cluster = new MongoClusterKey("primary");
        var options = new MongoTransactionOptions { MaxOperations = 2_000 };

        // Act
        var result = await runner.ExecuteAsync(
            cluster,
            async (tx, ct) =>
            {
                var bulk = tx.Repositories.GetBulkRepository<Order>("orders");
                var docs = Enumerable.Range(0, 1_000)
                    .Select(i => new Order { Name = "b" + i })
                    .ToArray();
                (await bulk.BulkStoreAsync(docs, ct)).ShouldBeSuccess();
                return Result.Fail<bool>("abort");
            },
            options);
        long? count = null;
        if (result.IsFailed)
        {
            count = await CountAsync<Order>(provider, "orders");
        }

        // Assert
        result.ShouldBeFailure();
        count.ShouldBe(0);
    }

    [Test]
    [CancelAfter(180_000)]
    public async Task Transaction_DocumentAboveBsonLimit_ReturnsDocumentTooLargeAndAborts()
    {
        // Arrange
        var (runner, provider) = CreateHost("too-large-db");
        var cluster = new MongoClusterKey("primary");
        var options = new MongoTransactionOptions { MaxEstimatedBytes = 32 * 1024 * 1024 };
        var payload = new string('x', (16 * 1024 * 1024) + 200_000);

        // Act
        var result = await runner.ExecuteAsync(
            cluster,
            async (tx, ct) =>
            {
                var orders = tx.Repositories.GetRepository<Order>("orders");
                var stored = await orders.StoreAsync(
                    new Order { Name = "huge", Payload = payload },
                    ct);
                stored.ShouldBeFailure();
                stored.ShouldHaveError<DocumentTooLargeError>();
                return stored.ToResult();
            },
            options);
        long? count = null;
        if (result.IsFailed)
        {
            count = await CountAsync<Order>(provider, "orders");
        }

        // Assert
        result.ShouldBeFailure();
        result.ShouldHaveError<DocumentTooLargeError>();
        count.ShouldBe(0);
    }

    [Test]
    [CancelAfter(180_000)]
    public async Task Transaction_ManyDocumentsOverSixteenMegabytes_CommitsWhenClientBudgetAllows()
    {
        // Arrange
        var (runner, provider) = CreateHost("over-sixteen-db");
        const int count = 18;
        var cluster = new MongoClusterKey("primary");
        var options = new MongoTransactionOptions { MaxEstimatedBytes = 32 * 1024 * 1024, MaxOperations = 100 };

        // Act
        var result = await runner.ExecuteAsync(
            cluster,
            async (tx, ct) =>
            {
                var orders = tx.Repositories.GetRepository<Order>("orders");
                for (var i = 0; i < count; i++)
                {
                    var order = new Order { Name = "m" + i, Payload = new string('x', 1024 * 1024) };
                    (await orders.StoreAsync(order, ct)).ShouldBeSuccess();
                }

                return Result.Ok(true);
            },
            options);
        long? storedCount = null;
        if (result.IsSuccess)
        {
            storedCount = await CountAsync<Order>(provider, "orders");
        }

        // Assert
        result.ShouldBeSuccess();
        storedCount.ShouldBe(count);
    }

    [Test]
    public async Task Transaction_ByteBudget_RejectsOversizedSingleWrite()
    {
        // Arrange
        var (runner, provider) = CreateHost("byte-cap-db");
        var cluster = new MongoClusterKey("primary");
        var options = new MongoTransactionOptions { MaxEstimatedBytes = 1024 * 1024 };
        var payload = new string('x', 2 * 1024 * 1024);

        // Act
        var result = await runner.ExecuteAsync(
            cluster,
            async (tx, ct) =>
            {
                var orders = tx.Repositories.GetRepository<Order>("orders");
                var stored = await orders.StoreAsync(new Order { Name = "big", Payload = payload }, ct);
                stored.ShouldBeFailure();
                stored.ShouldHaveError<TransactionBudgetExceededError>();
                return stored.ToResult();
            },
            options);
        long? count = null;
        if (result.IsFailed)
        {
            count = await CountAsync<Order>(provider, "orders");
        }

        // Assert
        result.ShouldBeFailure();
        result.ShouldHaveError<TransactionBudgetExceededError>();
        count.ShouldBe(0);
    }

    [Test]
    public async Task Transaction_ByteBudget_RejectsBulkWhoseModelsExceedTheCap()
    {
        // Arrange
        var (runner, provider) = CreateHost("bulk-bytes-db");
        var cluster = new MongoClusterKey("primary");
        var options = new MongoTransactionOptions { MaxEstimatedBytes = 500_000, MaxOperations = 20 };

        // Act
        var result = await runner.ExecuteAsync(
            cluster,
            async (tx, ct) =>
            {
                var bulk = tx.Repositories.GetBulkRepository<Order>("orders");
                var docs = Enumerable.Range(0, 6)
                    .Select(i => new Order { Name = "p" + i, Payload = new string('x', 200_000) })
                    .ToArray();
                var stored = await bulk.BulkStoreAsync(docs, ct);
                stored.ShouldBeFailure();
                stored.ShouldHaveError<TransactionBudgetExceededError>();
                return stored.ToResult();
            },
            options);
        long? count = null;
        if (result.IsFailed)
        {
            count = await CountAsync<Order>(provider, "orders");
        }

        // Assert
        result.ShouldBeFailure();
        result.ShouldHaveError<TransactionBudgetExceededError>();
        count.ShouldBe(0);
    }

    [Test]
    public async Task Transaction_OperationBudget_FailsMidBulkAndAborts()
    {
        // Arrange
        var (runner, provider) = CreateHost("bulk-ops-db");
        var cluster = new MongoClusterKey("primary");
        var options = new MongoTransactionOptions { MaxOperations = 3 };

        // Act
        var result = await runner.ExecuteAsync(
            cluster,
            async (tx, ct) =>
            {
                var bulk = tx.Repositories.GetBulkRepository<Order>("orders");
                var docs = Enumerable.Range(0, 10).Select(i => new Order { Name = "n" + i }).ToArray();
                var stored = await bulk.BulkStoreAsync(docs, ct);
                stored.ShouldBeFailure();
                stored.ShouldHaveError<TransactionBudgetExceededError>();
                return stored.ToResult();
            },
            options);
        long? count = null;
        if (result.IsFailed)
        {
            count = await CountAsync<Order>(provider, "orders");
        }

        // Assert
        result.ShouldBeFailure();
        result.ShouldHaveError<TransactionBudgetExceededError>();
        count.ShouldBe(0);
    }

    [Test]
    public async Task Transaction_CallbackException_AbortsAndPropagates()
    {
        // Arrange
        var (runner, provider) = CreateHost("throw-db");
        var cluster = new MongoClusterKey("primary");

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await runner.ExecuteAsync(cluster, async (tx, ct) =>
            {
                var orders = tx.Repositories.GetRepository<Order>("orders");
                (await orders.StoreAsync(new Order { Name = "thrown" }, ct)).ShouldBeSuccess();
                throw new InvalidOperationException("callback blew up");
            }));
        var count = await CountAsync<Order>(provider, "orders");

        // Assert
        exception.Message.ShouldBe("callback blew up");
        count.ShouldBe(0);
    }

    [Test]
    public async Task Transaction_Cancellation_AbortsWithoutCommit()
    {
        // Arrange
        var (runner, provider) = CreateHost("cancel-db");
        using var cts = new CancellationTokenSource();
        var cluster = new MongoClusterKey("primary");

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await runner.ExecuteAsync(
                cluster,
                async (tx, ct) =>
                {
                    var orders = tx.Repositories.GetRepository<Order>("orders");
                    (await orders.StoreAsync(new Order { Name = "cancel" }, ct)).ShouldBeSuccess();
                    await cts.CancelAsync();
                    await Task.Delay(Timeout.Infinite, ct);
                    return Result.Ok(true);
                },
                cancellationToken: cts.Token));
        var count = await CountAsync<Order>(provider, "orders");

        // Assert
        count.ShouldBe(0);
    }

    [Test]
    public async Task Transaction_UnknownCluster_ReturnsFailure()
    {
        // Arrange
        var (runner, _) = CreateHost("unknown-db");
        var cluster = new MongoClusterKey("missing");

        // Act
        var result = await runner.ExecuteAsync(cluster, (_, _) => Task.FromResult(Result.Ok(true)));

        // Assert
        result.ShouldBeFailure();
        result.Errors.ShouldContain(error => error.Message.Contains("Unknown cluster", StringComparison.Ordinal));
    }

    [Test]
    public async Task Transaction_ScopeRepository_DoesNotJoinTheSession()
    {
        // Arrange
        var (runner, provider) = CreateHost("bypass-db");
        var cluster = new MongoClusterKey("primary");

        // Act
        var result = await runner.ExecuteAsync(cluster, async (_, ct) =>
        {
            using var scope = provider.CreateScope();
            var orders = scope.ServiceProvider.GetRequiredKeyedService<IGenericRepository<Order>>("orders");
            (await orders.StoreAsync(new Order { Name = "outside" }, ct)).ShouldBeSuccess();
            return Result.Fail<bool>("abort");
        });
        long? count = null;
        if (result.IsFailed)
        {
            count = await CountAsync<Order>(provider, "orders");
        }

        // Assert
        result.ShouldBeFailure();
        count.ShouldBe(1);
    }

    [Test]
    public async Task Transaction_OtherCluster_ReturnsCrossClusterError()
    {
        // Arrange
        var (runner, provider) = CreateHost("cross-db", withSecondCluster: true);
        var cluster = new MongoClusterKey("primary");

        // Act
        var result = await runner.ExecuteAsync(cluster, async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            (await orders.StoreAsync(new Order { Name = "local" }, ct)).ShouldBeSuccess();
            var other = tx.Repositories.GetRepository<Order>("other-orders");
            var stored = await other.StoreAsync(new Order { Name = "remote" }, ct);
            return stored.ToResult();
        });
        long? count = null;
        if (result.IsFailed)
        {
            count = await CountAsync<Order>(provider, "orders");
        }

        // Assert
        result.ShouldBeFailure();
        result.ShouldHaveError<CrossClusterOperationError>();
        count.ShouldBe(0);
    }

    [Test]
    public async Task Transaction_DuplicateKey_AbortsTheWholeTransaction()
    {
        // Arrange
        var (runner, provider) = CreateHost("dup-db");
        await EnsureUniqueNameIndex(provider, "dup-db");
        var cluster = new MongoClusterKey("primary");

        // Act
        var result = await runner.ExecuteAsync(cluster, async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            (await orders.StoreAsync(new Order { Name = "same" }, ct)).ShouldBeSuccess();
            var duplicate = await orders.StoreAsync(new Order { Name = "same" }, ct);
            duplicate.ShouldBeFailure();
            duplicate.ShouldHaveError<DuplicateKeyError>();
            return duplicate.ToResult();
        });
        long? count = null;
        if (result.IsFailed)
        {
            count = await CountAsync<Order>(provider, "orders");
        }

        // Assert
        result.ShouldBeFailure();
        result.ShouldHaveError<DuplicateKeyError>();
        count.ShouldBe(0);
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task Transaction_WriteConflict_RetriesCallbackAndBothCommit()
    {
        // Arrange
        var (runner, provider) = CreateHost("conflict-db");
        var cluster = new MongoClusterKey("primary");
        var seeded = await runner.ExecuteAsync<Guid>(cluster, async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            var stored = await orders.StoreAsync(new Order { Name = "seed" }, ct);
            stored.ShouldBeSuccess();
            return Result.Ok(stored.Value.Id);
        });
        Guid id = Guid.Empty;
        if (seeded.IsSuccess)
        {
            using var scope = provider.CreateScope();
            var orders = scope.ServiceProvider.GetRequiredKeyedService<IGenericRepository<Order>>("orders");
            var filter = Builders<Order>.Filter.Eq(x => x.Name, "seed");
            id = (await orders.GetAsync(filter)).Value.Id;
        }

        var attempts = 0;
        var reads = 0;
        var bothRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<Result<bool>> UpdateName(string name, CancellationToken ct)
        {
            return await runner.ExecuteAsync(cluster, async (tx, token) =>
            {
                Interlocked.Increment(ref attempts);
                var orders = tx.Repositories.GetRepository<Order>("orders");
                var idFilter = Builders<Order>.Filter.Eq(x => x.Id, id);
                var current = await orders.GetAsync(idFilter, token);
                current.ShouldBeSuccess();
                if (Interlocked.Increment(ref reads) == 2)
                {
                    bothRead.TrySetResult();
                }

                // Both transactions hold the same snapshot before either writes.
                await bothRead.Task.WaitAsync(TimeSpan.FromSeconds(15), token);
                current.Value.Name = name;
                (await orders.ReplaceAsync(current.Value, token)).ShouldBeSuccess();
                return Result.Ok(true);
            }, cancellationToken: ct);
        }

        // Act
        Result<bool>[]? results = null;
        Result<Order>? final = null;
        long? count = null;
        if (seeded.IsSuccess)
        {
            var first = UpdateName("left", CancellationToken.None);
            var second = UpdateName("right", CancellationToken.None);
            results = await Task.WhenAll(first, second);
            using var verify = provider.CreateScope();
            var repo = verify.ServiceProvider.GetRequiredKeyedService<IGenericRepository<Order>>("orders");
            var idFilter = Builders<Order>.Filter.Eq(x => x.Id, id);
            final = await repo.GetAsync(idFilter);
            count = await CountAsync<Order>(provider, "orders");
        }

        // Assert
        seeded.ShouldBeSuccess();
        results!.ShouldAllBe(result => result.IsSuccess);
        attempts.ShouldBeGreaterThan(2);
        final!.ShouldBeSuccess();
        final!.Value.Name.ShouldBeOneOf("left", "right");
        count.ShouldBe(1);
    }

    [Test]
    public async Task Transaction_TimeLimitWithoutRepositoryCalls_FailsBeforeCommit()
    {
        // Arrange
        var (runner, provider) = CreateHost("time-db");
        var cluster = new MongoClusterKey("primary");
        var options = new MongoTransactionOptions { TimeLimit = TimeSpan.FromMilliseconds(30) };

        // Act
        var result = await runner.ExecuteAsync(
            cluster,
            async (_, ct) =>
            {
                await Task.Delay(200, ct);
                return Result.Ok(true);
            },
            options);
        long? count = null;
        if (result.IsFailed)
        {
            count = await CountAsync<Order>(provider, "orders");
        }

        // Assert
        result.ShouldBeFailure();
        result.ShouldHaveError<TransactionBudgetExceededError>();
        count.ShouldBe(0);
    }

    [Test]
    public async Task Transaction_NestedExecuteAsync_InnerCommitsWhenOuterAborts()
    {
        // Arrange
        var (runner, provider) = CreateHost("nested-db");
        var cluster = new MongoClusterKey("primary");

        // Act
        var result = await runner.ExecuteAsync(cluster, async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            (await orders.StoreAsync(new Order { Name = "outer" }, ct)).ShouldBeSuccess();

            var inner = await runner.ExecuteAsync(cluster, async (innerTx, innerCt) =>
            {
                var payments = innerTx.Repositories.GetRepository<Payment>("payments");
                (await payments.StoreAsync(new Payment { Name = "inner" }, innerCt)).ShouldBeSuccess();
                return Result.Ok(true);
            }, cancellationToken: ct);
            inner.ShouldBeSuccess();
            return Result.Fail<bool>("abort outer");
        });
        long? orderCount = null;
        long? paymentCount = null;
        if (result.IsFailed)
        {
            orderCount = await CountAsync<Order>(provider, "orders");
            paymentCount = await CountAsync<Payment>(provider, "payments");
        }

        // Assert
        result.ShouldBeFailure();
        orderCount.ShouldBe(0);
        paymentCount.ShouldBe(1);
    }

    [Test]
    [CancelAfter(180_000)]
    public async Task Transaction_StandaloneServer_ReturnsFailureInsteadOfThrowing()
    {
        // Arrange
        await using var standalone = MongoTestImages.CreateStandalone();
        await standalone.StartAsync();
        var connectionString = standalone.GetConnectionString();
        var (runner, _) = CreateHost("standalone-db", connectionString);
        var cluster = new MongoClusterKey("primary");

        // Act
        var result = await runner.ExecuteAsync(cluster, async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            var stored = await orders.StoreAsync(new Order { Name = "alone" }, ct);
            return stored.ToResult();
        });

        // Assert
        result.ShouldBeFailure();
    }

    private static async Task<long> CountAsync<TDocument>(ServiceProvider provider, string bindingKey)
        where TDocument : class, IDocumentEntity
    {
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredKeyedService<IGenericRepository<TDocument>>(bindingKey);
        var count = await repository.CountAsync(Builders<TDocument>.Filter.Empty);
        count.ShouldBeSuccess();
        return count.Value;
    }

    private static async Task EnsureUniqueNameIndex(ServiceProvider provider, string database)
    {
        var client = provider.GetRequiredKeyedService<IMongoClient>("primary");
        var collection = client.GetDatabase(database).GetCollection<Order>("orders");
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Order>(
            Builders<Order>.IndexKeys.Ascending(x => x.Name),
            new CreateIndexOptions<Order> { Unique = true, Name = "ux_name" }));
    }
}