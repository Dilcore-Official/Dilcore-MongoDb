using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Extensions;
using Dilcore.DocumentDb.MongoDb.IntegrationTests.Infrastructure;
using FluentResults;

namespace Dilcore.DocumentDb.MongoDb.IntegrationTests;

public class BsonDocumentCollectionFactoryTests : BaseIntegrationTests
{
    private const string DatabaseName = "TestDb";
    private const string CollectionName = "TestCollection";

    private const string TestPrefix = "TestPrefix";

    [Test]
    public async Task GetCollectionAsync_WhenCalled_ReturnsCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(builder => builder.UseConnectionString(connectionString), container =>
        {
            container.AddDatabase(DatabaseName, databaseContainer =>
            {
                databaseContainer.AddBsonDocumentCollectionFactory();
            });
        });

        var serviceProvider = services.BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IBsonDocumentCollectionFactory>();

        // Act
        var result = await factory.GetCollectionAsync(DatabaseName, CollectionName);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var collection = result.Value;

        collection.ShouldNotBeNull();

        var collectionName = collection.CollectionNamespace.CollectionName;

        collectionName.ShouldBe(CollectionName);
    }

    [Test]
    public async Task GetCollectionAsync_WhenDatabaseDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(builder => builder.UseConnectionString(connectionString), container =>
        {
            container.AddDatabase("TestDb", databaseContainer =>
            {
                databaseContainer.AddBsonDocumentCollectionFactory();
            });
        });

        var serviceProvider = services.BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IBsonDocumentCollectionFactory>();

        // Act
        var result = await factory.GetCollectionAsync("NonExistentTestDb", "NonExistentCollection");

        // Assert
        result.ShouldBeFailure();
    }

    [Test]
    public async Task GetCollectionAsync_WithCustomCollectionPrefix_WhenCalled_ReturnsCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(builder => builder.UseConnectionString(connectionString), container =>
        {
            container.AddDatabase(DatabaseName, databaseContainer =>
            {
                databaseContainer
                    .AddBsonDocumentCollectionFactory()
                    .AddCustomCollectionPrefixResolver<TestCollectionPrefixResolver>();
            });
        });

        var serviceProvider = services.BuildServiceProvider();

        var factory = serviceProvider.GetRequiredService<IBsonDocumentCollectionFactory>();

        // Act
        var result = await factory.GetCollectionAsync(DatabaseName, CollectionName);

        // Assert
        result.ShouldBeSuccess();

        var collection = result.Value;

        collection.ShouldNotBeNull();

        var collectionName = collection.CollectionNamespace.CollectionName;

        collectionName.ShouldBe($"{TestPrefix}_{CollectionName}");
    }

    [Test]
    public async Task GetMultipleCollections_FromMultipleDatabases_WhenCalled_ReturnEachCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        var dbName1 = $"{DatabaseName}_1";
        var dbName2 = $"{DatabaseName}_2";

        services.AddMongoDb(builder => builder.UseConnectionString(connectionString), container =>
        {
            container.AddDatabase(dbName1, databaseContainer =>
            {
                databaseContainer
                    .AddBsonDocumentCollectionFactory()
                    .AddCustomCollectionPrefixResolver<TestCollectionPrefixResolver>();
            });
            container.AddDatabase(dbName2, databaseContainer =>
            {
                databaseContainer
                    .AddBsonDocumentCollectionFactory()
                    .AddCustomCollectionPrefixResolver<TestCollectionPrefixResolver>();
            });
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var factory = serviceProvider.GetRequiredService<IBsonDocumentCollectionFactory>();

        var result1 = await factory.GetCollectionAsync(dbName1, CollectionName);
        var result2 = await factory.GetCollectionAsync(dbName2, CollectionName);

        // Assert
        result1.ShouldBeSuccess();
        result2.ShouldBeSuccess();

        var collection1 = result1.Value;
        var collection2 = result2.Value;

        collection1.ShouldNotBeNull();
        collection2.ShouldNotBeNull();

        var collectionName1 = collection1.CollectionNamespace.CollectionName;
        var collectionName2 = collection2.CollectionNamespace.CollectionName;

        collectionName1.ShouldBe($"{TestPrefix}_{CollectionName}");
        collectionName2.ShouldBe($"{TestPrefix}_{CollectionName}");
    }

    private class TestCollectionPrefixResolver : IDocumentCollectionPrefixProvider
    {
        public Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Ok(TestPrefix));
        }
    }
}