using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Provisioning;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

[Category("M3Matrix")]
public class ProvisioningRunnerTests : BaseIntegrationTests
{
    [Test]
    public async Task CollectionFactory_DoesNotCreateIndexesOnGet()
    {
        // Arrange
        var (provider, bindingKey) = CreateHost("hot-path-indexes");
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();

        // Act
        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
        var seed = new GenericRepositoryTests.TestEntity1 { Name = "seed" };
        await collection.InsertOneAsync(seed);
        var indexCursor = await collection.Indexes.ListAsync();
        var indexes = await indexCursor.ToListAsync();

        // Assert
        indexes.ShouldContain(doc => doc.GetValue("name").AsString == "_id_");
        indexes.ShouldNotContain(doc => doc.GetValue("name").AsString == "name_unique");
    }

    [Test]
    public async Task Provisioner_Apply_IsIdempotent()
    {
        // Arrange
        var (provider, _) = CreateHost("provision-once");
        using var scope = provider.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        // Act
        var first = await provisioner.ApplyAsync();
        var second = first.IsSuccess
            ? await provisioner.ApplyAsync()
            : null;

        // Assert
        first.ShouldBeSuccess();
        first.Value.Applied.ShouldBeTrue();
        first.Value.Steps.ShouldContain(step => step.Action == "create");
        second!.ShouldBeSuccess();
        second!.Value.Steps.ShouldNotContain(step => step.Action == "create");
        second.Value.Steps.ShouldContain(step => step.Action == "skip");
    }

    [Test]
    public async Task Provisioner_DryRun_DoesNotCreateIndexes()
    {
        // Arrange
        var (provider, bindingKey) = CreateHost("provision-dry");
        using var scope = provider.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();

        // Act
        var dry = await provisioner.DryRunAsync();
        List<BsonDocument>? indexes = null;
        if (dry.IsSuccess)
        {
            var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
            var seed = new GenericRepositoryTests.TestEntity1 { Name = "seed" };
            await collection.InsertOneAsync(seed);
            var indexCursor = await collection.Indexes.ListAsync();
            indexes = await indexCursor.ToListAsync();
        }

        // Assert
        dry.ShouldBeSuccess();
        dry.Value.Applied.ShouldBeFalse();
        dry.Value.Steps.ShouldContain(step => step.Action == "would-create");
        indexes!.ShouldNotContain(doc => doc.GetValue("name").AsString == "name_unique");
    }

    [Test]
    public async Task Provisioner_Apply_PreservesExistingCollectionDocuments()
    {
        // Arrange
        var collectionName = "provision-preserve-docs";
        var (provider, bindingKey) = CreateHost(collectionName);
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
        var seedId = Guid.CreateVersion7();
        var seed = new GenericRepositoryTests.TestEntity1
        {
            Id = seedId,
            Name = "keep-me"
        };
        await collection.InsertOneAsync(seed);

        // Act
        var applied = await provisioner.ApplyAsync();
        var persisted = applied.IsSuccess
            ? await collection.Find(x => x.Id == seedId).FirstOrDefaultAsync()
            : null;
        var count = applied.IsSuccess
            ? await collection.CountDocumentsAsync(FilterDefinition<GenericRepositoryTests.TestEntity1>.Empty)
            : (long?)null;

        // Assert
        applied.ShouldBeSuccess();
        applied.Value.Steps.ShouldContain(step =>
            step.Name == "e1:collection" && step.Action == "skip" && step.Details == collectionName);
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe("keep-me");
        count.ShouldBe(1);
    }

    [Test]
    public async Task Provisioner_Apply_SkipsExistingNamedIndex()
    {
        // Arrange
        var (provider, bindingKey) = CreateHost("provision-existing-index");
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
        var index = new CreateIndexModel<GenericRepositoryTests.TestEntity1>(
            Builders<GenericRepositoryTests.TestEntity1>.IndexKeys.Ascending(x => x.Name),
            new CreateIndexOptions { Unique = true, Name = "name_unique" });
        await collection.Indexes.CreateOneAsync(index);

        // Act
        var applied = await provisioner.ApplyAsync();
        List<BsonDocument>? indexes = null;
        if (applied.IsSuccess)
        {
            var indexCursor = await collection.Indexes.ListAsync();
            indexes = await indexCursor.ToListAsync();
        }

        // Assert
        applied.ShouldBeSuccess();
        applied.Value.Steps.ShouldContain(step =>
            step.Name == "e1:index:name_unique" && step.Action == "skip");
        applied.Value.Steps.ShouldNotContain(step =>
            step.Name == "e1:index:name_unique" && step.Action == "create");
        indexes!.Count(doc => doc.GetValue("name").AsString == "name_unique").ShouldBe(1);
        indexes!.Single(doc => doc.GetValue("name").AsString == "name_unique")
            .GetValue("unique", false).ToBoolean()
            .ShouldBeTrue();
    }

