using Testcontainers.MongoDb;

namespace Dilcore.DocumentDb.MongoDb.IntegrationTests.Infrastructure;

public abstract class BaseIntegrationTests
{
    protected readonly Testcontainers.MongoDb.MongoDbContainer MongoDbContainer =
        new MongoDbBuilder().Build();

    [OneTimeSetUp]
    public async Task InitializeAsync()
    {
        await MongoDbContainer.StartAsync();
    }
    
    [OneTimeTearDown]
    public async Task CleanupAsync()
    {
        await MongoDbContainer.DisposeAsync();
    }
}