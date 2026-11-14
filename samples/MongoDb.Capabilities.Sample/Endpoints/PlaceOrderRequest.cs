namespace MongoDb.Capabilities.Sample.Endpoints;

public sealed record PlaceOrderRequest(string Sku, int Quantity, decimal Amount);