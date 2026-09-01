# Transactions

**Current.** Thin coordination over driver sessions. MongoDB multi-document transactions require a **replica set** (or mongos). Standalone `mongo` cannot run them.

Sample: [samples/MongoDb.Capabilities.Sample](../../samples/MongoDb.Capabilities.Sample) (`POST /transactions/place-order`). Driver session escape hatch: [driver-escape-hatches.md](../product/driver-escape-hatches.md).

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

- Do not start a second session inside the callback.
- Work that targets another cluster is rejected as `CrossClusterOperationError` before dispatch.
- Returning a failed `Result` aborts the transaction.
- Callbacks must stay sequential; do not issue parallel operations on the same session.

## Budgets

`MongoTransactionOptions` are **client-side estimates**:

| Option | Default | Meaning |
|--------|---------|---------|
| `MaxOperations` | 1_000 | Count of Dilcore-tracked operations |
| `MaxEstimatedBytes` | 16 MiB | Estimated payload; **not** a MongoDB “16 MiB total transaction” server cap |
| `TimeLimit` | 60s | Elapsed wall clock on the client |
| `DriverOptions` | null | Passed through to the driver `TransactionOptions` |

Exceeding a budget returns `TransactionBudgetExceededError`. MongoDB still enforces its own document size and transaction lifetime limits.

Retry and idempotency remain your responsibility; see [production-mongodb.md](../security/production-mongodb.md).
