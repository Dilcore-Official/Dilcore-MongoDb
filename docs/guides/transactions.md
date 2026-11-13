# Transactions

**Current.** Thin coordination over driver sessions. MongoDB multi-document transactions require a **replica set** (or mongos). Standalone `mongo` cannot run them.

Sample: [samples/MongoDb.Capabilities.Sample](../../samples/MongoDb.Capabilities.Sample) (`POST /transactions/place-order`). Runnable coverage: `test/Repositories.IntegrationTests/TransactionRunnerTests.cs`. Driver session escape hatch: [driver-escape-hatches.md](../product/driver-escape-hatches.md).

## API

`IMongoDbTransactionRunner` is registered by `AddMongoDb` (scoped). `WithTransactionAsync` is a default-interface alias for `ExecuteAsync`.

```csharp
var result = await runner.WithTransactionAsync(
    new MongoClusterKey("primary"),
    async (tx, ct) =>
    {
        var orders = tx.Repositories.GetRepository<Order>("orders");
        var payments = tx.Repositories.GetRepository<Payment>("payments");
        var stored = await orders.StoreAsync(order, ct);
        if (stored.IsFailed)
            return stored.ToResult();

        var payment = await payments.StoreAsync(new Payment { OrderId = stored.Value.Id }, ct);
        if (payment.IsFailed)
            return payment.ToResult();

        return Result.Ok(stored.Value.Id);
    },
    new MongoTransactionOptions
    {
        MaxOperations = 1_000,
        MaxEstimatedBytes = 16 * 1024 * 1024,
        TimeLimit = TimeSpan.FromSeconds(60)
    });
```

`IMongoDbTransactionContext`:

- `ClusterKey` — cluster this transaction is bound to
- `Session` — driver `IClientSessionHandle` for APIs Dilcore does not wrap
- `Repositories` — `IRepositoryResolver` (same binding keys as DI)

Resolve repositories **from the context**, not from the request scope, so writes join the session.

## Rules

- Do not start a second session inside the callback. A nested `ExecuteAsync` opens its own session and can commit even when the outer callback returns a failed `Result`.
- Resolve repositories from `IMongoDbTransactionContext.Repositories`. A repository taken from the request scope does not join the session, so those writes survive an abort.
- Work that targets another cluster is rejected as `CrossClusterOperationError` before dispatch.
- Returning a failed `Result` aborts the transaction. An exception other than a retryable transaction error aborts and propagates to the caller.
- Callbacks must stay sequential; do not issue parallel operations on the same session.
- A standalone `mongod` cannot start a multi-document transaction. `ExecuteAsync` returns a failed `Result` instead of throwing.

## Budgets

`MongoTransactionOptions` are **client-side estimates**:

| Option | Default | Meaning |
|--------|---------|---------|
| `MaxOperations` | 1_000 | Dilcore-tracked operations. A document write counts as one. A bulk write counts one per model. Reads count as one and reserve 256 bytes. |
| `MaxEstimatedBytes` | 16 MiB | Sum of rendered BSON sizes for writes. **Not** a MongoDB “16 MiB total transaction” server cap. An estimate equal to the cap is allowed; the next byte is not. Negative estimates fail and do not consume an operation. |
| `TimeLimit` | 60s | Elapsed wall clock from when the runner starts, including driver retries. Checked on each reservation and again when the callback returns, so a callback that never touches a repository can still exceed it. |
| `DriverOptions` | null | Passed through to the driver `TransactionOptions` |

Exceeding a budget returns `TransactionBudgetExceededError` and aborts. `ResetAttempt` clears the operation and byte counters for a driver retry; it does not extend `TimeLimit`.

MongoDB still enforces its own limits. One document above 16 MiB is `DocumentTooLargeError` when `MaxEstimatedBytes` is high enough to let the write through; otherwise the client budget fails first. Many documents whose total size is above 16 MiB commit on current MongoDB when the client budget allows them. There is no server cap on the sum of a transaction.

## Retries

The driver may run the callback more than once when it sees `TransientTransactionError` (including a write conflict). Session-bound repository calls rethrow those errors so the retry happens. Other MongoDB failures stay as `Result` values. Policy fields staged for a write are restored before a retryable error leaves the repository, but the callback itself must be safe to run again. See [production-mongodb.md](../security/production-mongodb.md).
