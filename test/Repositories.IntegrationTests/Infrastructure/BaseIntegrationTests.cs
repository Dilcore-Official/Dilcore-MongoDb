using Testcontainers.MongoDb;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Infrastructure;

public abstract class BaseIntegrationTests
{
    protected readonly MongoDbContainer MongoDbContainer =
        new MongoDbBuilder("mongo:7.0").Build();

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
