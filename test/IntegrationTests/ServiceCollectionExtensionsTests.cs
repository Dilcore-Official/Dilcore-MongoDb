using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Extensions;
using Dilcore.DocumentDb.MongoDb.Services;
using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Dilcore.DocumentDb.MongoDb.IntegrationTests;

public class ServiceCollectionExtensionsTests
{
    private readonly Testcontainers.MongoDb.MongoDbContainer _mongoDbContainer =
        new MongoDbBuilder().Build();

    [OneTimeSetUp]
    public async Task InitializeAsync()
    {
        await _mongoDbContainer.StartAsync();
    }
    
    [Test]
    public async Task ServiceCollectionExtensions_AddMongoDb()
    {
        var services = new ServiceCollection();
        var connectionString = _mongoDbContainer.GetConnectionString();
        
        services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
        {
            builder.AddDatabase("TestDB1", 
                db =>
                {
                    db.AddGenericRepository<TestEntity1>(options => options.WithCollectionName("testEntity1")
                        .WithDatabaseName("TestDB1"));
                    db.AddGenericRepository<TestEntity2>(options => options.WithCollectionName("testEntity2")
                        .WithDatabaseName("TestDB1"));
                })
                .AddDatabase("TestDB2", 
                db =>
                {
                    db.AddGenericRepository<TestEntity3>(options => options.WithCollectionName("testEntity3")
                        .WithDatabaseName("TestDB2"));
                });
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        var repository1 = serviceProvider.GetRequiredService<IGenericRepository<TestEntity1>>();
        await repository1.StoreAsync(new TestEntity1
        {
            Value = 1
        });
        
        var repository2 = serviceProvider.GetRequiredService<IGenericRepository<TestEntity2>>();
        await repository2.StoreAsync(new TestEntity2
        {
            Value = "2"
        });
        
        var repository3 = serviceProvider.GetRequiredService<IGenericRepository<TestEntity3>>();
        await repository3.StoreAsync(new TestEntity3
        {
            Value = 3.3m
        });
        
        var mongoClientProvider = serviceProvider.GetRequiredService<MongoClientProvider>();
        var mongoClient = mongoClientProvider.GetMongoClient();
        var databases = await mongoClient.ListDatabaseNamesAsync();
        var databaseNames = await databases.ToListAsync();
        
        databaseNames.Should().Contain("TestDB1");
        databaseNames.Should().Contain("TestDB2");

        var database = mongoClient.GetDatabase("TestDB1");
        var collection1 = database.GetCollection<TestEntity1>("testEntity1");
        
        var entity1 = await collection1.Find(entity1 => entity1.Value == 1).FirstOrDefaultAsync();
        entity1.Should().NotBeNull();
        
        var collection2 = database.GetCollection<TestEntity2>("testEntity2");
        
        var entity2 = await collection2.Find(entity2 => entity2.Value == "2").FirstOrDefaultAsync();
        entity2.Should().NotBeNull();
        
        var database2 = mongoClient.GetDatabase("TestDB2");
        var collection3 = database2.GetCollection<TestEntity3>("testEntity3");
        var entity3 = await collection3.Find(e => e.Value == 3.3m).FirstOrDefaultAsync();
        entity3.Should().NotBeNull();
        
        collection3 = database.GetCollection<TestEntity3>("testEntity3");
        entity3 = await collection3.Find(e => e.Value == 3.3m).FirstOrDefaultAsync();
        entity3.Should().BeNull();
    }
    
    [Test]
    public async Task ServiceCollectionExtensions_AddMongoDb_WithDifferentDatabasePrefixProviders()
    {
        var services = new ServiceCollection();
        var connectionString = _mongoDbContainer.GetConnectionString();

        services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
        {
            builder.AddDatabase("TestDB1",
                    db =>
                    {
                        db.AddCustomDatabasePrefixResolver<TestDbPrefixProvider1>();

                        db.AddGenericRepository<TestEntity1>(options => options.WithCollectionName("testEntity1")
                            .WithDatabaseName("TestDB1"));
                    })
                .AddDatabase("TestDB2",
                    db =>
                    {
                        db.AddCustomDatabasePrefixResolver<TestDbPrefixProvider2>();

                        db.AddGenericRepository<TestEntity2>(options => options.WithCollectionName("testEntity2")
                            .WithDatabaseName("TestDB2"));
                    })
                .AddDatabase("TestDB3",
                    db =>
                    {
                        db.AddCustomDatabasePrefixResolver<TestDbPrefixProvider3>();

                        db.AddGenericRepository<TestEntity3>(options => options.WithCollectionName("testEntity3")
                            .WithDatabaseName("TestDB3"));
                    });
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        var repository1 = serviceProvider.GetRequiredService<IGenericRepository<TestEntity1>>();
        await repository1.StoreAsync(new TestEntity1
        {
            Value = 1
        });
        
        var repository2 = serviceProvider.GetRequiredService<IGenericRepository<TestEntity2>>();
        await repository2.StoreAsync(new TestEntity2
        {
            Value = "2"
        });
        
        var repository3 = serviceProvider.GetRequiredService<IGenericRepository<TestEntity3>>();
        await repository3.StoreAsync(new TestEntity3
        {
            Value = 3.3m
        });
        
        var mongoClientProvider = serviceProvider.GetRequiredService<MongoClientProvider>();
        var mongoClient = mongoClientProvider.GetMongoClient();
        var databases = await mongoClient.ListDatabaseNamesAsync();
        var databaseNames = await databases.ToListAsync();
        
        databaseNames.Should().Contain("prefix1_TestDB1");
        databaseNames.Should().Contain("prefix2_TestDB2");
        databaseNames.Should().Contain("prefix3_TestDB3");

        var database1 = mongoClient.GetDatabase("prefix1_TestDB1");
        var collection1 = database1.GetCollection<TestEntity1>("testEntity1");
        
        var entity1 = await collection1.Find(entity1 => entity1.Value == 1).FirstOrDefaultAsync();
        entity1.Should().NotBeNull();
        
        var database2 = mongoClient.GetDatabase("prefix2_TestDB2");
        var collection2 = database2.GetCollection<TestEntity2>("testEntity2");
        
        var entity2 = await collection2.Find(entity2 => entity2.Value == "2").FirstOrDefaultAsync();
        entity2.Should().NotBeNull();
        
        var database3 = mongoClient.GetDatabase("prefix3_TestDB3");
        var collection3 = database3.GetCollection<TestEntity3>("testEntity3");
        
        var entity3 = await collection3.Find(entity3 => entity3.Value == 3.3m).FirstOrDefaultAsync();
        entity3.Should().NotBeNull();
    }
    
    [Test]
    public async Task ServiceCollectionExtensions_AddMongoDb_WithDifferentCollectionPrefixProviders()
    {
        var services = new ServiceCollection();
        var connectionString = _mongoDbContainer.GetConnectionString();

        services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
        {
            builder.AddDatabase("TestDB1",
                    db =>
                    {
                        db.AddCustomDatabasePrefixResolver<TestDbPrefixProvider1>();
                        db.AddCustomCollectionPrefixResolver<TestCollectionProvider>();

                        db.AddGenericRepository<TestEntity1>(options => options.WithCollectionName("testEntity1")
                            .WithDatabaseName("TestDB1"));
                    })
                .AddDatabase("TestDB2",
                    db =>
                    {
                        db.AddCustomDatabasePrefixResolver<TestDbPrefixProvider2>();

                        db.AddGenericRepository<TestEntity2>(options => options.WithCollectionName("testEntity2")
                            .WithDatabaseName("TestDB2"));
                    });
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        var repository1 = serviceProvider.GetRequiredService<IGenericRepository<TestEntity1>>();
        await repository1.StoreAsync(new TestEntity1
        {
            Value = 1
        });
        
        var repository2 = serviceProvider.GetRequiredService<IGenericRepository<TestEntity2>>();
        await repository2.StoreAsync(new TestEntity2
        {
            Value = "2"
        });
        
    
        var mongoClientProvider = serviceProvider.GetRequiredService<MongoClientProvider>();
        var mongoClient = mongoClientProvider.GetMongoClient();
        var databases = await mongoClient.ListDatabaseNamesAsync();
        var databaseNames = await databases.ToListAsync();
        
        databaseNames.Should().Contain("prefix1_TestDB1");
        databaseNames.Should().Contain("prefix2_TestDB2");

        var database1 = mongoClient.GetDatabase("prefix1_TestDB1");
        var collection1 = database1.GetCollection<TestEntity1>("collectionPrefix_testEntity1");
        
        var entity1 = await collection1.Find(entity1 => entity1.Value == 1).FirstOrDefaultAsync();
        entity1.Should().NotBeNull();
        
        var database2 = mongoClient.GetDatabase("prefix2_TestDB2");
        var collection2 = database2.GetCollection<TestEntity2>("testEntity2");
        
        var entity2 = await collection2.Find(entity2 => entity2.Value == "2").FirstOrDefaultAsync();
        entity2.Should().NotBeNull();
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
    
    public class TestEntity2 : IDocumentEntity
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime UpdateAt { get; set; }
        public DateTime? ExpireAt { get; set; }
        
        public string Value { get; set; }
    }
    
    public class TestEntity3 : IDocumentEntity
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime UpdateAt { get; set; }
        public DateTime? ExpireAt { get; set; }
        
        public decimal Value { get; set; }
    }
    
    private class TestDbPrefixProvider1 : IDocumentDatabasePrefixProvider
    {
        public Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Ok("prefix1"));
        }
    }
    
    private class TestDbPrefixProvider2 : IDocumentDatabasePrefixProvider
    {
        public Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Ok("prefix2"));
        }
    }
    
    private class TestDbPrefixProvider3 : IDocumentDatabasePrefixProvider
    {
        public Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Ok("prefix3"));
        }
    }
    
    private class TestCollectionProvider : IDocumentCollectionPrefixProvider
    {
        public Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Ok("collectionPrefix"));
        }
    }
}