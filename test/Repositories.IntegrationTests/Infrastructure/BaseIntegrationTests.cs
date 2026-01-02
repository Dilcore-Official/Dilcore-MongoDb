using Testcontainers.MongoDb;

namespace Dilcore.DocumentDb.MongoDb.Repositories.IntegrationTests.Infrastructure;

public abstract class BaseIntegrationTests
{
    protected readonly Testcontainers.MongoDb.MongoDbContainer MongoDbContainer =
        new MongoDbBuilder("mongo:latest").Build();

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