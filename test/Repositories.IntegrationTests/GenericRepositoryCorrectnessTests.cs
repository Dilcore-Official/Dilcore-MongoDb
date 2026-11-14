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
        // Arrange
        var (repository, _) = CreateRepository("soft-count", enableSoftDelete: true);
        var stored = await repository.StoreAsync(CreateEntity());
        var idFilter = stored.IsSuccess
            ? Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id)
            : null;

        // Act
        var countBefore = idFilter is not null ? await repository.CountAsync(idFilter) : null;
        var anyBefore = idFilter is not null ? await repository.HasAnyAsync(idFilter) : null;
        var deleted = stored.IsSuccess
            ? await repository.DeleteAsync(stored.Value.Id, stored.Value.ETag)
            : null;
        var countAfter = deleted is { IsSuccess: true } && idFilter is not null
            ? await repository.CountAsync(idFilter)
            : null;
        var anyAfter = deleted is { IsSuccess: true } && idFilter is not null
            ? await repository.HasAnyAsync(idFilter)
            : null;
        var listAfter = deleted is { IsSuccess: true } && idFilter is not null
            ? await repository.GetListAsync(idFilter)
            : null;
        var missing = deleted is { IsSuccess: true } && idFilter is not null
            ? await repository.GetAsync(idFilter)
            : null;

        // Assert
        stored.ShouldBeSuccess();
        countBefore!.Value.ShouldBe(1);
        anyBefore!.Value.ShouldBeTrue();
        deleted!.ShouldBeSuccess();
        countAfter!.Value.ShouldBe(0);
        anyAfter!.Value.ShouldBeFalse();
        listAfter!.Value.Count.ShouldBe(0);
        missing!.ShouldBeFailure();
        missing!.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task GetAsync_MissingDocument_ReturnsNotFound()
    {
        // Arrange
        var (repository, _) = CreateRepository("not-found", enableSoftDelete: false);
        var missingId = Guid.NewGuid();

        // Act
        var result = await repository.GetAsync(
            Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, missingId));

        // Assert
        result.ShouldBeFailure();
        result.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task FailedEtagUpdate_DoesNotMutatePolicyFields()
    {
        // Arrange
        var (repository, _) = CreateRepository("etag-restore", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        GenericRepositoryTests.TestEntity1? entity = null;
        var originalEtag = 0L;
        var originalUpdated = default(DateTime);
        string? originalName = null;
        if (stored.IsSuccess)
        {
            entity = stored.Value;
            originalEtag = entity.ETag;
            originalUpdated = entity.UpdatedAt;
            originalName = entity.Name;
            entity.Name = "mutated";
            entity.ETag = 123;
        }

        // Act
        var update = entity is not null
            ? await repository.StoreAsync(entity)
            : null;
        var persisted = update is { IsFailed: true }
            ? await repository.GetAsync(entity!.Id)
            : null;

        // Assert
        stored.ShouldBeSuccess();
        update!.ShouldBeFailure();
        update!.ShouldHaveError<ConcurrencyConflictError>();
        entity!.ETag.ShouldBe(123);
        entity.UpdatedAt.ShouldBe(originalUpdated);
        entity.Name.ShouldBe("mutated");
        persisted!.ShouldBeSuccess();
        persisted!.Value.ETag.ShouldBe(originalEtag);
        persisted.Value.Name.ShouldBe(originalName);
    }

    [Test]
    public async Task PatchAsync_DoesNotClobberUnrelatedFields()
    {
        // Arrange
        var (repository, factory) = CreateRepository("patch", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        if (stored.IsSuccess)
        {
            var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(
                new MongoDocumentBindingKey("e1"))).Value;
            var idFilter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id);
            var concurrentValue = Builders<GenericRepositoryTests.TestEntity1>.Update.Set(x => x.Value, "concurrent");
            await collection.UpdateOneAsync(idFilter, concurrentValue);
        }

        // Act
        var patched = stored.IsSuccess
            ? await repository.PatchAsync(
                Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id)
                    & Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.ETag, stored.Value.ETag),
                Builders<GenericRepositoryTests.TestEntity1>.Update.Set(x => x.Name, "from-client"))
            : null;

        // Assert
        stored.ShouldBeSuccess();
        patched!.ShouldBeSuccess();
        patched!.Value.Name.ShouldBe("from-client");
        patched.Value.Value.ShouldBe("concurrent");
        patched.Value.ETag.ShouldNotBe(stored.Value.ETag);
    }

    [Test]
    public async Task PatchAsync_StaleETag_ReturnsConcurrencyConflict()
    {
        // Arrange
        var (repository, _) = CreateRepository("patch-stale", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());

        // Act
        var patched = stored.IsSuccess
            ? await repository.PatchAsync(
                Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id)
                    & Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.ETag, 999),
                Builders<GenericRepositoryTests.TestEntity1>.Update.Set(x => x.Name, "nope"))
            : null;
        var loaded = patched is { IsFailed: true }
            ? await repository.GetAsync(stored.Value.Id)
            : null;

        // Assert
        stored.ShouldBeSuccess();
        patched!.ShouldBeFailure();
        patched!.ShouldHaveError<ConcurrencyConflictError>();
        loaded!.Value.Name.ShouldBe(stored.Value.Name);
        loaded.Value.ETag.ShouldBe(stored.Value.ETag);
    }

    [Test]
    public async Task PatchAsync_MissingDocumentWithoutConcurrency_ReturnsNotFound()
    {
        // Arrange
        var repository = CreateMinimalRepository("patch-min-missing");
        var missingId = Guid.NewGuid();

        // Act
        var patched = await repository.PatchAsync(
            Builders<GenericRepositoryTests.MinimalEntity>.Filter.Eq(x => x.Id, missingId),
            Builders<GenericRepositoryTests.MinimalEntity>.Update.Set(x => x.Name, "nope"));

        // Assert
        patched.ShouldBeFailure();
        patched.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task ReplaceAsync_ReplacesEntireDocument()
    {
        // Arrange
        var (repository, _) = CreateRepository("replace", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        if (stored.IsSuccess)
        {
            stored.Value.Name = "replaced";
            stored.Value.Value = null;
        }

        // Act
        var replaced = stored.IsSuccess
            ? await repository.ReplaceAsync(stored.Value)
            : null;
        var loaded = replaced is { IsSuccess: true }
            ? await repository.GetAsync(stored.Value.Id)
            : null;

        // Assert
        stored.ShouldBeSuccess();
        replaced!.ShouldBeSuccess();
        loaded!.Value.Name.ShouldBe("replaced");
        loaded.Value.Value.ShouldBeNull();
    }

    [Test]
    public async Task ReplaceAsync_StaleETag_ReturnsConcurrencyConflictAndRestoresPolicyFields()
    {
        // Arrange
        var (repository, _) = CreateRepository("replace-stale", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        var concurrent = stored.IsSuccess
            ? await repository.GetAsync(stored.Value.Id)
            : null;
        Result<GenericRepositoryTests.TestEntity1>? concurrentStore = null;
        if (concurrent is { IsSuccess: true })
        {
            concurrent.Value.Name = "from-other-writer";
            concurrentStore = await repository.StoreAsync(concurrent.Value);
        }

        var stale = stored.IsSuccess ? stored.Value : null;
        var staleEtag = stale?.ETag ?? 0;
        var staleUpdated = stale?.UpdatedAt ?? default;
        if (stale is not null && concurrentStore is { IsSuccess: true })
        {
            stale.Name = "stomped";
        }

        // Act
        var replaced = stale is not null && concurrentStore is { IsSuccess: true }
            ? await repository.ReplaceAsync(stale)
            : null;
        var loaded = replaced is { IsFailed: true }
            ? await repository.GetAsync(stored.Value.Id)
            : null;

        // Assert
        stored.ShouldBeSuccess();
        concurrentStore!.ShouldBeSuccess();
        replaced!.ShouldBeFailure();
        replaced!.ShouldHaveError<ConcurrencyConflictError>();
        stale!.ETag.ShouldBe(staleEtag);
        stale.UpdatedAt.ShouldBe(staleUpdated);
        loaded!.Value.Name.ShouldBe("from-other-writer");
        loaded.Value.ETag.ShouldNotBe(staleEtag);
    }

    [Test]
    public async Task ReplaceAsync_MissingDocument_ReturnsNotFound()
    {
        // Arrange
        var (repository, _) = CreateRepository("replace-missing", enableSoftDelete: false);
        var entity = CreateEntity();
        entity.Id = Guid.NewGuid();
        entity.ETag = 1;

        // Act
        var replaced = await repository.ReplaceAsync(entity);

        // Assert
        replaced.ShouldBeFailure();
        replaced.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task UpdateSnapshotAsync_PreservesUnknownFields_ReplaceDoesNot()
    {
        // Arrange
        var (repository, factory) = CreateRepository("snapshot-extra", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        var collection = stored.IsSuccess
            ? (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(
                new MongoDocumentBindingKey("e1"))).Value
            : null;
        if (collection is not null)
        {
            var idFilter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id);
            var extra = Builders<GenericRepositoryTests.TestEntity1>.Update.Set("extra", "keep-me");
            await collection.UpdateOneAsync(idFilter, extra);
            stored.Value.Name = "snapshot";
        }

        // Act
        var snapshot = stored.IsSuccess
            ? await repository.UpdateSnapshotAsync(stored.Value)
            : null;
        BsonDocument? afterSnapshot = null;
        if (snapshot is { IsSuccess: true } && collection is not null)
        {
            afterSnapshot = await collection.Find(
                    Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id))
                .As<BsonDocument>()
                .FirstAsync();
        }

        Result<GenericRepositoryTests.TestEntity1>? replaced = null;
        BsonDocument? afterReplace = null;
        if (afterSnapshot is not null)
        {
            stored.Value.Name = "replaced";
            replaced = await repository.ReplaceAsync(stored.Value);
        }

        if (replaced is { IsSuccess: true } && collection is not null)
        {
            afterReplace = await collection.Find(
                    Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id))
                .As<BsonDocument>()
                .FirstAsync();
        }

        // Assert
        stored.ShouldBeSuccess();
        snapshot!.ShouldBeSuccess();
        snapshot!.Value.ETag.ShouldNotBe(0);
        afterSnapshot!["extra"].AsString.ShouldBe("keep-me");
        afterSnapshot["name"].AsString.ShouldBe("snapshot");
        replaced!.ShouldBeSuccess();
        afterReplace!.Contains("extra").ShouldBeFalse();
    }

    [Test]
    public async Task UpdateSnapshotAsync_MissingDocument_ReturnsNotFound()
    {
        // Arrange
        var repository = CreateMinimalRepository("snapshot-missing");
        var missing = new GenericRepositoryTests.MinimalEntity { Id = Guid.NewGuid(), Name = "gone" };

        // Act
        var result = await repository.UpdateSnapshotAsync(missing);

        // Assert
        result.ShouldBeFailure();
        result.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task ReplaceAsync_MinimalMissingDocument_ReturnsNotFound()
    {
        // Arrange
        var repository = CreateMinimalRepository("replace-minimal-missing");
        var missing = new GenericRepositoryTests.MinimalEntity
        {
            Id = Guid.NewGuid(),
            Name = "gone"
        };

        // Act
        var replaced = await repository.ReplaceAsync(missing);

        // Assert
        replaced.ShouldBeFailure();
        replaced.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task ReplaceAsync_AuditableWithoutETag_MissingDocument_RestoresUpdatedAt()
    {
        // Arrange
        var repository = CreateAuditableNoEtagRepository("replace-audit-missing");
        var entity = new AuditableNoEtagEntity
        {
            Id = Guid.NewGuid(),
            Name = "gone",
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
        var previousUpdated = entity.UpdatedAt;

        // Act
        var replaced = await repository.ReplaceAsync(entity);

        // Assert
        replaced.ShouldBeFailure();
        replaced.ShouldHaveError<DocumentNotFoundError>();
        entity.UpdatedAt.ShouldBe(previousUpdated);
    }

    [Test]
    public async Task StoreAsync_CallerAssignedIdWithoutPolicies_Inserts()
    {
        // Arrange
        var repository = CreateMinimalRepository("assigned-id");
        var id = Guid.NewGuid();
        var entity = new GenericRepositoryTests.MinimalEntity
        {
            Id = id,
            Name = "preassigned"
        };

        // Act
        var stored = await repository.StoreAsync(entity);
        var loaded = stored.IsSuccess
            ? await repository.GetAsync(
                Builders<GenericRepositoryTests.MinimalEntity>.Filter.Eq(x => x.Id, id))
            : null;

        // Assert
        stored.ShouldBeSuccess();
        stored.Value.Id.ShouldBe(id);
        loaded!.ShouldBeSuccess();
        loaded!.Value.Name.ShouldBe("preassigned");
    }

    [Test]
    public async Task StoreAsync_OversizedDocument_ReturnsDocumentTooLargeAndRestoresPolicyFields()
    {
        // Arrange
        var (repository, _) = CreateRepository("too-large", enableSoftDelete: false);
        var entity = CreateEntity();
        entity.Value = new string('x', 17 * 1024 * 1024);
        var previousUpdated = entity.UpdatedAt;

        // Act
        var result = await repository.StoreAsync(entity);

        // Assert
        result.ShouldBeFailure();
        result.ShouldHaveError<DocumentTooLargeError>();
        entity.ETag.ShouldBe(0);
        entity.UpdatedAt.ShouldBe(previousUpdated);
    }

    [Test]
    public async Task StoreAsync_CanceledCreate_RestoresStagedIdAndPolicyFields()
    {
        // Arrange
        var (repository, _) = CreateRepository("cancel-create", enableSoftDelete: false);
        var entity = CreateEntity();
        var previousUpdated = entity.UpdatedAt;

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await repository.StoreAsync(entity, new CancellationToken(canceled: true)));

        // Assert
        entity.Id.ShouldBe(Guid.Empty);
        entity.ETag.ShouldBe(0);
        entity.UpdatedAt.ShouldBe(previousUpdated);
    }

    [Test]
    public async Task StoreAsync_CanceledUpdate_RestoresPolicyFields()
    {
        // Arrange
        var (repository, _) = CreateRepository("cancel-update", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        GenericRepositoryTests.TestEntity1? entity = null;
        var originalEtag = 0L;
        var originalUpdated = default(DateTime);
        if (stored.IsSuccess)
        {
            entity = stored.Value;
            originalEtag = entity.ETag;
            originalUpdated = entity.UpdatedAt;
            entity.Name = "canceled";
        }

        // Act & Assert
        if (entity is not null)
        {
            await Should.ThrowAsync<OperationCanceledException>(async () =>
                await repository.StoreAsync(entity, new CancellationToken(canceled: true)));
        }

        // Assert
        stored.ShouldBeSuccess();
        entity!.ETag.ShouldBe(originalEtag);
        entity.UpdatedAt.ShouldBe(originalUpdated);
    }

    [Test]
    public async Task StoreAsync_SoftDeletedDocument_ReturnsNotFound()
    {
        // Arrange
        var (repository, _) = CreateRepository("soft-write", enableSoftDelete: true);
        var stored = await repository.StoreAsync(CreateEntity());
        var deleted = stored.IsSuccess
            ? await repository.DeleteAsync(stored.Value.Id, stored.Value.ETag)
            : null;
        if (deleted is { IsSuccess: true })
        {
            stored.Value.Name = "resurrect";
        }

        // Act
        var update = deleted is { IsSuccess: true }
            ? await repository.StoreAsync(stored.Value)
            : null;

        // Assert
        stored.ShouldBeSuccess();
        deleted!.ShouldBeSuccess();
        update!.ShouldBeFailure();
        update!.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task DeleteAsync_WrongETag_ReturnsConcurrencyConflict()
    {
        // Arrange
        var (repository, _) = CreateRepository("delete-etag", enableSoftDelete: true);
        var stored = await repository.StoreAsync(CreateEntity());

        // Act
        var deleted = stored.IsSuccess
            ? await repository.DeleteAsync(stored.Value.Id, 999)
            : null;
        var loaded = deleted is { IsFailed: true }
            ? await repository.GetAsync(stored.Value.Id)
            : null;

        // Assert
        stored.ShouldBeSuccess();
        deleted!.ShouldBeFailure();
        deleted!.ShouldHaveError<ConcurrencyConflictError>();
        loaded!.ShouldBeSuccess();
        loaded!.Value.IsDeleted.ShouldBeFalse();
    }

    [Test]
    public async Task DeleteAsync_MissingDocument_ReturnsNotFound()
    {
        // Arrange
        var repository = CreateMinimalRepository("delete-missing");
        var missingId = Guid.NewGuid();

        // Act
        var deleted = await repository.DeleteAsync(
            Builders<GenericRepositoryTests.MinimalEntity>.Filter.Eq(x => x.Id, missingId));

        // Assert
        deleted.ShouldBeFailure();
        deleted.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task CustomBsonElementNames_HonorSerializationMappings()
    {
        // Arrange
        var (repository, factory) = CreateCustomMappedRepository("custom-fields");
        var stored = await repository.StoreAsync(new CustomMappedEntity { Name = "one" });

        // Act
        BsonDocument? raw = null;
        if (stored.IsSuccess)
        {
            var collection = (await factory.GetCollectionAsync<CustomMappedEntity>(
                new MongoDocumentBindingKey("custom"))).Value;
            raw = await collection.Find(Builders<CustomMappedEntity>.Filter.Eq(x => x.Id, stored.Value.Id))
                .As<BsonDocument>()
                .FirstAsync();
        }

        var previousEtag = stored.IsSuccess ? stored.Value.ETag : 0;
        if (stored.IsSuccess)
        {
            stored.Value.Name = "two";
        }

        var updated = stored.IsSuccess
            ? await repository.StoreAsync(stored.Value)
            : null;
        var deleted = updated is { IsSuccess: true }
            ? await repository.DeleteAsync(updated.Value.Id, updated.Value.ETag)
            : null;
        var missing = deleted is { IsSuccess: true }
            ? await repository.GetAsync(updated!.Value.Id)
            : null;

        // Assert
        stored.ShouldBeSuccess();
        stored.Value.ETag.ShouldNotBe(0);
        raw!.Contains("etag").ShouldBeTrue();
        raw.Contains("deleted").ShouldBeTrue();
        raw.Contains("updated").ShouldBeTrue();
        raw.Contains("eTag").ShouldBeFalse();
        updated!.ShouldBeSuccess();
        updated!.Value.ETag.ShouldNotBe(previousEtag);
        deleted!.ShouldBeSuccess();
        missing!.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task GetAsyncEnumerable_CollectionResolutionFailure_ThrowsCollectionResolutionException()
    {
        // Arrange
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

        // Act & Assert
        var ex = await Should.ThrowAsync<CollectionResolutionException>(enumerate());

        // Assert
        ex.Message.ShouldContain("Failed to get collection");
    }

    [Test]
    public async Task GetAsyncEnumerable_Derived_CollectionResolutionFailure_ThrowsCollectionResolutionException()
    {
        // Arrange
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

        // Act & Assert
        var ex = await Should.ThrowAsync<CollectionResolutionException>(enumerate());

        // Assert
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