    [Test]
    public async Task Provisioner_Apply_CreatesMissingIndex_AndRemovesOrphanIndex()
    {
        // Arrange
        var (provider, bindingKey) = CreateHost("provision-partial-indexes");
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
        var preexisting = new CreateIndexModel<GenericRepositoryTests.TestEntity1>(
            Builders<GenericRepositoryTests.TestEntity1>.IndexKeys.Ascending(x => x.Value),
            new CreateIndexOptions { Name = "preexisting_value" });
        await collection.Indexes.CreateOneAsync(preexisting);

        // Act
        var applied = await provisioner.ApplyAsync();
        HashSet<string>? names = null;
        if (applied.IsSuccess)
        {
            var indexCursor = await collection.Indexes.ListAsync();
            var indexes = await indexCursor.ToListAsync();
            names = indexes.Select(doc => doc.GetValue("name").AsString).ToHashSet();
        }

        // Assert
        applied.ShouldBeSuccess();
        applied.Value.Steps.ShouldContain(step =>
            step.Name == "e1:index:name_unique" && step.Action == "create");
        applied.Value.Steps.ShouldContain(step =>
            step.Name == "e1:index:preexisting_value" && step.Action == "remove");
        var indexNames = names!;
        indexNames.ShouldNotContain("preexisting_value");
        indexNames.ShouldContain("name_unique");
        indexNames.ShouldContain("_id_");
    }

    [Test]
    public async Task Provisioner_RecreatesIndex_WhenDefinitionChanges_PreservesDocuments()
    {
        // Arrange
        const string collectionName = "provision-recreate-index";
        const string databaseName = "ProvisionRecreateDB";
        var seedId = Guid.CreateVersion7();
        var nameIndex = new CreateIndexModel<GenericRepositoryTests.TestEntity1>(
            Builders<GenericRepositoryTests.TestEntity1>.IndexKeys.Ascending(x => x.Name),
            new CreateIndexOptions { Unique = true, Name = "name_unique" });

        var (firstProvider, firstKey) = CreateHost(
            collectionName,
            databaseName: databaseName,
            indexes: [nameIndex]);
        Result<ProvisioningReport> firstApplied;
        using (firstProvider)
        using (var firstScope = firstProvider.CreateScope())
        {
            var factory = firstScope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
            var provisioner = firstScope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
            var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(firstKey)).Value;
            var seed = new GenericRepositoryTests.TestEntity1
            {
                Id = seedId,
                Name = "keep-me",
                Value = "v1"
            };
            await collection.InsertOneAsync(seed);
            firstApplied = await provisioner.ApplyAsync();
        }

