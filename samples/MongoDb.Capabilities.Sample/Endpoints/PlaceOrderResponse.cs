namespace MongoDb.Capabilities.Sample.Endpoints;

public sealed record PlaceOrderResponse(Guid OrderId, Guid PaymentId);