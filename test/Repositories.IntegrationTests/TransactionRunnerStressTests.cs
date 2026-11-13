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
        var (runner, provider) = CreateHost("large-one-db");
        var payload = new string('x', 10 * 1024 * 1024);

        var result = await runner.WithTransactionAsync<Guid>(new MongoClusterKey("primary"), async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            var stored = await orders.StoreAsync(new Order { Name = "large", Payload = payload }, ct);
            stored.ShouldBeSuccess();
            return Result.Ok(stored.Value.Id);
        });

        result.ShouldBeSuccess();
        result.Value.ShouldNotBe(Guid.Empty);

        using var scope = provider.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredKeyedService<IGenericRepository<Order>>("orders");
        var loaded = await orders.GetAsync(Builders<Order>.Filter.Eq(x => x.Name, "large"));
        loaded.ShouldBeSuccess();
        loaded.Value.Payload!.Length.ShouldBe(payload.Length);
    }

    [Test]
    [CancelAfter(180_000)]
    public async Task Transaction_ManyDocumentsAcrossTwoCollections_CommitsAtomically()
    {
        var (runner, provider) = CreateHost("many-db");
        const int perCollection = 250;

        var result = await runner.ExecuteAsync(new MongoClusterKey("primary"), async (tx, ct) =>
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

        result.ShouldBeSuccess();
        (await CountAsync<Order>(provider, "orders")).ShouldBe(perCollection);
        (await CountAsync<Payment>(provider, "payments")).ShouldBe(perCollection);
    }

    [Test]
    public async Task Transaction_FailureAfterWritesOnBothCollections_RollsBothBack()
    {
        var (runner, provider) = CreateHost("rollback-both-db");
        var result = await runner.ExecuteAsync(new MongoClusterKey("primary"), async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            var payments = tx.Repositories.GetRepository<Payment>("payments");
            (await orders.StoreAsync(new Order { Name = "o" }, ct)).ShouldBeSuccess();
            (await payments.StoreAsync(new Payment { Name = "p" }, ct)).ShouldBeSuccess();
            return Result.Fail<bool>("business rule");
        });

        result.ShouldBeFailure();
        (await CountAsync<Order>(provider, "orders")).ShouldBe(0);
        (await CountAsync<Payment>(provider, "payments")).ShouldBe(0);
    }

    [Test]
    [CancelAfter(180_000)]
    public async Task Transaction_BulkStoreThenAbort_PersistsNothing()
    {
        var (runner, provider) = CreateHost("bulk-abort-db");
        var result = await runner.ExecuteAsync(
            new MongoClusterKey("primary"),
            async (tx, ct) =>
            {
                var bulk = tx.Repositories.GetBulkRepository<Order>("orders");
                var docs = Enumerable.Range(0, 1_000)
                    .Select(i => new Order { Name = "b" + i })
                    .ToArray();
                (await bulk.BulkStoreAsync(docs, ct)).ShouldBeSuccess();
                return Result.Fail<bool>("abort");
            },
            new MongoTransactionOptions { MaxOperations = 2_000 });

        result.ShouldBeFailure();
        (await CountAsync<Order>(provider, "orders")).ShouldBe(0);
    }

    [Test]
    [CancelAfter(180_000)]
    public async Task Transaction_DocumentAboveBsonLimit_ReturnsDocumentTooLargeAndAborts()
    {
        var (runner, provider) = CreateHost("too-large-db");
        var result = await runner.ExecuteAsync(
            new MongoClusterKey("primary"),
            async (tx, ct) =>
            {
                var orders = tx.Repositories.GetRepository<Order>("orders");
                var stored = await orders.StoreAsync(
                    new Order { Name = "huge", Payload = new string('x', (16 * 1024 * 1024) + 200_000) },
                    ct);
                stored.ShouldBeFailure();
                stored.ShouldHaveError<DocumentTooLargeError>();
                return stored.ToResult();
            },
            new MongoTransactionOptions { MaxEstimatedBytes = 32 * 1024 * 1024 });

        result.ShouldBeFailure();
        result.ShouldHaveError<DocumentTooLargeError>();
        (await CountAsync<Order>(provider, "orders")).ShouldBe(0);
    }

    [Test]
    [CancelAfter(180_000)]
    public async Task Transaction_ManyDocumentsOverSixteenMegabytes_CommitsWhenClientBudgetAllows()
    {
        var (runner, provider) = CreateHost("over-sixteen-db");
        const int count = 18;
        var result = await runner.ExecuteAsync(
            new MongoClusterKey("primary"),
            async (tx, ct) =>
            {
                var orders = tx.Repositories.GetRepository<Order>("orders");
                for (var i = 0; i < count; i++)
                {
                    (await orders.StoreAsync(
                        new Order { Name = "m" + i, Payload = new string('x', 1024 * 1024) },
                        ct)).ShouldBeSuccess();
                }

                return Result.Ok(true);
            },
            new MongoTransactionOptions { MaxEstimatedBytes = 32 * 1024 * 1024, MaxOperations = 100 });

        result.ShouldBeSuccess();
        (await CountAsync<Order>(provider, "orders")).ShouldBe(count);
    }

    [Test]
    public async Task Transaction_ByteBudget_RejectsOversizedSingleWrite()
    {
        var (runner, provider) = CreateHost("byte-cap-db");
        var result = await runner.ExecuteAsync(
            new MongoClusterKey("primary"),
            async (tx, ct) =>
            {
                var orders = tx.Repositories.GetRepository<Order>("orders");
                var stored = await orders.StoreAsync(
                    new Order { Name = "big", Payload = new string('x', 2 * 1024 * 1024) },
                    ct);
                stored.ShouldBeFailure();
                stored.ShouldHaveError<TransactionBudgetExceededError>();
                return stored.ToResult();
            },
            new MongoTransactionOptions { MaxEstimatedBytes = 1024 * 1024 });

        result.ShouldBeFailure();
        result.ShouldHaveError<TransactionBudgetExceededError>();
        (await CountAsync<Order>(provider, "orders")).ShouldBe(0);
    }

    [Test]
    public async Task Transaction_ByteBudget_RejectsBulkWhoseModelsExceedTheCap()
    {
        var (runner, provider) = CreateHost("bulk-bytes-db");
        var result = await runner.ExecuteAsync(
            new MongoClusterKey("primary"),
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
            new MongoTransactionOptions { MaxEstimatedBytes = 500_000, MaxOperations = 20 });

        result.ShouldBeFailure();
        result.ShouldHaveError<TransactionBudgetExceededError>();
        (await CountAsync<Order>(provider, "orders")).ShouldBe(0);
    }

    [Test]
    public async Task Transaction_OperationBudget_FailsMidBulkAndAborts()
    {
        var (runner, provider) = CreateHost("bulk-ops-db");
        var result = await runner.ExecuteAsync(
            new MongoClusterKey("primary"),
            async (tx, ct) =>
            {
                var bulk = tx.Repositories.GetBulkRepository<Order>("orders");
                var docs = Enumerable.Range(0, 10).Select(i => new Order { Name = "n" + i }).ToArray();
                var stored = await bulk.BulkStoreAsync(docs, ct);
                stored.ShouldBeFailure();
                stored.ShouldHaveError<TransactionBudgetExceededError>();
                return stored.ToResult();
            },
            new MongoTransactionOptions { MaxOperations = 3 });

        result.ShouldBeFailure();
        result.ShouldHaveError<TransactionBudgetExceededError>();
        (await CountAsync<Order>(provider, "orders")).ShouldBe(0);
    }

    [Test]
    public async Task Transaction_CallbackException_AbortsAndPropagates()
    {
        var (runner, provider) = CreateHost("throw-db");
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await runner.ExecuteAsync(new MongoClusterKey("primary"), async (tx, ct) =>
            {
                var orders = tx.Repositories.GetRepository<Order>("orders");
                (await orders.StoreAsync(new Order { Name = "thrown" }, ct)).ShouldBeSuccess();
                throw new InvalidOperationException("callback blew up");
            }));

        exception.Message.ShouldBe("callback blew up");
        (await CountAsync<Order>(provider, "orders")).ShouldBe(0);
    }

    [Test]
    public async Task Transaction_Cancellation_AbortsWithoutCommit()
    {
        var (runner, provider) = CreateHost("cancel-db");
        using var cts = new CancellationTokenSource();
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await runner.ExecuteAsync(
                new MongoClusterKey("primary"),
                async (tx, ct) =>
                {
                    var orders = tx.Repositories.GetRepository<Order>("orders");
                    (await orders.StoreAsync(new Order { Name = "cancel" }, ct)).ShouldBeSuccess();
                    await cts.CancelAsync();
                    await Task.Delay(Timeout.Infinite, ct);
                    return Result.Ok(true);
                },
                cancellationToken: cts.Token));

        (await CountAsync<Order>(provider, "orders")).ShouldBe(0);
    }

    [Test]
    public async Task Transaction_UnknownCluster_ReturnsFailure()
    {
        var (runner, _) = CreateHost("unknown-db");
        var result = await runner.ExecuteAsync(
            new MongoClusterKey("missing"),
            (_, _) => Task.FromResult(Result.Ok(true)));

        result.ShouldBeFailure();
        result.Errors.ShouldContain(error => error.Message.Contains("Unknown cluster", StringComparison.Ordinal));
    }

    [Test]
    public async Task Transaction_ScopeRepository_DoesNotJoinTheSession()
    {
        var (runner, provider) = CreateHost("bypass-db");
        var result = await runner.ExecuteAsync(new MongoClusterKey("primary"), async (_, ct) =>
        {
            using var scope = provider.CreateScope();
            var orders = scope.ServiceProvider.GetRequiredKeyedService<IGenericRepository<Order>>("orders");
            (await orders.StoreAsync(new Order { Name = "outside" }, ct)).ShouldBeSuccess();
            return Result.Fail<bool>("abort");
        });

        result.ShouldBeFailure();
        (await CountAsync<Order>(provider, "orders")).ShouldBe(1);
    }

    [Test]
    public async Task Transaction_OtherCluster_ReturnsCrossClusterError()
    {
        var (runner, provider) = CreateHost("cross-db", withSecondCluster: true);
        var result = await runner.ExecuteAsync(new MongoClusterKey("primary"), async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            (await orders.StoreAsync(new Order { Name = "local" }, ct)).ShouldBeSuccess();
            var other = tx.Repositories.GetRepository<Order>("other-orders");
            var stored = await other.StoreAsync(new Order { Name = "remote" }, ct);
            return stored.ToResult();
        });

        result.ShouldBeFailure();
        result.ShouldHaveError<CrossClusterOperationError>();
        (await CountAsync<Order>(provider, "orders")).ShouldBe(0);
    }

    [Test]
    public async Task Transaction_DuplicateKey_AbortsTheWholeTransaction()
    {
        var (runner, provider) = CreateHost("dup-db");
        await EnsureUniqueNameIndex(provider, "dup-db");

        var result = await runner.ExecuteAsync(new MongoClusterKey("primary"), async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            (await orders.StoreAsync(new Order { Name = "same" }, ct)).ShouldBeSuccess();
            var duplicate = await orders.StoreAsync(new Order { Name = "same" }, ct);
            duplicate.ShouldBeFailure();
            duplicate.ShouldHaveError<DuplicateKeyError>();
            return duplicate.ToResult();
        });

        result.ShouldBeFailure();
        result.ShouldHaveError<DuplicateKeyError>();
        (await CountAsync<Order>(provider, "orders")).ShouldBe(0);
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task Transaction_WriteConflict_RetriesCallbackAndBothCommit()
    {
        var (runner, provider) = CreateHost("conflict-db");
        Guid id = Guid.Empty;
        (await runner.ExecuteAsync<Guid>(new MongoClusterKey("primary"), async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            var stored = await orders.StoreAsync(new Order { Name = "seed" }, ct);
            stored.ShouldBeSuccess();
            return Result.Ok(stored.Value.Id);
        })).ShouldBeSuccess();

        using (var scope = provider.CreateScope())
        {
            var orders = scope.ServiceProvider.GetRequiredKeyedService<IGenericRepository<Order>>("orders");
            id = (await orders.GetAsync(Builders<Order>.Filter.Eq(x => x.Name, "seed"))).Value.Id;
        }

        var attempts = 0;
        var reads = 0;
        var bothRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<Result<bool>> UpdateName(string name, CancellationToken ct)
        {
            return await runner.ExecuteAsync(new MongoClusterKey("primary"), async (tx, token) =>
            {
                Interlocked.Increment(ref attempts);
                var orders = tx.Repositories.GetRepository<Order>("orders");
                var current = await orders.GetAsync(Builders<Order>.Filter.Eq(x => x.Id, id), token);
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

        var first = UpdateName("left", CancellationToken.None);
        var second = UpdateName("right", CancellationToken.None);
        var results = await Task.WhenAll(first, second);

        results.ShouldAllBe(result => result.IsSuccess);
        attempts.ShouldBeGreaterThan(2);

        using var verify = provider.CreateScope();
        var repo = verify.ServiceProvider.GetRequiredKeyedService<IGenericRepository<Order>>("orders");
        var final = await repo.GetAsync(Builders<Order>.Filter.Eq(x => x.Id, id));
        final.ShouldBeSuccess();
        final.Value.Name.ShouldBeOneOf("left", "right");
        (await CountAsync<Order>(provider, "orders")).ShouldBe(1);
    }

    [Test]
    public async Task Transaction_TimeLimitWithoutRepositoryCalls_FailsBeforeCommit()
    {
        var (runner, provider) = CreateHost("time-db");
        var result = await runner.ExecuteAsync(
            new MongoClusterKey("primary"),
            async (_, ct) =>
            {
                await Task.Delay(200, ct);
                return Result.Ok(true);
            },
            new MongoTransactionOptions { TimeLimit = TimeSpan.FromMilliseconds(30) });

        result.ShouldBeFailure();
        result.ShouldHaveError<TransactionBudgetExceededError>();
        (await CountAsync<Order>(provider, "orders")).ShouldBe(0);
    }

    [Test]
    public async Task Transaction_NestedExecuteAsync_InnerCommitsWhenOuterAborts()
    {
        var (runner, provider) = CreateHost("nested-db");
        var result = await runner.ExecuteAsync(new MongoClusterKey("primary"), async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            (await orders.StoreAsync(new Order { Name = "outer" }, ct)).ShouldBeSuccess();

            var inner = await runner.ExecuteAsync(new MongoClusterKey("primary"), async (innerTx, innerCt) =>
            {
                var payments = innerTx.Repositories.GetRepository<Payment>("payments");
                (await payments.StoreAsync(new Payment { Name = "inner" }, innerCt)).ShouldBeSuccess();
                return Result.Ok(true);
            }, cancellationToken: ct);
            inner.ShouldBeSuccess();
            return Result.Fail<bool>("abort outer");
        });

        result.ShouldBeFailure();
        (await CountAsync<Order>(provider, "orders")).ShouldBe(0);
        (await CountAsync<Payment>(provider, "payments")).ShouldBe(1);
    }

    [Test]
    [CancelAfter(180_000)]
    public async Task Transaction_StandaloneServer_ReturnsFailureInsteadOfThrowing()
    {
        await using var standalone = MongoTestImages.CreateStandalone();
        await standalone.StartAsync();
        var (runner, _) = CreateHost("standalone-db", standalone.GetConnectionString());

        var result = await runner.ExecuteAsync(new MongoClusterKey("primary"), async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            return (await orders.StoreAsync(new Order { Name = "alone" }, ct)).ToResult();
        });

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