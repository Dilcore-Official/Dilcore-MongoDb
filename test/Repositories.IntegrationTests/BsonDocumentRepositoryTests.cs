using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Extensions;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace Dilcore.DocumentDb.MongoDb.Repositories.IntegrationTests;

public class BsonDocumentRepositoryTests : BaseIntegrationTests
{
    private const string DatabaseName = "JsonDocuments";
    private const string Prefix = "prefix";
    const string CollectionName = "test";
    
    [Test]
    public void CustomBsonDocumentRepository_WhenGetRequiredService_ShouldBeResolved()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(builder => builder.UseConnectionString(connectionString), container =>
        {
            container.AddDatabase(DatabaseName, databaseContainer =>
            {
                databaseContainer.AddBsonDocumentCollectionFactory()
                    .AddBsonDocumentRepository<IJObjectRepository, JObjectRepository>();
            });
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Act
        var repository = serviceProvider.GetRequiredService<IJObjectRepository>();

        // Assert
        repository.Should().NotBeNull();
    }

    [Test]
    public async Task CustomBsonDocumentRepository_WhenMethodsCalled_ShouldBeSuccess()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(builder => builder.UseConnectionString(connectionString), container =>
        {
            container.AddDatabase(DatabaseName, databaseContainer =>
            {
                databaseContainer.AddBsonDocumentCollectionFactory()
                    .AddBsonDocumentRepository<IJObjectRepository, JObjectRepository>();
            });
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        var repository = serviceProvider.GetRequiredService<IJObjectRepository>();
        
        var id = Guid.NewGuid().ToString();
        var entity = JObject.Parse("{ \"name\": \"test\", \"_id\": \"" + id + "\" }");
        
        // Act
        var createdEntity = await repository.CreateJObjectAsync(entity);
        
        var result = await repository.GetJObjectAsync(id);
        
        // Assert
        result.Should().BeSuccess();
    }

    [Test]
    public async Task CustomBsonDocumentRepository_WithDependencies_WhenMethodsCalled_ShouldBeSuccess()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddScoped<SomeDependency>();
        services.AddMongoDb(builder => builder.UseConnectionString(connectionString), container =>
        {
            container.AddDatabase(DatabaseName, databaseContainer =>
            {
                databaseContainer.AddBsonDocumentCollectionFactory()
                    .AddBsonDocumentRepository<IJObjectRepository, JObjectRepositoryWithDependencies>();
            });
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        var repository = serviceProvider.GetRequiredService<IJObjectRepository>();
        
        var id = Guid.NewGuid().ToString();
        var entity = JObject.Parse("{ \"name\": \"test\", \"_id\": \"" + id + "\" }");
        
        // Act
        var createdEntity = await repository.CreateJObjectAsync(entity);
        
        var result = await repository.GetJObjectAsync(id);
        
        // Assert
        result.Should().BeSuccess();
    }
    
    [Test]
    public async Task CustomBsonDocumentRepository_WithDependencies_And_WithCustomCollectionPrefix_WhenMethodsCalled_ShouldBeSuccess()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddScoped<SomeDependency>();
        services.AddMongoDb(builder => builder.UseConnectionString(connectionString), container =>
        {
            container.AddDatabase(DatabaseName, databaseContainer =>
            {
                databaseContainer
                    .AddCustomCollectionPrefixResolver<CustomCollectionPrefixProvider>()
                    .AddBsonDocumentCollectionFactory()
                    .AddBsonDocumentRepository<IJObjectRepository, JObjectRepositoryWithDependencies>();
            });
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        var repository = serviceProvider.GetRequiredService<IJObjectRepository>();
        
        var id = Guid.NewGuid().ToString();
        var entity = JObject.Parse("{ \"name\": \"test\", \"_id\": \"" + id + "\" }");
        
        // Act
        await repository.CreateJObjectAsync(entity);
        var result = await repository.GetJObjectAsync(id);
        
        // Assert
        
        var mongoDbContainer = serviceProvider.GetRequiredKeyedService<IMongoDatabaseProvider>(DatabaseName);

        var database = await mongoDbContainer.GetDatabaseAsync(DatabaseName);
        database.Should().BeSuccess();
        
        var collectionName = $"{Prefix}_{CollectionName}";
        
        var collection = database.Value.GetCollection<BsonDocument>(collectionName);
        
        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        var resultFromDb = await collection.Find(filter).FirstOrDefaultAsync();
        
        resultFromDb.Should().NotBeNull();
    }

    #region Simple Repository Example

    public interface IJObjectRepository : IBsonDocumentRepository
    {
        Task<Result<JObject>> GetJObjectAsync(string id);
        Task<JObject> CreateJObjectAsync(JObject entity);
    }
    
    public class JObjectRepository(string dbName, IBsonDocumentCollectionFactory bsonDocumentCollectionFactory)
        : BsonDocumentRepository(dbName, bsonDocumentCollectionFactory), IJObjectRepository
    {
        public async Task<Result<JObject>> GetJObjectAsync(string id)
        {
            return await ExecuteAsync(CollectionName, async collection =>
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                var result = await collection.Find(filter).FirstOrDefaultAsync();
                
                return Result.Ok(JObject.Parse(result.ToJson()));
            });
        }
        
        public async Task<JObject> CreateJObjectAsync(JObject entity)
        {
            var json = entity.ToString();
            var document = BsonDocument.Parse(json);
            
            await ExecuteAsync(CollectionName, async collection =>
            {
                await collection.InsertOneAsync(document);
                
                return Result.Ok();
            });

            return entity;
        }
    }

    #endregion

    #region Custom Repository with dependencies Example

    public class JObjectRepositoryWithDependencies(SomeDependency dependency, string dbName, IBsonDocumentCollectionFactory bsonDocumentCollectionFactory)
        : BsonDocumentRepository(dbName, bsonDocumentCollectionFactory), IJObjectRepository
    {
        public async Task<Result<JObject>> GetJObjectAsync(string id)
        {
            return await ExecuteAsync(CollectionName, async collection =>
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                var result = await collection.Find(filter).FirstOrDefaultAsync();
                
                await dependency.Execute();
                
                return Result.Ok(JObject.Parse(result.ToJson()));
            });
        }
        
        public async Task<JObject> CreateJObjectAsync(JObject entity)
        {
            var json = entity.ToString();
            var document = BsonDocument.Parse(json);
            
            await ExecuteAsync(CollectionName, async collection =>
            {
                await collection.InsertOneAsync(document);
                
                await dependency.Execute();
                
                return Result.Ok();
            });

            return entity;
        }
    }

    public class SomeDependency
    {
        public async Task Execute()
        {
            // Some logic here
            await Task.Delay(TimeSpan.FromMicroseconds(25));
        }
    }
    
    #endregion

    #region Custom Collection Prefix Example

    public class CustomCollectionPrefixProvider : IDocumentCollectionPrefixProvider
    {
        public Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Ok(Prefix));
        }
    }
    
    #endregion
}