using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Extensions;
using FluentAssertions;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Dilcore.DocumentDb.MongoDb.IntegrationTests;

public class MongoCollectionProviderTests
{
    private readonly Testcontainers.MongoDb.MongoDbContainer _mongoDbContainer =
        new MongoDbBuilder().Build();

    [OneTimeSetUp]
    public async Task InitializeAsync()
    {
        await _mongoDbContainer.StartAsync();
    }
    
    [Test]
    public async Task MongoCollectionProvider_UseRegularCollection()
    {
        var services = new ServiceCollection();
        var connectionString = _mongoDbContainer.GetConnectionString();
        
        services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
        {
            builder.AddDatabase("TestDB1", 
                db =>
                {
                    db.AddMongoCollection<TestEntity1>(options =>
                    {
                        options.WithCollectionName("testEntity1")
                            .WithDatabaseName("TestDB1");
                    });
                });
        })
        .AddScoped<CustomRepository>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        var repository = serviceProvider.GetRequiredService<CustomRepository>();
        var result = await repository.GetAsync();

        result.Should().BeSuccess();
    }

    [TestCase("asc", "value_1")]
    [TestCase("desc", "value_-1")]
    public async Task MongoCollectionProvider_UseRegularCollectionWithIndexes(string sortDirection, string expectedIndexName)
    {
        var services = new ServiceCollection();
        var connectionString = _mongoDbContainer.GetConnectionString();

        services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
            {
                builder.AddDatabase("TestDB1",
                    db =>
                    {
                        db.AddMongoCollection<TestEntity1>(options =>
                            {
                                options.WithCollectionName("testEntity1")
                                    .WithDatabaseName("TestDB1");
                                    
                                    if(sortDirection == "asc")
                                    {
                                        options.WithIndexes(
                                            new CreateIndexModel<TestEntity1>(
                                                Builders<TestEntity1>.IndexKeys.Ascending(x => x.Value)));
                                    }
                                    else
                                    {
                                        options.WithIndexes(
                                            new CreateIndexModel<TestEntity1>(
                                                Builders<TestEntity1>.IndexKeys.Descending(x => x.Value)));
                                    }
                            }).AddCustomDatabasePrefixResolver<TestDbPrefixProvider>()
                            .AddCustomCollectionPrefixResolver<TestCollectionProvider>();
                    });
            })
            .AddScoped<CustomRepository>();

        var serviceProvider = services.BuildServiceProvider();

        var collectionFactory = serviceProvider.GetRequiredService<IMongoDbCollectionFactory>();

        var collectionResult =
            await collectionFactory.GetCollectionAsync<TestEntity1>("TestDB1", CancellationToken.None);
        collectionResult.Should().BeSuccess();

        var collection = collectionResult.ValueOrDefault;
        var collectionIndexes = await (await collection.Indexes.ListAsync(CancellationToken.None)).ToListAsync();

        collectionIndexes.Should().HaveCount(2);
        collectionIndexes.Should().Contain(x => x.GetValue("name") == "_id_");
        collectionIndexes.Should().Contain(x => x.GetValue("name") == expectedIndexName);
    }

    [OneTimeTearDown]
    public Task TearDown()
    {
        return _mongoDbContainer.DisposeAsync().AsTask();
    }
    
    public class TestEntity1 : IDocumentEntity
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime UpdateAt { get; set; }
        public DateTime? ExpireAt { get; set; }
        
        public int Value { get; set; }
    }
    
    private class TestDbPrefixProvider : IDocumentDatabasePrefixProvider
    {
        public Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Ok("db_prefix"));
        }
    }
    
    private class TestCollectionProvider : IDocumentCollectionPrefixProvider
    {
        public Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Ok("collection_Prefix"));
        }
    }
    
    private class CustomRepository(IMongoDbCollectionFactory collectionProvider)
    {
        public async Task<Result<TestEntity1>> GetAsync(CancellationToken cancellationToken = default)
        {
            var collectionResult = await collectionProvider.GetCollectionAsync<TestEntity1>("TestDB1", cancellationToken);

            if (collectionResult.IsFailed)
            {
                return collectionResult.ToResult();
            }
            
            return await collectionResult.ValueOrDefault.Find(FilterDefinition<TestEntity1>.Empty)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}