        // Act
        Result<ProvisioningReport>? dry = null;
        Result<ProvisioningReport>? applied = null;
        List<BsonDocument>? indexes = null;
        GenericRepositoryTests.TestEntity1? persisted = null;
        Result<ProvisioningReport>? again = null;
        if (firstApplied.IsSuccess)
        {
            var valueIndex = new CreateIndexModel<GenericRepositoryTests.TestEntity1>(
                Builders<GenericRepositoryTests.TestEntity1>.IndexKeys.Ascending(x => x.Value),
                new CreateIndexOptions { Unique = true, Name = "name_unique" });
            var (secondProvider, secondKey) = CreateHost(
                collectionName,
                databaseName: databaseName,
                indexes: [valueIndex]);
            using (secondProvider)
            using (var secondScope = secondProvider.CreateScope())
            {
                var factory = secondScope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
                var provisioner = secondScope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
                var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(secondKey)).Value;
                dry = await provisioner.DryRunAsync();
                applied = dry.IsSuccess
                    ? await provisioner.ApplyAsync()
                    : null;
                if (applied is { IsSuccess: true })
                {
                    var indexCursor = await collection.Indexes.ListAsync();
                    indexes = await indexCursor.ToListAsync();
                    persisted = await collection.Find(x => x.Id == seedId).FirstOrDefaultAsync();
                    again = await provisioner.ApplyAsync();
                }
            }
        }

        // Assert
        firstApplied.ShouldBeSuccess();
        dry!.ShouldBeSuccess();
        dry!.Value.Steps.ShouldContain(step =>
            step.Name == "e1:index:name_unique" && step.Action == "would-recreate");
        applied!.ShouldBeSuccess();
        applied!.Value.Steps.ShouldContain(step =>
            step.Name == "e1:index:name_unique" && step.Action == "recreate");
        indexes!.Count(doc => doc.GetValue("name").AsString == "name_unique").ShouldBe(1);
        var key = indexes!.Single(doc => doc.GetValue("name").AsString == "name_unique")
            .GetValue("key").AsBsonDocument;
        key.Names.ShouldContain("value");
        key.Names.ShouldNotContain("name");
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe("keep-me");
        persisted.Value.ShouldBe("v1");
        again!.ShouldBeSuccess();
        again!.Value.Steps.ShouldContain(step =>
            step.Name == "e1:index:name_unique" && step.Action == "skip");
        again.Value.Steps.ShouldNotContain(step =>
            step.Action == "recreate" || step.Action == "create" || step.Action == "remove");
    }

    [Test]
    public async Task Provisioner_RemovesIndex_WhenDroppedFromBinding_PreservesDocuments()
    {
        // Arrange
        const string collectionName = "provision-remove-index";
        const string databaseName = "ProvisionRemoveDB";
        var seedId = Guid.CreateVersion7();

        var (firstProvider, firstKey) = CreateHost(collectionName, databaseName: databaseName);
        Result<ProvisioningReport> firstApplied;
        using (firstProvider)
        using (var firstScope = firstProvider.CreateScope())
        {
            var factory = firstScope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
            var provisioner = firstScope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
            firstApplied = await provisioner.ApplyAsync();
            if (firstApplied.IsSuccess)
            {
                var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(firstKey)).Value;
                var seed = new GenericRepositoryTests.TestEntity1
                {
                    Id = seedId,
                    Name = "keep-me"
                };
                await collection.InsertOneAsync(seed);
            }
        }

        // Act
        Result<ProvisioningReport>? dry = null;
        Result<ProvisioningReport>? applied = null;
        IEnumerable<string>? indexNames = null;
        GenericRepositoryTests.TestEntity1? persisted = null;
        if (firstApplied.IsSuccess)
        {
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
                dry = await provisioner.DryRunAsync();
                applied = dry.IsSuccess
                    ? await provisioner.ApplyAsync()
                    : null;
                if (applied is { IsSuccess: true })
                {
                    var indexCursor = await collection.Indexes.ListAsync();
                    var indexes = await indexCursor.ToListAsync();
                    indexNames = indexes.Select(doc => doc.GetValue("name").AsString).ToList();
                    persisted = await collection.Find(x => x.Id == seedId).FirstOrDefaultAsync();
                }
            }
        }

        // Assert
        firstApplied.ShouldBeSuccess();
        dry!.ShouldBeSuccess();
        dry!.Value.Steps.ShouldContain(step =>
            step.Name == "e1:index:name_unique" && step.Action == "would-remove");
        applied!.ShouldBeSuccess();
        applied!.Value.Steps.ShouldContain(step =>
            step.Name == "e1:index:name_unique" && step.Action == "remove");
        indexNames!.ShouldBe(["_id_"]);
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe("keep-me");
    }

    [Test]
    public async Task Provisioner_RemovesTtlIndex_WhenRemovedFromBinding_PreservesDocuments()
    {
        // Arrange
        const string collectionName = "provision-remove-ttl";
        const string databaseName = "ProvisionRemoveTtlDB";
        var seedId = Guid.CreateVersion7();
        var ttlName = $"{collectionName}_ttl";

        var (firstProvider, firstKey) = CreateHost(
            collectionName,
            withUniqueNameIndex: false,
            withTtl: true,
            databaseName: databaseName);
        Result<ProvisioningReport> firstApplied;
        using (firstProvider)
        using (var firstScope = firstProvider.CreateScope())
        {
            var factory = firstScope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
            var provisioner = firstScope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
            firstApplied = await provisioner.ApplyAsync();
            if (firstApplied.IsSuccess)
            {
                var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(firstKey)).Value;
                var seed = new GenericRepositoryTests.TestEntity1
                {
                    Id = seedId,
                    Name = "ttl-keep",
                    CreatedAt = DateTime.UtcNow
                };
                await collection.InsertOneAsync(seed);
            }
        }

        // Act
        Result<ProvisioningReport>? dry = null;
        Result<ProvisioningReport>? applied = null;
        List<BsonDocument>? indexes = null;
        GenericRepositoryTests.TestEntity1? persisted = null;
        if (firstApplied.IsSuccess)
        {
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
                dry = await provisioner.DryRunAsync();
                applied = dry.IsSuccess
                    ? await provisioner.ApplyAsync()
                    : null;
                if (applied is { IsSuccess: true })
                {
                    var indexCursor = await collection.Indexes.ListAsync();
                    indexes = await indexCursor.ToListAsync();
                    persisted = await collection.Find(x => x.Id == seedId).FirstOrDefaultAsync();
                }
            }
        }

        // Assert
        firstApplied.ShouldBeSuccess();
        dry!.ShouldBeSuccess();
        dry!.Value.Steps.ShouldContain(step =>
            step.Name == $"e1:index:{ttlName}" && step.Action == "would-remove");
        applied!.ShouldBeSuccess();
        applied!.Value.Steps.ShouldContain(step =>
            step.Name == $"e1:index:{ttlName}" && step.Action == "remove");
        indexes!.ShouldNotContain(doc => doc.GetValue("name").AsString == ttlName);
        indexes!.ShouldContain(doc => doc.GetValue("name").AsString == "_id_");
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe("ttl-keep");
    }

    [Test]
    public async Task Provisioner_DryRun_ReportsSkipForExistingCollectionWithoutMutating()
    {
        // Arrange
        var collectionName = "provision-dry-existing";
        var (provider, bindingKey) = CreateHost(collectionName);
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
        var seed = new GenericRepositoryTests.TestEntity1 { Name = "seed" };
        await collection.InsertOneAsync(seed);
        var index = new CreateIndexModel<GenericRepositoryTests.TestEntity1>(
            Builders<GenericRepositoryTests.TestEntity1>.IndexKeys.Ascending(x => x.Name),
            new CreateIndexOptions { Unique = true, Name = "name_unique" });
        await collection.Indexes.CreateOneAsync(index);
        var beforeCursor = await collection.Indexes.ListAsync();
        var beforeIndexes = await beforeCursor.ToListAsync();
        var beforeNames = beforeIndexes.Select(doc => doc.GetValue("name").AsString).OrderBy(x => x).ToList();
        var beforeCount = await collection.CountDocumentsAsync(FilterDefinition<GenericRepositoryTests.TestEntity1>.Empty);

        // Act
        var dry = await provisioner.DryRunAsync();
        long? afterCount = null;
        List<string>? afterNames = null;
        if (dry.IsSuccess)
        {
            afterCount = await collection.CountDocumentsAsync(FilterDefinition<GenericRepositoryTests.TestEntity1>.Empty);
            var afterCursor = await collection.Indexes.ListAsync();
            var afterIndexes = await afterCursor.ToListAsync();
            afterNames = afterIndexes.Select(doc => doc.GetValue("name").AsString).OrderBy(x => x).ToList();
        }

        // Assert
        dry.ShouldBeSuccess();
        dry.Value.Applied.ShouldBeFalse();
        dry.Value.Steps.ShouldContain(step =>
            step.Name == "e1:collection" && step.Action == "skip");
        dry.Value.Steps.ShouldContain(step =>
            step.Name == "e1:index:name_unique" && step.Action == "skip");
        dry.Value.Steps.ShouldNotContain(step => step.Action == "create" || step.Action == "would-create");
        afterCount.ShouldBe(beforeCount);
        afterNames!.ShouldBe(beforeNames);
    }

    [Test]
    public async Task Provisioner_Apply_CreatesTtlIndex_AndSkipsOnSecondRun()
    {
        // Arrange
        var collectionName = "provision-ttl";
        var (provider, bindingKey) = CreateHost(collectionName, withUniqueNameIndex: false, withTtl: true);
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
        var seedId = Guid.CreateVersion7();
        var seed = new GenericRepositoryTests.TestEntity1
        {
            Id = seedId,
            Name = "ttl-seed",
            CreatedAt = DateTime.UtcNow
        };
        await collection.InsertOneAsync(seed);
        var ttlIndexName = $"{collectionName}_ttl";

        // Act
        var first = await provisioner.ApplyAsync();
        BsonDocument? ttl = null;
        GenericRepositoryTests.TestEntity1? persisted = null;
        Result<ProvisioningReport>? second = null;
        var ttlCount = 0;
        if (first.IsSuccess)
        {
            var indexCursor = await collection.Indexes.ListAsync();
            var indexes = await indexCursor.ToListAsync();
            ttl = indexes.SingleOrDefault(doc => doc.GetValue("name").AsString == ttlIndexName);
            persisted = await collection.Find(x => x.Id == seedId).FirstOrDefaultAsync();
            second = await provisioner.ApplyAsync();
            if (second.IsSuccess)
            {
                var secondCursor = await collection.Indexes.ListAsync();
                var secondIndexes = await secondCursor.ToListAsync();
                ttlCount = secondIndexes.Count(doc => doc.GetValue("name").AsString == ttlIndexName);
            }
        }

        // Assert
        first.ShouldBeSuccess();
        first.Value.Steps.ShouldContain(step =>
            step.Name == $"e1:index:{collectionName}_ttl" && step.Action == "create");
        ttl!.GetValue("expireAfterSeconds").ToInt64().ShouldBe((long)TimeSpan.FromHours(1).TotalSeconds);
        persisted.ShouldNotBeNull();
        second!.ShouldBeSuccess();
        second!.Value.Steps.ShouldContain(step =>
            step.Name == $"e1:index:{collectionName}_ttl" && step.Action == "skip");
        ttlCount.ShouldBe(1);
    }

    [Test]
    public async Task Provisioner_ExecutesCustomStep_AndIncludesItInReport()
    {
        // Arrange
        var steps = new IProvisioningStep[] { new RecordingProvisioningStep() };
        var (provider, _) = CreateHost(
            "provision-extra-step",
            withUniqueNameIndex: false,
            extraSteps: steps);
        using var scope = provider.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        // Act
        var dry = await provisioner.DryRunAsync();
        var applied = dry.IsSuccess
            ? await provisioner.ApplyAsync()
            : null;

        // Assert
        dry.ShouldBeSuccess();
        dry.Value.Steps.ShouldContain(step =>
            step.Name == RecordingProvisioningStep.StepName && step.Action == "would-apply");
        applied!.ShouldBeSuccess();
        applied!.Value.Steps.ShouldContain(step =>
            step.Name == RecordingProvisioningStep.StepName && step.Action == "applied");
    }

    [Test]
    public async Task Provisioner_CustomStepFailure_FailsClosedWithoutLedgerWrite()
    {
        // Arrange
        const string databaseName = "ProvisionFailDB";
        var steps = new IProvisioningStep[] { new FailingProvisioningStep() };
        var (provider, _) = CreateHost(
            "provision-extra-fail",
            withUniqueNameIndex: false,
            extraSteps: steps,
            databaseName: databaseName);
        using var scope = provider.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
        var databaseResolver = scope.ServiceProvider.GetRequiredService<IMongoDatabaseResolver>();

        // Act
        var applied = await provisioner.ApplyAsync();
        List<string>? ledgerNames = null;
        if (applied.IsFailed)
        {
            var databaseKey = new MongoDatabaseKey(databaseName);
            var database = (await databaseResolver.GetDatabaseAsync(databaseKey)).Value;
            var namesCursor = await database.ListCollectionNamesAsync();
            ledgerNames = await namesCursor.ToListAsync();
        }

        // Assert
        applied.ShouldBeFailure();
        applied.Errors.ShouldContain(error => error.Message.Contains("boom", StringComparison.Ordinal));
        ledgerNames!.ShouldNotContain("_dilcore_provisioning");
    }

    [Test]
    public async Task Provisioner_Apply_CollectionOnlyBinding_DoesNotCreateExtraIndexes()
    {
        // Arrange
        var (provider, bindingKey) = CreateHost("provision-collection-only", withUniqueNameIndex: false);
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        // Act
        var applied = await provisioner.ApplyAsync();
        List<string>? indexNames = null;
        if (applied.IsSuccess)
        {
            var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(bindingKey)).Value;
            var indexCursor = await collection.Indexes.ListAsync();
            var indexes = await indexCursor.ToListAsync();
            indexNames = indexes.Select(doc => doc.GetValue("name").AsString).ToList();
        }

        // Assert
        applied.ShouldBeSuccess();
        applied.Value.Steps.ShouldContain(step => step.Name == "e1:collection" && step.Action == "create");
        applied.Value.Steps.ShouldNotContain(step => step.Name.Contains(":index:", StringComparison.Ordinal));
        indexNames!.ShouldBe(["_id_"]);
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