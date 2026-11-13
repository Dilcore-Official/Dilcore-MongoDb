using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Transactions;
using FluentResults;
using MongoDb.Capabilities.Sample.Documents;
using MongoDb.Capabilities.Sample.Http;

namespace MongoDb.Capabilities.Sample.Endpoints;

internal static class TransactionEndpoints
{
    public static void MapTransactionEndpoints(this WebApplication app)
    {
        app.MapPost("/transactions/place-order", async (
                IMongoDbTransactionRunner runner,
                PlaceOrderRequest request,
                CancellationToken ct) =>
            {
                // Multi-document transactions require a replica set (this sample starts one).
                // Resolve repositories from tx.Repositories so they share the driver session.
                // Do not start a second session inside the callback. Cross-cluster work is rejected.
                var result = await runner.WithTransactionAsync(
                    new MongoClusterKey("primary"),
                    async (tx, token) =>
                    {
                        var orders = tx.Repositories.GetRepository<Order>("orders");
                        var payments = tx.Repositories.GetRepository<Payment>("payments");

                        var order = new Order
                        {
                            Sku = request.Sku,
                            Quantity = request.Quantity,
                            ExpiresAt = DateTime.UtcNow.AddDays(30)
                        };
                        var stored = await orders.StoreAsync(order, token);
                        if (stored.IsFailed)
                        {
                            return stored.ToResult();
                        }

                        var payment = await payments.StoreAsync(new Payment
                        {
                            OrderId = stored.Value.Id,
                            Amount = request.Amount
                        }, token);
                        if (payment.IsFailed)
                        {
                            return payment.ToResult();
                        }

                        return Result.Ok(new PlaceOrderResponse(stored.Value.Id, payment.Value.Id));
                    },
                    new MongoTransactionOptions
                    {
                        MaxOperations = 1_000,
                        MaxEstimatedBytes = 16 * 1024 * 1024,
                        TimeLimit = TimeSpan.FromSeconds(60)
                    },
                    ct);

                return ResultHttp.ToHttp(result);
            })
            .WithTags("Transactions");
    }
}

public sealed record PlaceOrderRequest(string Sku, int Quantity, decimal Amount);

public sealed record PlaceOrderResponse(Guid OrderId, Guid PaymentId);
