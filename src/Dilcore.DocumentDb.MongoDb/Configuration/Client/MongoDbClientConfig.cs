namespace Dilcore.DocumentDb.MongoDb.Configuration.Client;

internal class MongoDbClientConfig
{
    /// <summary>
    /// Gets or sets MongoDb connection string
    /// </summary>
    internal required string ConnectionString { get; set; }

    /// <summary>
    /// Max connection pool size to MongoDB Cluster
    /// </summary>
    internal int? MaxConnectionPoolSize { get; set; }
}