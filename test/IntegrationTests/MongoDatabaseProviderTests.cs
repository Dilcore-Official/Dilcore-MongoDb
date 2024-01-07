using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Extensions;
using Dilcore.DocumentDb.MongoDb.Services;
using FluentAssertions;
using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MongoDb;

namespace Dilcore.DocumentDb.MongoDb.IntegrationTests;

public class MongoDatabaseProviderTests
{
    private const string TestPrefix = "test";
    private const string TestDbName = "TestDB";
    
    private readonly Testcontainers.MongoDb.MongoDbContainer _mongoDbContainer =
        new MongoDbBuilder().Build();

    [OneTimeSetUp]
    public async Task InitializeAsync()
    {
        await _mongoDbContainer.StartAsync();
    }
    
    [Test]
    public async Task MongoDbProvider_GetDatabase()
    {
        var services = new ServiceCollection();
        var connectionString = _mongoDbContainer.GetConnectionString();
        
        services.ConfigureMongoDb(configBuilder => configBuilder
            .UseConnectionString(connectionString), _ =>
        {
        });

        var serviceProvider = services.BuildServiceProvider();
        
        var provider = serviceProvider.GetRequiredService<MongoDatabaseProvider>();
        var databaseResult = await provider.GetDatabaseAsync(TestDbName);
        
        databaseResult.Should().BeSuccess();
        databaseResult.Value.Should().NotBeNull();
        
        var database = databaseResult.Value;
        database.DatabaseNamespace.DatabaseName.Should().Be(TestDbName);
    }
    
    [Test]
    public async Task MongoDbProvider_GetDatabase_WithPrefix()
    {
        var services = new ServiceCollection();
        var connectionString = _mongoDbContainer.GetConnectionString();
        
        services.ConfigureMongoDb(configBuilder => configBuilder
            .UseConnectionString(connectionString), _ =>
        {
           
        }).AddCustomDatabasePrefixProvider<TestDbPrefixResolver>();

        var serviceProvider = services.BuildServiceProvider();
        
        var provider = serviceProvider.GetRequiredService<MongoDatabaseProvider>();
        var databaseResult = await provider.GetDatabaseAsync(TestDbName);
        
        databaseResult.Should().BeSuccess();
        databaseResult.Value.Should().NotBeNull();
        
        var database = databaseResult.Value;
        database.DatabaseNamespace.DatabaseName.Should().Be($"{TestPrefix}_{TestDbName}");
    }
    
    [OneTimeTearDown]
    public Task TearDown()
    {
        return _mongoDbContainer.DisposeAsync().AsTask();
    }

    private class TestDbPrefixResolver : IDocumentDatabasePrefixProvider
    {
        public Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Ok(TestPrefix));
        }
    }
}