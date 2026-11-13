using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Provisioning;
using Dilcore.MongoDB.Extensions;
using FluentResults;
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

    [Test]
    public async Task Provisioner_Apply_PreservesExistingCollectionDocuments()
    {
        var collectionName = "provision-preserve-docs";
        var (provider, bindingKey) = CreateHost(collectionName);
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
        var seedId = Guid.CreateVersion7();
        await collection.InsertOneAsync(new GenericRepositoryTests.TestEntity1
        {
            Id = seedId,
            Name = "keep-me"
        });

        var applied = await provisioner.ApplyAsync();
        applied.ShouldBeSuccess();
        applied.Value.Steps.ShouldContain(step =>
            step.Name == "e1:collection" && step.Action == "skip" && step.Details == collectionName);

        var persisted = await collection.Find(x => x.Id == seedId).FirstOrDefaultAsync();
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe("keep-me");
        (await collection.CountDocumentsAsync(FilterDefinition<GenericRepositoryTests.TestEntity1>.Empty))
            .ShouldBe(1);
    }

    [Test]
    public async Task Provisioner_Apply_SkipsExistingNamedIndex()
    {
        var (provider, bindingKey) = CreateHost("provision-existing-index");
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<GenericRepositoryTests.TestEntity1>(
                Builders<GenericRepositoryTests.TestEntity1>.IndexKeys.Ascending(x => x.Name),
                new CreateIndexOptions { Unique = true, Name = "name_unique" }));

        var applied = await provisioner.ApplyAsync();
        applied.ShouldBeSuccess();
        applied.Value.Steps.ShouldContain(step =>
            step.Name == "e1:index:name_unique" && step.Action == "skip");
        applied.Value.Steps.ShouldNotContain(step =>
            step.Name == "e1:index:name_unique" && step.Action == "create");

        var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        indexes.Count(doc => doc.GetValue("name").AsString == "name_unique").ShouldBe(1);
        indexes.Single(doc => doc.GetValue("name").AsString == "name_unique")
            .GetValue("unique", false).ToBoolean()
            .ShouldBeTrue();
    }

    [Test]
    public async Task Provisioner_Apply_CreatesMissingIndex_AndRemovesOrphanIndex()
    {
        var (provider, bindingKey) = CreateHost("provision-partial-indexes");
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<GenericRepositoryTests.TestEntity1>(
                Builders<GenericRepositoryTests.TestEntity1>.IndexKeys.Ascending(x => x.Value),
                new CreateIndexOptions { Name = "preexisting_value" }));

        var applied = await provisioner.ApplyAsync();
        applied.ShouldBeSuccess();
        applied.Value.Steps.ShouldContain(step =>
            step.Name == "e1:index:name_unique" && step.Action == "create");
        applied.Value.Steps.ShouldContain(step =>
            step.Name == "e1:index:preexisting_value" && step.Action == "remove");

        var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        var names = indexes.Select(doc => doc.GetValue("name").AsString).ToHashSet();
        names.ShouldNotContain("preexisting_value");
        names.ShouldContain("name_unique");
        names.ShouldContain("_id_");
    }

    [Test]
    public async Task Provisioner_RecreatesIndex_WhenDefinitionChanges_PreservesDocuments()
    {
        const string collectionName = "provision-recreate-index";
        const string databaseName = "ProvisionRecreateDB";
        var seedId = Guid.CreateVersion7();

        var (firstProvider, firstKey) = CreateHost(
            collectionName,
            databaseName: databaseName,
            indexes:
            [
                new CreateIndexModel<GenericRepositoryTests.TestEntity1>(
                    Builders<GenericRepositoryTests.TestEntity1>.IndexKeys.Ascending(x => x.Name),
                    new CreateIndexOptions { Unique = true, Name = "name_unique" })
            ]);
        using (firstProvider)
        using (var firstScope = firstProvider.CreateScope())
        {
            var factory = firstScope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
            var provisioner = firstScope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
            var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(firstKey)).Value;
            await collection.InsertOneAsync(new GenericRepositoryTests.TestEntity1
            {
                Id = seedId,
                Name = "keep-me",
                Value = "v1"
            });

            (await provisioner.ApplyAsync()).ShouldBeSuccess();
        }

        var (secondProvider, secondKey) = CreateHost(
            collectionName,
            databaseName: databaseName,
            indexes:
            [
                new CreateIndexModel<GenericRepositoryTests.TestEntity1>(
                    Builders<GenericRepositoryTests.TestEntity1>.IndexKeys.Ascending(x => x.Value),
                    new CreateIndexOptions { Unique = true, Name = "name_unique" })
            ]);
        using (secondProvider)
        using (var secondScope = secondProvider.CreateScope())
        {
            var factory = secondScope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
            var provisioner = secondScope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
            var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(secondKey)).Value;

            var dry = await provisioner.DryRunAsync();
            dry.ShouldBeSuccess();
            dry.Value.Steps.ShouldContain(step =>
                step.Name == "e1:index:name_unique" && step.Action == "would-recreate");

            var applied = await provisioner.ApplyAsync();
            applied.ShouldBeSuccess();
            applied.Value.Steps.ShouldContain(step =>
                step.Name == "e1:index:name_unique" && step.Action == "recreate");

            var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
            indexes.Count(doc => doc.GetValue("name").AsString == "name_unique").ShouldBe(1);
            var key = indexes.Single(doc => doc.GetValue("name").AsString == "name_unique")
                .GetValue("key").AsBsonDocument;
            key.Names.ShouldContain("value");
            key.Names.ShouldNotContain("name");

            var persisted = await collection.Find(x => x.Id == seedId).FirstOrDefaultAsync();
            persisted.ShouldNotBeNull();
            persisted.Name.ShouldBe("keep-me");
            persisted.Value.ShouldBe("v1");

            var again = await provisioner.ApplyAsync();
            again.ShouldBeSuccess();
            again.Value.Steps.ShouldContain(step =>
                step.Name == "e1:index:name_unique" && step.Action == "skip");
            again.Value.Steps.ShouldNotContain(step =>
                step.Action == "recreate" || step.Action == "create" || step.Action == "remove");
        }
    }

    [Test]
    public async Task Provisioner_RemovesIndex_WhenDroppedFromBinding_PreservesDocuments()
    {
        const string collectionName = "provision-remove-index";
        const string databaseName = "ProvisionRemoveDB";
        var seedId = Guid.CreateVersion7();

        var (firstProvider, firstKey) = CreateHost(collectionName, databaseName: databaseName);
        using (firstProvider)
        using (var firstScope = firstProvider.CreateScope())
        {
            var factory = firstScope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
            var provisioner = firstScope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
            (await provisioner.ApplyAsync()).ShouldBeSuccess();

            var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(firstKey)).Value;
            await collection.InsertOneAsync(new GenericRepositoryTests.TestEntity1
            {
                Id = seedId,
                Name = "keep-me"
            });
        }

        var (secondProvider, secondKey) = CreateHost(
            collectionName,
            withUniqueNameIndex: false,
            databaseName: databaseName);
        using (secondProvider)
        using (var secondScope = secondProvider.CreateScope())
        {
            var factory = secondScope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
            var provisioner = secondScope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
            var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(secondKey)).Value;

            var dry = await provisioner.DryRunAsync();
            dry.ShouldBeSuccess();
            dry.Value.Steps.ShouldContain(step =>
                step.Name == "e1:index:name_unique" && step.Action == "would-remove");

            var applied = await provisioner.ApplyAsync();
            applied.ShouldBeSuccess();
            applied.Value.Steps.ShouldContain(step =>
                step.Name == "e1:index:name_unique" && step.Action == "remove");

            var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
            indexes.Select(doc => doc.GetValue("name").AsString).ShouldBe(["_id_"]);

            var persisted = await collection.Find(x => x.Id == seedId).FirstOrDefaultAsync();
            persisted.ShouldNotBeNull();
            persisted.Name.ShouldBe("keep-me");
        }
    }

    [Test]
    public async Task Provisioner_RemovesTtlIndex_WhenRemovedFromBinding_PreservesDocuments()
    {
        const string collectionName = "provision-remove-ttl";
        const string databaseName = "ProvisionRemoveTtlDB";
        var seedId = Guid.CreateVersion7();
        var ttlName = $"{collectionName}_ttl";

        var (firstProvider, firstKey) = CreateHost(
            collectionName,
            withUniqueNameIndex: false,
            withTtl: true,
            databaseName: databaseName);
        using (firstProvider)
        using (var firstScope = firstProvider.CreateScope())
        {
            var factory = firstScope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
            var provisioner = firstScope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
            (await provisioner.ApplyAsync()).ShouldBeSuccess();

            var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(firstKey)).Value;
            await collection.InsertOneAsync(new GenericRepositoryTests.TestEntity1
            {
                Id = seedId,
                Name = "ttl-keep",
                CreatedAt = DateTime.UtcNow
            });
        }

        var (secondProvider, secondKey) = CreateHost(
            collectionName,
            withUniqueNameIndex: false,
            withTtl: false,
            databaseName: databaseName);
        using (secondProvider)
        using (var secondScope = secondProvider.CreateScope())
        {
            var factory = secondScope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
            var provisioner = secondScope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
            var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(secondKey)).Value;

            var dry = await provisioner.DryRunAsync();
            dry.ShouldBeSuccess();
            dry.Value.Steps.ShouldContain(step =>
                step.Name == $"e1:index:{ttlName}" && step.Action == "would-remove");

            var applied = await provisioner.ApplyAsync();
            applied.ShouldBeSuccess();
            applied.Value.Steps.ShouldContain(step =>
                step.Name == $"e1:index:{ttlName}" && step.Action == "remove");

            var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
            indexes.ShouldNotContain(doc => doc.GetValue("name").AsString == ttlName);
            indexes.ShouldContain(doc => doc.GetValue("name").AsString == "_id_");

            var persisted = await collection.Find(x => x.Id == seedId).FirstOrDefaultAsync();
            persisted.ShouldNotBeNull();
            persisted.Name.ShouldBe("ttl-keep");
        }
    }

    [Test]
    public async Task Provisioner_DryRun_ReportsSkipForExistingCollectionWithoutMutating()
    {
        var collectionName = "provision-dry-existing";
        var (provider, bindingKey) = CreateHost(collectionName);
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
        await collection.InsertOneAsync(new GenericRepositoryTests.TestEntity1 { Name = "seed" });
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<GenericRepositoryTests.TestEntity1>(
                Builders<GenericRepositoryTests.TestEntity1>.IndexKeys.Ascending(x => x.Name),
                new CreateIndexOptions { Unique = true, Name = "name_unique" }));

        var beforeIndexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        var beforeCount = await collection.CountDocumentsAsync(FilterDefinition<GenericRepositoryTests.TestEntity1>.Empty);

        var dry = await provisioner.DryRunAsync();
        dry.ShouldBeSuccess();
        dry.Value.Applied.ShouldBeFalse();
        dry.Value.Steps.ShouldContain(step =>
            step.Name == "e1:collection" && step.Action == "skip");
        dry.Value.Steps.ShouldContain(step =>
            step.Name == "e1:index:name_unique" && step.Action == "skip");
        dry.Value.Steps.ShouldNotContain(step => step.Action == "create" || step.Action == "would-create");

        (await collection.CountDocumentsAsync(FilterDefinition<GenericRepositoryTests.TestEntity1>.Empty))
            .ShouldBe(beforeCount);
        var afterIndexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        afterIndexes.Select(doc => doc.GetValue("name").AsString).OrderBy(x => x)
            .ShouldBe(beforeIndexes.Select(doc => doc.GetValue("name").AsString).OrderBy(x => x));
    }

    [Test]
    public async Task Provisioner_Apply_CreatesTtlIndex_AndSkipsOnSecondRun()
    {
        var collectionName = "provision-ttl";
        var (provider, bindingKey) = CreateHost(collectionName, withUniqueNameIndex: false, withTtl: true);
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
        var seedId = Guid.CreateVersion7();
        await collection.InsertOneAsync(new GenericRepositoryTests.TestEntity1
        {
            Id = seedId,
            Name = "ttl-seed",
            CreatedAt = DateTime.UtcNow
        });

        var first = await provisioner.ApplyAsync();
        first.ShouldBeSuccess();
        first.Value.Steps.ShouldContain(step =>
            step.Name == $"e1:index:{collectionName}_ttl" && step.Action == "create");

        var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        var ttl = indexes.Single(doc => doc.GetValue("name").AsString == $"{collectionName}_ttl");
        ttl.GetValue("expireAfterSeconds").ToInt64().ShouldBe((long)TimeSpan.FromHours(1).TotalSeconds);

        (await collection.Find(x => x.Id == seedId).FirstOrDefaultAsync()).ShouldNotBeNull();

        var second = await provisioner.ApplyAsync();
        second.ShouldBeSuccess();
        second.Value.Steps.ShouldContain(step =>
            step.Name == $"e1:index:{collectionName}_ttl" && step.Action == "skip");
        (await (await collection.Indexes.ListAsync()).ToListAsync())
            .Count(doc => doc.GetValue("name").AsString == $"{collectionName}_ttl")
            .ShouldBe(1);
    }

    [Test]
    public async Task Provisioner_ExecutesCustomStep_AndIncludesItInReport()
    {
        var (provider, _) = CreateHost(
            "provision-extra-step",
            withUniqueNameIndex: false,
            extraSteps: [new RecordingProvisioningStep()]);
        using var scope = provider.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        var dry = await provisioner.DryRunAsync();
        dry.ShouldBeSuccess();
        dry.Value.Steps.ShouldContain(step =>
            step.Name == RecordingProvisioningStep.StepName && step.Action == "would-apply");

        var applied = await provisioner.ApplyAsync();
        applied.ShouldBeSuccess();
        applied.Value.Steps.ShouldContain(step =>
            step.Name == RecordingProvisioningStep.StepName && step.Action == "applied");
    }

    [Test]
    public async Task Provisioner_CustomStepFailure_FailsClosedWithoutLedgerWrite()
    {
        const string databaseName = "ProvisionFailDB";
        var (provider, _) = CreateHost(
            "provision-extra-fail",
            withUniqueNameIndex: false,
            extraSteps: [new FailingProvisioningStep()],
            databaseName: databaseName);
        using var scope = provider.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
        var databaseResolver = scope.ServiceProvider.GetRequiredService<IMongoDatabaseResolver>();

        var applied = await provisioner.ApplyAsync();
        applied.ShouldBeFailure();
        applied.Errors.ShouldContain(error => error.Message.Contains("boom", StringComparison.Ordinal));

        var database = (await databaseResolver.GetDatabaseAsync(new MongoDatabaseKey(databaseName))).Value;
        var ledgerNames = await (await database.ListCollectionNamesAsync()).ToListAsync();
        ledgerNames.ShouldNotContain("_dilcore_provisioning");
    }

    [Test]
    public async Task Provisioner_Apply_CollectionOnlyBinding_DoesNotCreateExtraIndexes()
    {
        var (provider, bindingKey) = CreateHost("provision-collection-only", withUniqueNameIndex: false);
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        var applied = await provisioner.ApplyAsync();
        applied.ShouldBeSuccess();
        applied.Value.Steps.ShouldContain(step => step.Name == "e1:collection" && step.Action == "create");
        applied.Value.Steps.ShouldNotContain(step => step.Name.Contains(":index:", StringComparison.Ordinal));

        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
        var indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        indexes.Select(doc => doc.GetValue("name").AsString).ShouldBe(["_id_"]);
    }

    private (ServiceProvider Provider, MongoDocumentBindingKey BindingKey) CreateHost(
        string collection,
        bool withUniqueNameIndex = true,
        bool withTtl = false,
        IEnumerable<IProvisioningStep>? extraSteps = null,
        string databaseName = "ProvisionDB",
        CreateIndexModel<GenericRepositoryTests.TestEntity1>[]? indexes = null)
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase(databaseName, db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<GenericRepositoryTests.TestEntity1>("e1", d =>
                {
                    d.WithCollectionName(collection);
                    if (indexes is { Length: > 0 })
                    {
                        d.WithIndexes(indexes);
                    }
                    else if (withUniqueNameIndex)
                    {
                        d.WithIndexes(new CreateIndexModel<GenericRepositoryTests.TestEntity1>(
                            Builders<GenericRepositoryTests.TestEntity1>.IndexKeys.Ascending(x => x.Name),
                            new CreateIndexOptions { Unique = true, Name = "name_unique" }));
                    }

                    if (withTtl)
                    {
                        d.WithCollectionItemsTimeToLive(TimeSpan.FromHours(1), x => x.CreatedAt);
                    }
                });
            }));

        if (extraSteps is not null)
        {
            foreach (var step in extraSteps)
            {
                services.AddSingleton<IProvisioningStep>(step);
            }
        }

        return (AcceptanceServiceProviderFactory.Create(services), new MongoDocumentBindingKey("e1"));
    }

    private sealed class RecordingProvisioningStep : IProvisioningStep
    {
        public const string StepName = "recording-step";
        public string Name => StepName;

        public Task<Result<ProvisioningStepResult>> ExecuteAsync(
            IMongoDatabaseResolver databaseResolver,
            bool apply,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Ok(new ProvisioningStepResult
            {
                Name = Name,
                Action = apply ? "applied" : "would-apply"
            }));
        }
    }

    private sealed class FailingProvisioningStep : IProvisioningStep
    {
        public string Name => "failing-step";

        public Task<Result<ProvisioningStepResult>> ExecuteAsync(
            IMongoDatabaseResolver databaseResolver,
            bool apply,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Fail<ProvisioningStepResult>("boom"));
        }
    }
}