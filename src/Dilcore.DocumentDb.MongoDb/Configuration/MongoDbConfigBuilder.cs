using FluentValidation;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Configuration;

/// <summary>
/// 
/// </summary>
public class MongoDbConfigBuilder
{
    private const int DefaultMaxConnectionPoolSize = 25;
    
    private static readonly Validator BuilderValidator = new();
   
    /// <summary>
    /// Connection string to the MongoDb
    /// </summary>
    public string ConnectionString { get; private set; }
    
    /// <summary>
    /// MongoDB database name
    /// </summary>
    public string DatabaseName { get; private set; }
    
    /// <summary>
    /// Gets max connection pool size to the MongoDb connection
    /// </summary>
    public int? MaxConnectionPoolSize { get; private set; }
    
    /// <summary>
    /// Set MongoDb connection string
    /// </summary>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    public MongoDbConfigBuilder UseConnectionString(string connectionString)
    {
        ConnectionString = connectionString;
        return this;
    }

    /// <summary>
    /// Set database name 
    /// </summary>
    /// <param name="databaseName"></param>
    /// <returns></returns>
    public MongoDbConfigBuilder UseDatabaseName(string databaseName)
    {
        DatabaseName = databaseName;
        return this;
    }
    
    /// <summary>
    /// Set MongoDB connection pool size for <see cref="IMongoClient"/>. By default it is 25
    /// </summary>
    /// <param name="maxConnectionPoolSize"></param>
    /// <returns></returns>
    public MongoDbConfigBuilder UseMaxConnectionPoolSize(int maxConnectionPoolSize)
    {
        MaxConnectionPoolSize = maxConnectionPoolSize;
        return this;
    }
    
    internal MongoDbConfig Build()
    {
        BuilderValidator.ValidateAndThrow(this);
        
        return new MongoDbConfig
        {
            ConnectionString = ConnectionString,
            DatabaseName = DatabaseName,
            MaxConnectionPoolSize = MaxConnectionPoolSize ?? DefaultMaxConnectionPoolSize
        };
    }
    
    internal static MongoDbConfigBuilder Create() => new();
    
    private class Validator : AbstractValidator<MongoDbConfigBuilder>
    {
        public Validator()
        {
            RuleFor(x => x.ConnectionString)
                .NotEmpty()
                .WithMessage("MongoDB Connection String cannot be null or empty");
            
            RuleFor(x => x.DatabaseName)
                .NotEmpty()
                .WithMessage("MongoDB Database Description cannot be null or empty");
        }
    }
}