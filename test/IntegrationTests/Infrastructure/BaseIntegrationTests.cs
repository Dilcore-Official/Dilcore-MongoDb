using Dilcore.MongoDB.TestSupport;
using Testcontainers.MongoDb;

namespace Dilcore.MongoDB.IntegrationTests.Infrastructure;

public abstract class BaseIntegrationTests
{
    protected readonly MongoDbContainer MongoDbContainer = MongoTestImages.CreateStandalone();

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
