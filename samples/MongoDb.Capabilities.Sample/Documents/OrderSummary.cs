namespace MongoDb.Capabilities.Sample.Documents;

public sealed class OrderSummary
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = "";

    public int Quantity { get; set; }
}
