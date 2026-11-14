using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Exceptions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

[Category("M3Matrix")]
public class GenericRepositoryCorrectnessTests : BaseIntegrationTests
{
    private static readonly Fixture Fixture = new();

    [Test]
    public async Task CountAndHasAny_ExcludeSoftDeletedDocuments()
    {
        var (repository, _) = CreateRepository("soft-count", enableSoftDelete: true);

        var stored = await repository.StoreAsync(CreateEntity());
        stored.ShouldBeSuccess();

        var idFilter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id);
        (await repository.CountAsync(idFilter)).Value.ShouldBe(1);
        (await repository.HasAnyAsync(idFilter)).Value.ShouldBeTrue();

        (await repository.DeleteAsync(stored.Value.Id, stored.Value.ETag)).ShouldBeSuccess();

        (await repository.CountAsync(idFilter)).Value.ShouldBe(0);
        (await repository.HasAnyAsync(idFilter)).Value.ShouldBeFalse();
        (await repository.GetListAsync(idFilter)).Value.Count.ShouldBe(0);
        var missing = await repository.GetAsync(idFilter);
        missing.ShouldBeFailure();
        missing.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task GetAsync_MissingDocument_ReturnsNotFound()
    {
        var (repository, _) = CreateRepository("not-found", enableSoftDelete: false);
        var result = await repository.GetAsync(
            Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, Guid.NewGuid()));
        result.ShouldBeFailure();
        result.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task FailedEtagUpdate_DoesNotMutatePolicyFields()
    {
        var (repository, _) = CreateRepository("etag-restore", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        stored.ShouldBeSuccess();

        var entity = stored.Value;
        var originalEtag = entity.ETag;
        var originalUpdated = entity.UpdatedAt;
        var originalName = entity.Name;
        entity.Name = "mutated";
        entity.ETag = 123;

        var update = await repository.StoreAsync(entity);
        update.ShouldBeFailure();
        update.ShouldHaveError<ConcurrencyConflictError>();
        entity.ETag.ShouldBe(123);
        entity.UpdatedAt.ShouldBe(originalUpdated);
        entity.Name.ShouldBe("mutated");

        var persisted = await repository.GetAsync(entity.Id);
        persisted.ShouldBeSuccess();
        persisted.Value.ETag.ShouldBe(originalEtag);
        persisted.Value.Name.ShouldBe(originalName);
    }

    [Test]
    public async Task PatchAsync_DoesNotClobberUnrelatedFields()
    {
        var (repository, factory) = CreateRepository("patch", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        stored.ShouldBeSuccess();

        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(
            new MongoDocumentBindingKey("e1"))).Value;
        await collection.UpdateOneAsync(
            Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id),
            Builders<GenericRepositoryTests.TestEntity1>.Update.Set(x => x.Value, "concurrent"));

        var patched = await repository.PatchAsync(
            Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id)
                & Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.ETag, stored.Value.ETag),
            Builders<GenericRepositoryTests.TestEntity1>.Update.Set(x => x.Name, "from-client"));
        patched.ShouldBeSuccess();
        patched.Value.Name.ShouldBe("from-client");
        patched.Value.Value.ShouldBe("concurrent");
        patched.Value.ETag.ShouldNotBe(stored.Value.ETag);
    }

    [Test]
    public async Task PatchAsync_StaleETag_ReturnsConcurrencyConflict()
    {
        var (repository, _) = CreateRepository("patch-stale", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        stored.ShouldBeSuccess();

        var patched = await repository.PatchAsync(
            Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id)
                & Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.ETag, 999),
            Builders<GenericRepositoryTests.TestEntity1>.Update.Set(x => x.Name, "nope"));
        patched.ShouldBeFailure();
        patched.ShouldHaveError<ConcurrencyConflictError>();

        var loaded = await repository.GetAsync(stored.Value.Id);
        loaded.Value.Name.ShouldBe(stored.Value.Name);
        loaded.Value.ETag.ShouldBe(stored.Value.ETag);
    }

    [Test]
    public async Task PatchAsync_MissingDocumentWithoutConcurrency_ReturnsNotFound()
    {
        var repository = CreateMinimalRepository("patch-min-missing");
        var patched = await repository.PatchAsync(
            Builders<GenericRepositoryTests.MinimalEntity>.Filter.Eq(x => x.Id, Guid.NewGuid()),
            Builders<GenericRepositoryTests.MinimalEntity>.Update.Set(x => x.Name, "nope"));
        patched.ShouldBeFailure();
        patched.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task ReplaceAsync_ReplacesEntireDocument()
    {
        var (repository, _) = CreateRepository("replace", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        stored.ShouldBeSuccess();
        stored.Value.Name = "replaced";
        stored.Value.Value = null;
        var replaced = await repository.ReplaceAsync(stored.Value);
        replaced.ShouldBeSuccess();
        var loaded = await repository.GetAsync(stored.Value.Id);
        loaded.Value.Name.ShouldBe("replaced");
        loaded.Value.Value.ShouldBeNull();
    }

    [Test]
    public async Task ReplaceAsync_StaleETag_ReturnsConcurrencyConflictAndRestoresPolicyFields()
    {
        var (repository, _) = CreateRepository("replace-stale", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        stored.ShouldBeSuccess();

        var concurrent = await repository.GetAsync(stored.Value.Id);
        concurrent.Value.Name = "from-other-writer";
        (await repository.StoreAsync(concurrent.Value)).ShouldBeSuccess();

        var stale = stored.Value;
        var staleEtag = stale.ETag;
        var staleUpdated = stale.UpdatedAt;
        stale.Name = "stomped";
        var replaced = await repository.ReplaceAsync(stale);
        replaced.ShouldBeFailure();
        replaced.ShouldHaveError<ConcurrencyConflictError>();
        stale.ETag.ShouldBe(staleEtag);
        stale.UpdatedAt.ShouldBe(staleUpdated);

        var loaded = await repository.GetAsync(stored.Value.Id);
        loaded.Value.Name.ShouldBe("from-other-writer");
        loaded.Value.ETag.ShouldNotBe(staleEtag);
    }

    [Test]
    public async Task ReplaceAsync_MissingDocument_ReturnsNotFound()
    {
        var (repository, _) = CreateRepository("replace-missing", enableSoftDelete: false);
        var entity = CreateEntity();
        entity.Id = Guid.NewGuid();
        entity.ETag = 1;
        var replaced = await repository.ReplaceAsync(entity);
        replaced.ShouldBeFailure();
        replaced.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task UpdateSnapshotAsync_PreservesUnknownFields_ReplaceDoesNot()
    {
        var (repository, factory) = CreateRepository("snapshot-extra", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        stored.ShouldBeSuccess();

        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(
            new MongoDocumentBindingKey("e1"))).Value;
        await collection.UpdateOneAsync(
            Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id),
            Builders<GenericRepositoryTests.TestEntity1>.Update.Set("extra", "keep-me"));

        stored.Value.Name = "snapshot";
        var snapshot = await repository.UpdateSnapshotAsync(stored.Value);
        snapshot.ShouldBeSuccess();
        snapshot.Value.ETag.ShouldNotBe(0);

        var afterSnapshot = await collection.Find(
                Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id))
            .As<BsonDocument>()
            .FirstAsync();
        afterSnapshot["extra"].AsString.ShouldBe("keep-me");
        afterSnapshot["name"].AsString.ShouldBe("snapshot");

        stored.Value.Name = "replaced";
        (await repository.ReplaceAsync(stored.Value)).ShouldBeSuccess();
        var afterReplace = await collection.Find(
                Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id))
            .As<BsonDocument>()
            .FirstAsync();
        afterReplace.Contains("extra").ShouldBeFalse();
    }

    [Test]
    public async Task UpdateSnapshotAsync_MissingDocument_ReturnsNotFound()
    {
        var repository = CreateMinimalRepository("snapshot-missing");
        var missing = new GenericRepositoryTests.MinimalEntity { Id = Guid.NewGuid(), Name = "gone" };
        var result = await repository.UpdateSnapshotAsync(missing);
        result.ShouldBeFailure();
        result.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task ReplaceAsync_MinimalMissingDocument_ReturnsNotFound()
    {
        var repository = CreateMinimalRepository("replace-minimal-missing");
        var missing = new GenericRepositoryTests.MinimalEntity
        {
            Id = Guid.NewGuid(),
            Name = "gone"
        };

        var replaced = await repository.ReplaceAsync(missing);

        replaced.ShouldBeFailure();
        replaced.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task ReplaceAsync_AuditableWithoutETag_MissingDocument_RestoresUpdatedAt()
    {
        var repository = CreateAuditableNoEtagRepository("replace-audit-missing");
        var entity = new AuditableNoEtagEntity
        {
            Id = Guid.NewGuid(),
            Name = "gone",
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
        var previousUpdated = entity.UpdatedAt;

        var replaced = await repository.ReplaceAsync(entity);

        replaced.ShouldBeFailure();
        replaced.ShouldHaveError<DocumentNotFoundError>();
        entity.UpdatedAt.ShouldBe(previousUpdated);
    }

    [Test]
    public async Task StoreAsync_CallerAssignedIdWithoutPolicies_Inserts()
    {
        var repository = CreateMinimalRepository("assigned-id");
        var id = Guid.NewGuid();
        var stored = await repository.StoreAsync(new GenericRepositoryTests.MinimalEntity
        {
            Id = id,
            Name = "preassigned"
        });
        stored.ShouldBeSuccess();
        stored.Value.Id.ShouldBe(id);

        var loaded = await repository.GetAsync(
            Builders<GenericRepositoryTests.MinimalEntity>.Filter.Eq(x => x.Id, id));
        loaded.ShouldBeSuccess();
        loaded.Value.Name.ShouldBe("preassigned");
    }

    [Test]
    public async Task StoreAsync_OversizedDocument_ReturnsDocumentTooLargeAndRestoresPolicyFields()
    {
        var (repository, _) = CreateRepository("too-large", enableSoftDelete: false);
        var entity = CreateEntity();
        entity.Value = new string('x', 17 * 1024 * 1024);
        var previousUpdated = entity.UpdatedAt;

        var result = await repository.StoreAsync(entity);
        result.ShouldBeFailure();
        result.ShouldHaveError<DocumentTooLargeError>();
        entity.ETag.ShouldBe(0);
        entity.UpdatedAt.ShouldBe(previousUpdated);
    }

    [Test]
    public async Task StoreAsync_CanceledCreate_RestoresStagedIdAndPolicyFields()
    {
        var (repository, _) = CreateRepository("cancel-create", enableSoftDelete: false);
        var entity = CreateEntity();
        var previousUpdated = entity.UpdatedAt;

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await repository.StoreAsync(entity, new CancellationToken(canceled: true)));

        entity.Id.ShouldBe(Guid.Empty);
        entity.ETag.ShouldBe(0);
        entity.UpdatedAt.ShouldBe(previousUpdated);
    }

    [Test]
    public async Task StoreAsync_CanceledUpdate_RestoresPolicyFields()
    {
        var (repository, _) = CreateRepository("cancel-update", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        stored.ShouldBeSuccess();

        var entity = stored.Value;
        var originalEtag = entity.ETag;
        var originalUpdated = entity.UpdatedAt;
        entity.Name = "canceled";

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await repository.StoreAsync(entity, new CancellationToken(canceled: true)));

        entity.ETag.ShouldBe(originalEtag);
        entity.UpdatedAt.ShouldBe(originalUpdated);
    }

    [Test]
    public async Task StoreAsync_SoftDeletedDocument_ReturnsNotFound()
    {
        var (repository, _) = CreateRepository("soft-write", enableSoftDelete: true);
        var stored = await repository.StoreAsync(CreateEntity());
        stored.ShouldBeSuccess();
        (await repository.DeleteAsync(stored.Value.Id, stored.Value.ETag)).ShouldBeSuccess();

        stored.Value.Name = "resurrect";
        var update = await repository.StoreAsync(stored.Value);
        update.ShouldBeFailure();
        update.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task DeleteAsync_WrongETag_ReturnsConcurrencyConflict()
    {
        var (repository, _) = CreateRepository("delete-etag", enableSoftDelete: true);
        var stored = await repository.StoreAsync(CreateEntity());
        stored.ShouldBeSuccess();

        var deleted = await repository.DeleteAsync(stored.Value.Id, 999);
        deleted.ShouldBeFailure();
        deleted.ShouldHaveError<ConcurrencyConflictError>();

        var loaded = await repository.GetAsync(stored.Value.Id);
        loaded.ShouldBeSuccess();
        loaded.Value.IsDeleted.ShouldBeFalse();
    }

    [Test]
    public async Task DeleteAsync_MissingDocument_ReturnsNotFound()
    {
        var repository = CreateMinimalRepository("delete-missing");
        var deleted = await repository.DeleteAsync(
            Builders<GenericRepositoryTests.MinimalEntity>.Filter.Eq(x => x.Id, Guid.NewGuid()));
        deleted.ShouldBeFailure();
        deleted.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task CustomBsonElementNames_HonorSerializationMappings()
    {
        var (repository, factory) = CreateCustomMappedRepository("custom-fields");
        var stored = await repository.StoreAsync(new CustomMappedEntity { Name = "one" });
        stored.ShouldBeSuccess();
        stored.Value.ETag.ShouldNotBe(0);

        var collection = (await factory.GetCollectionAsync<CustomMappedEntity>(
            new MongoDocumentBindingKey("custom"))).Value;
        var raw = await collection.Find(Builders<CustomMappedEntity>.Filter.Eq(x => x.Id, stored.Value.Id))
            .As<BsonDocument>()
            .FirstAsync();
        raw.Contains("etag").ShouldBeTrue();
        raw.Contains("deleted").ShouldBeTrue();
        raw.Contains("updated").ShouldBeTrue();
        raw.Contains("eTag").ShouldBeFalse();

        stored.Value.Name = "two";
        var previousEtag = stored.Value.ETag;
        var updated = await repository.StoreAsync(stored.Value);
        updated.ShouldBeSuccess();
        updated.Value.ETag.ShouldNotBe(previousEtag);

        var deleted = await repository.DeleteAsync(updated.Value.Id, updated.Value.ETag);
        deleted.ShouldBeSuccess();
        (await repository.GetAsync(updated.Value.Id)).ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task GetAsyncEnumerable_CollectionResolutionFailure_ThrowsCollectionResolutionException()
    {
        var services = new ServiceCollection();
        services.AddScoped<INamespaceSegmentContributor>(_ => new RequiredPrefixContributor());
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("StreamFailDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<GenericRepositoryTests.MinimalEntity>("stream", d => d
                    .WithCollectionName("streamEntities"));
            }));

        using var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider
            .GetRequiredService<IGenericRepository<GenericRepositoryTests.MinimalEntity>>();

        var enumerate = async () =>
        {
            await foreach (var _ in repository.GetAsyncEnumerable(
                Builders<GenericRepositoryTests.MinimalEntity>.Filter.Empty))
            {
                // Expect CollectionResolutionException before any yield.
            }
        };

        var ex = await Should.ThrowAsync<CollectionResolutionException>(enumerate());
        ex.Message.ShouldContain("Failed to get collection");
    }

    [Test]
    public async Task GetAsyncEnumerable_Derived_CollectionResolutionFailure_ThrowsCollectionResolutionException()
    {
        var services = new ServiceCollection();
        services.AddScoped<INamespaceSegmentContributor>(_ => new RequiredPrefixContributor());
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("StreamDerivedFailDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<GenericRepositoryTests.TestEntity1>("stream-derived", d => d
                    .WithCollectionName("streamDerivedEntities"));
            }));

        using var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider
            .GetRequiredService<IGenericRepository<GenericRepositoryTests.TestEntity1>>();

        var enumerate = async () =>
        {
            await foreach (var _ in repository.GetAsyncEnumerable<GenericRepositoryTests.DerivedTestEntity1>(
                Builders<GenericRepositoryTests.DerivedTestEntity1>.Filter.Empty))
            {
                // Expect CollectionResolutionException before any yield.
            }
        };

        var ex = await Should.ThrowAsync<CollectionResolutionException>(enumerate());
        ex.Message.ShouldContain("Failed to get collection");
    }

    private (IGenericRepository<GenericRepositoryTests.TestEntity1> Repository, IMongoDbCollectionFactory Factory)
        CreateRepository(string collection, bool enableSoftDelete)
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("CorrectnessDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<GenericRepositoryTests.TestEntity1>("e1", d =>
                {
                    d.WithCollectionName(collection);
                    if (enableSoftDelete)
                    {
                        d.WithSoftDelete();
                    }
                });
            }));

        var provider = AcceptanceServiceProviderFactory.Create(services);
        var scope = provider.CreateScope();
        return (
            scope.ServiceProvider.GetRequiredService<IGenericRepository<GenericRepositoryTests.TestEntity1>>(),
            scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>());
    }

    private IGenericRepository<GenericRepositoryTests.MinimalEntity> CreateMinimalRepository(string collection)
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("MinimalCorrectnessDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<GenericRepositoryTests.MinimalEntity>("minimal", d => d
                    .WithCollectionName(collection));
            }));

        return AcceptanceServiceProviderFactory.Create(services)
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<IGenericRepository<GenericRepositoryTests.MinimalEntity>>();
    }

    private IGenericRepository<AuditableNoEtagEntity> CreateAuditableNoEtagRepository(string collection)
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("AuditableNoEtagDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<AuditableNoEtagEntity>("audit-no-etag", d => d
                    .WithCollectionName(collection));
            }));

        return AcceptanceServiceProviderFactory.Create(services)
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<IGenericRepository<AuditableNoEtagEntity>>();
    }

    private (IGenericRepository<CustomMappedEntity> Repository, IMongoDbCollectionFactory Factory)
        CreateCustomMappedRepository(string collection)
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("CustomMappedDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<CustomMappedEntity>("custom", d => d
                    .WithCollectionName(collection)
                    .WithSoftDelete());
            }));

        var provider = AcceptanceServiceProviderFactory.Create(services);
        var scope = provider.CreateScope();
        return (
            scope.ServiceProvider.GetRequiredService<IGenericRepository<CustomMappedEntity>>(),
            scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>());
    }

    private static GenericRepositoryTests.TestEntity1 CreateEntity()
        => Fixture.Build<GenericRepositoryTests.TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();

    public sealed class CustomMappedEntity : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
    {
        public Guid Id { get; set; }

        [BsonElement("etag")]
        public long ETag { get; set; }

        [BsonElement("deleted")]
        public bool IsDeleted { get; set; }

        [BsonElement("created")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updated")]
        public DateTime UpdatedAt { get; set; }

        public string? Name { get; set; }
    }

    public sealed class AuditableNoEtagEntity : IDocumentEntity<Guid>, IAuditableDocument
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Name { get; set; }
    }

    private sealed class RequiredPrefixContributor : INamespaceSegmentContributor
    {
        public int Order => 50;

        public Task<Result<string?>> ContributeAsync(
            NamespaceResolutionRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Fail<string?>("A required namespace prefix is missing from the current request context."));
    }
}