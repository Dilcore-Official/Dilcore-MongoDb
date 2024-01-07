namespace Dilcore.DocumentDb.MongoDb.Configuration;

internal class MongoDbConfig
{
    /// <summary>
    /// Gets or sets MongoDb connection string
    /// </summary>
    internal string ConnectionString { get; set; }
    
    internal string DatabaseName { get; set; }
    
    /// <summary>
    /// Max connection pool size to MongoDB Cluster
    /// </summary>
    internal int? MaxConnectionPoolSize { get; set; }
}