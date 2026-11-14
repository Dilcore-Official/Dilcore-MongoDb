using NUnit.Framework;
using Testcontainers.MongoDb;

namespace Dilcore.MongoDB.TestSupport;

/// <summary>
/// Starts one standalone MongoDB container for an integration fixture.
/// </summary>
public abstract class BaseIntegrationTests
{
    /// <summary>The MongoDB container started for this fixture.</summary>
    protected readonly MongoDbContainer MongoDbContainer = MongoTestImages.CreateStandalone();

    /// <summary>Starts the container once for the fixture.</summary>
    [OneTimeSetUp]
    public async Task InitializeAsync()
    {
        await MongoDbContainer.StartAsync();
    }

    /// <summary>Disposes the container after the fixture finishes.</summary>
    [OneTimeTearDown]
    public async Task CleanupAsync()
    {
        await MongoDbContainer.DisposeAsync();
    }
}