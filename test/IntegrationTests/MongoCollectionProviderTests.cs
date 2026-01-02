using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Extensions;
using Dilcore.DocumentDb.MongoDb.IntegrationTests.Infrastructure;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.IntegrationTests;

public class MongoCollectionProviderTests : BaseIntegrationTests
{
    [Test]
    public async Task MongoCollectionProvider_UseRegularCollection()
    {
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

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

        result.ShouldBeSuccess();
    }

    [TestCase("asc", "value_1")]
    [TestCase("desc", "value_-1")]
    public async Task MongoCollectionProvider_UseRegularCollectionWithIndexes(string sortDirection, string expectedIndexName)
    {
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
            {
                builder.AddDatabase("TestDB1",
                    db =>
                    {
                        db.AddMongoCollection<TestEntity1>(options =>
                            {
                                options.WithCollectionName("testEntity1")
                                    .WithDatabaseName("TestDB1");

                                if (sortDirection == "asc")
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
        collectionResult.ShouldBeSuccess();

        var collection = collectionResult.ValueOrDefault;
        var collectionIndexes = await (await collection.Indexes.ListAsync(CancellationToken.None)).ToListAsync();

        collectionIndexes.Count.ShouldBe(2);
        collectionIndexes.ShouldContain(x => x.GetValue("name") == "_id_");
        collectionIndexes.ShouldContain(x => x.GetValue("name") == expectedIndexName);

        await collection.Indexes.DropAllAsync();
    }

    [Test]
    public async Task MongoCollectionProvider_UseTimeToLeaveIndex()
    {
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
            {
                builder.AddDatabase("TestDB1",
                    db =>
                    {
                        db.AddMongoCollection<TestEntity1>(options =>
                        {
                            options.WithCollectionName("testEntity1")
                                .WithDatabaseName("TestDB1")
                                .WithCollectionItemsTimeToLive(TimeSpan.FromSeconds(5), x => x.ExpireAt);
                        });
                    });
            })
            .AddScoped<CustomRepository>();

        var serviceProvider = services.BuildServiceProvider();

        var repository = serviceProvider.GetRequiredService<CustomRepository>();

        var result = await repository.CreateWithExpiration(TimeSpan.FromSeconds(5));
        result.ShouldBeSuccess();
        result.ValueOrDefault.ShouldNotBeNull();

        var createdEntity = await repository.GetAsync(result.ValueOrDefault.Id);
        createdEntity.ShouldBeSuccess();
        createdEntity.ValueOrDefault.ShouldNotBeNull();

        var act = async () =>
        {
            var sut = await repository.GetAsync(result.ValueOrDefault.Id);
            sut.ShouldBeSuccess();
            sut.ValueOrDefault.ShouldBeNull();
        };

        // Shouldly does not have direct NotThrowAfterAsync equivalent with polling.
        // We will implement a custom wait loop.
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(65))
        {
            var sut = await repository.GetAsync(result.ValueOrDefault.Id);
            if (sut.IsSuccess && sut.ValueOrDefault == null)
            {
                break;
            }
            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        // Final assertion
        await act();
    }

    [Test]
    public async Task MongoCollectionProvider_UseRegularCollection_Showcase()
    {
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
            {
                builder.AddDatabase("TestDB1", _ => { });
            });

        var serviceProvider = services.BuildServiceProvider();

        var collectionFactory = serviceProvider.GetRequiredService<IMongoDbCollectionFactory>();

        var collectionResult = await collectionFactory.GetCollectionAsync<TestEntity1>("TestDB1", options =>
        {
            options.WithCollectionName("testEntity1")
                .WithDatabaseName("TestDB1");
        });

        collectionResult.ShouldBeSuccess();

        var collection = collectionResult.ValueOrDefault;
        var entity = new TestEntity1
        {
            Id = Guid.NewGuid(),
            ETag = 1,
            UpdatedAt = DateTime.UtcNow,
            Value = 1,
            ExpireAt = DateTime.UtcNow.AddMinutes(5)
        };

        await collection.InsertOneAsync(entity, cancellationToken: CancellationToken.None);

        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, entity.Id);

        var sut = await collection.Find(filter).FirstOrDefaultAsync();
    }

    [OneTimeTearDown]
    public Task TearDown()
    {
        return MongoDbContainer.DisposeAsync().AsTask();
    }

    public class TestEntity1 : IDocumentEntity
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime ExpireAt { get; set; }

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

        public async Task<Result<TestEntity1>> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var collectionResult = await collectionProvider.GetCollectionAsync<TestEntity1>("TestDB1", cancellationToken);

            if (collectionResult.IsFailed)
            {
                return collectionResult.ToResult();
            }

            var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, id);

            return await collectionResult.ValueOrDefault.Find(filter)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Result<TestEntity1>> CreateWithExpiration(TimeSpan expireAfter)
        {
            var collectionResult = await collectionProvider.GetCollectionAsync<TestEntity1>("TestDB1", CancellationToken.None);

            if (collectionResult.IsFailed)
            {
                return collectionResult.ToResult();
            }

            var entity = new TestEntity1
            {
                Id = Guid.NewGuid(),
                ETag = 1,
                UpdatedAt = DateTime.UtcNow,
                Value = 1,
                ExpireAt = DateTime.UtcNow.Add(expireAfter)
            };

            await collectionResult.ValueOrDefault.InsertOneAsync(entity, cancellationToken: CancellationToken.None);
            return Result.Ok(entity);
        }
    }
}