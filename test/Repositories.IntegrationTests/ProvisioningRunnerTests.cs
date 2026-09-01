using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Provisioning;
using Dilcore.MongoDB.Extensions;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

[Category("M3Matrix")]
public class ProvisioningRunnerTests : BaseIntegrationTests
{
    [Test]
    public async Task CollectionFactory_DoesNotCreateIndexesOnGet()
    {
        var (provider, bindingKey) = CreateHost("hot-path-indexes");
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
        await collection.InsertOneAsync(new GenericRepositoryTests.TestEntity1 { Name = "seed" });
        var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        indexes.ShouldContain(doc => doc.GetValue("name").AsString == "_id_");
        indexes.ShouldNotContain(doc => doc.GetValue("name").AsString == "name_unique");
    }

    [Test]
    public async Task Provisioner_Apply_IsIdempotent()
    {
        var (provider, _) = CreateHost("provision-once");
        using var scope = provider.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        var first = await provisioner.ApplyAsync();
        first.ShouldBeSuccess();
        first.Value.Applied.ShouldBeTrue();
        first.Value.Steps.ShouldContain(step => step.Action == "create");

        var second = await provisioner.ApplyAsync();
        second.ShouldBeSuccess();
        second.Value.Steps.ShouldNotContain(step => step.Action == "create");
        second.Value.Steps.ShouldContain(step => step.Action == "skip");
    }

    [Test]
    public async Task Provisioner_DryRun_DoesNotCreateIndexes()
    {
        var (provider, bindingKey) = CreateHost("provision-dry");
        using var scope = provider.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();

        var dry = await provisioner.DryRunAsync();
        dry.ShouldBeSuccess();
        dry.Value.Applied.ShouldBeFalse();
        dry.Value.Steps.ShouldContain(step => step.Action == "would-create");

        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
        await collection.InsertOneAsync(new GenericRepositoryTests.TestEntity1 { Name = "seed" });
        var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        indexes.ShouldNotContain(doc => doc.GetValue("name").AsString == "name_unique");
    }

    private (ServiceProvider Provider, MongoDocumentBindingKey BindingKey) CreateHost(string collection)
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("ProvisionDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<GenericRepositoryTests.TestEntity1>("e1", d => d
                    .WithCollectionName(collection)
                    .WithIndexes(new CreateIndexModel<GenericRepositoryTests.TestEntity1>(
                        Builders<GenericRepositoryTests.TestEntity1>.IndexKeys.Ascending(x => x.Name),
                        new CreateIndexOptions { Unique = true, Name = "name_unique" })));
            }));

        return (AcceptanceServiceProviderFactory.Create(services), new MongoDocumentBindingKey("e1"));
    }
}
