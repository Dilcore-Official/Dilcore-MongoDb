using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.Internal;
using Dilcore.MongoDB.Repositories;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

[Category("M3Matrix")]
public class ProductionPolicyTests : BaseIntegrationTests
{
    [Test]
    public async Task GetPageAsync_ReturnsStableKeysetPages()
    {
        var repository = CreateRepository("pages", enableSoftDelete: false);
        for (var i = 0; i < 5; i++)
        {
            (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = $"n{i}" }))
                .ShouldBeSuccess();
        }

        var first = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
        {
            Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Id),
            PageSize = 2
        });
        first.ShouldBeSuccess();
        first.Value.Items.Count.ShouldBe(2);
        first.Value.HasMore.ShouldBeTrue();
        first.Value.NextCursor.ShouldNotBeNull();

        var second = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
        {
            Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Id),
            PageSize = 2,
            Cursor = first.Value.NextCursor
        });
        second.ShouldBeSuccess();
        second.Value.Items.Count.ShouldBe(2);
        second.Value.Items[0].Id.ShouldNotBe(first.Value.Items[0].Id);
        second.Value.Items[0].Id.ShouldNotBe(first.Value.Items[1].Id);
    }

    [Test]
    public async Task GetPageAsync_DescendingNonIdSort_HasNoRepeatsOrGaps()
    {
        var repository = CreateRepository("pages-desc", enableSoftDelete: false);
        foreach (var name in new[] { "a", "b", "c", "d", "e" })
        {
            (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = name })).ShouldBeSuccess();
        }

        var sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Descending(x => x.Name);
        var seen = new List<string>();
        string? cursor = null;
        for (var page = 0; page < 3; page++)
        {
            var result = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
                Sort = sort,
                PageSize = 2,
                Cursor = cursor
            });
            result.ShouldBeSuccess();
            seen.AddRange(result.Value.Items.Select(item => item.Name!));
            if (!result.Value.HasMore)
            {
                break;
            }

            cursor = result.Value.NextCursor;
        }

        seen.ShouldBe(["e", "d", "c", "b", "a"]);
    }

    [Test]
    public async Task GetPageAsync_CompoundSort_PagesWithoutOverlap()
    {
        var repository = CreateRepository("pages-compound", enableSoftDelete: false);
        foreach (var name in new[] { "a", "a", "b", "b", "c" })
        {
            (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = name })).ShouldBeSuccess();
        }

        var sort = Builders<GenericRepositoryTests.TestEntity1>.Sort
            .Ascending(x => x.Name)
            .Descending(x => x.Id);
        var first = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
        {
            Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            Sort = sort,
            PageSize = 2
        });
        first.ShouldBeSuccess();

        var second = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
        {
            Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            Sort = sort,
            PageSize = 2,
            Cursor = first.Value.NextCursor
        });
        second.ShouldBeSuccess();

        var ids = first.Value.Items.Select(x => x.Id)
            .Concat(second.Value.Items.Select(x => x.Id))
            .ToList();
        ids.Distinct().Count().ShouldBe(ids.Count);
    }

    [Test]
    public async Task GetPageAsync_TiedSortValues_UsesIdTieBreaker()
    {
        var repository = CreateRepository("pages-ties", enableSoftDelete: false);
        for (var i = 0; i < 6; i++)
        {
            (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "same" }))
                .ShouldBeSuccess();
        }

        var sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Name);
        var seen = new HashSet<Guid>();
        string? cursor = null;
        var hasMore = true;
        while (hasMore)
        {
            var page = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
                Sort = sort,
                PageSize = 2,
                Cursor = cursor
            });
            page.ShouldBeSuccess();
            foreach (var item in page.Value.Items)
            {
                seen.Add(item.Id).ShouldBeTrue();
            }

            hasMore = page.Value.HasMore;
            cursor = page.Value.NextCursor;
        }

        seen.Count.ShouldBe(6);
    }

    [Test]
    public async Task GetPageAsync_NullSortValues_AscendingAndDescending()
    {
        var repository = CreateRepository("pages-nulls", enableSoftDelete: false);
        (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = null })).ShouldBeSuccess();
        (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "b" })).ShouldBeSuccess();
        (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "a" })).ShouldBeSuccess();
        (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = null })).ShouldBeSuccess();

        var ascending = await CollectAllNames(
            repository,
            Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Name));
        ascending.Count.ShouldBe(4);
        ascending[0].ShouldBeNull();
        ascending[1].ShouldBeNull();
        ascending.Skip(2).ShouldBe(["a", "b"]);

        var descending = await CollectAllNames(
            repository,
            Builders<GenericRepositoryTests.TestEntity1>.Sort.Descending(x => x.Name));
        descending.Count.ShouldBe(4);
        descending.Take(2).ShouldBe(["b", "a"]);
        descending[2].ShouldBeNull();
        descending[3].ShouldBeNull();
    }

    [Test]
    public async Task GetPageAsync_CursorFromDifferentSort_Fails()
    {
        var repository = CreateRepository("pages-mismatch", enableSoftDelete: false);
        for (var i = 0; i < 3; i++)
        {
            (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = $"n{i}" }))
                .ShouldBeSuccess();
        }

        var first = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
        {
            Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Name),
            PageSize = 1
        });
        first.ShouldBeSuccess();

        var mismatched = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
        {
            Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Descending(x => x.Name),
            PageSize = 1,
            Cursor = first.Value.NextCursor
        });
        mismatched.ShouldBeFailure();
        mismatched.Errors[0].Message.ShouldContain("sort");
    }

    [Test]
    public async Task GetPageAsync_MalformedCursor_ReturnsFailedResult()
    {
        var repository = CreateRepository("pages-bad-cursor", enableSoftDelete: false);
        (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "x" })).ShouldBeSuccess();

        var result = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
        {
            Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Id),
            PageSize = 1,
            Cursor = "!"
        });

        result.ShouldBeFailure();
        result.Errors[0].Message.ShouldBe("Invalid cursor.");
    }

    [Test]
    public async Task GetPageAsync_PageSizeMaxValue_ReturnsFailedResult()
    {
        var repository = CreateRepository("pages-max-size", enableSoftDelete: false);

        var result = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
        {
            Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Id),
            PageSize = int.MaxValue
        });

        result.ShouldBeFailure();
        result.Errors[0].Message.ShouldContain("PageSize");
    }

    [Test]
    public async Task GetPageAsync_SoftDeletedDocuments_AreExcluded()
    {
        var repository = CreateRepository("pages-soft", enableSoftDelete: true);
        var keep = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "keep" });
        keep.ShouldBeSuccess();
        var gone = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "gone" });
        gone.ShouldBeSuccess();
        (await repository.DeleteAsync(gone.Value.Id, gone.Value.ETag)).ShouldBeSuccess();

        var page = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
        {
            Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Name),
            PageSize = 10
        });
        page.ShouldBeSuccess();
        page.Value.Items.Count.ShouldBe(1);
        page.Value.Items[0].Id.ShouldBe(keep.Value.Id);
    }

    [Test]
    public async Task GetPageAsync_Projection_ExcludesFieldsAndKeepsCursor()
    {
        var repository = CreateRepository("pages-proj", enableSoftDelete: false);
        foreach (var name in new[] { "a", "b", "c" })
        {
            (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1
            {
                Name = name,
                Value = $"v-{name}"
            })).ShouldBeSuccess();
        }

        var sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Name);
        var projection = Builders<GenericRepositoryTests.TestEntity1>.Projection.Exclude(x => x.Value);
        var first = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
        {
            Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            Sort = sort,
            PageSize = 2,
            Projection = projection
        });
        first.ShouldBeSuccess();
        foreach (var item in first.Value.Items)
        {
            item.Value.ShouldBeNull();
        }
        first.Value.NextCursor.ShouldNotBeNull();

        var second = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
        {
            Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            Sort = sort,
            PageSize = 2,
            Cursor = first.Value.NextCursor,
            Projection = projection
        });
        second.ShouldBeSuccess();
        second.Value.Items.Count.ShouldBe(1);
        second.Value.Items[0].Name.ShouldBe("c");
        second.Value.Items[0].Value.ShouldBeNull();
    }

    [Test]
    public async Task GetPageAsync_ProjectionExcludingSortKey_Fails()
    {
        var repository = CreateRepository("pages-proj-bad", enableSoftDelete: false);
        (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "a" })).ShouldBeSuccess();

        var result = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
        {
            Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Name),
            PageSize = 1,
            Projection = Builders<GenericRepositoryTests.TestEntity1>.Projection.Exclude(x => x.Name)
        });

        result.ShouldBeFailure();
        result.Errors[0].Message.ShouldContain("Name");
    }

    [Test]
    public async Task RestoreAndPurge_SoftDeletedDocuments()
    {
        var repository = CreateRepository("restore-purge", enableSoftDelete: true);
        var stored = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "gone" });
        stored.ShouldBeSuccess();
        (await repository.DeleteAsync(stored.Value.Id, stored.Value.ETag)).ShouldBeSuccess();

        var filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id);
        (await repository.GetAsync(filter)).ShouldBeFailure();

        (await repository.RestoreAsync(filter)).ShouldBeSuccess();
        (await repository.GetAsync(filter)).ShouldBeSuccess();

        (await repository.DeleteAsync(stored.Value.Id, (await repository.GetAsync(filter)).Value.ETag)).ShouldBeSuccess();
        var purged = await repository.PurgeAsync(filter);
        purged.ShouldBeSuccess();
        purged.Value.ShouldBe(1);
        (await repository.RestoreAsync(filter)).ShouldBeFailure();
    }

    [Test]
    public async Task RestoreAsync_WhenFilterMatchesActiveAndDeleted_RestoresOnlyDeleted()
    {
        var repository = CreateRepository("restore-select", enableSoftDelete: true);
        var active = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "shared" });
        active.ShouldBeSuccess();
        var deleted = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "shared" });
        deleted.ShouldBeSuccess();
        (await repository.DeleteAsync(deleted.Value.Id, deleted.Value.ETag)).ShouldBeSuccess();

        var nameFilter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Name, "shared");
        (await repository.RestoreAsync(nameFilter)).ShouldBeSuccess();

        var restored = await repository.GetAsync(
            Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, deleted.Value.Id));
        restored.ShouldBeSuccess();
        restored.Value.IsDeleted.ShouldBeFalse();

        var stillActive = await repository.GetAsync(
            Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, active.Value.Id));
        stillActive.ShouldBeSuccess();
        stillActive.Value.ETag.ShouldBe(active.Value.ETag);
        stillActive.Value.IsDeleted.ShouldBeFalse();
    }

    [Test]
    public async Task DuplicateKey_MapsToTypedError()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("DupDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<GenericRepositoryTests.TestEntity1>("e1", d => d
                    .WithCollectionName("dup-names")
                    .WithIndexes(new CreateIndexModel<GenericRepositoryTests.TestEntity1>(
                        Builders<GenericRepositoryTests.TestEntity1>.IndexKeys.Ascending(x => x.Name),
                        new CreateIndexOptions { Unique = true })));
            }));

        var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var repository = scope.ServiceProvider
            .GetRequiredService<IGenericRepository<GenericRepositoryTests.TestEntity1>>();

        (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(
            new Abstractions.Keys.MongoDocumentBindingKey("e1"))).ShouldBeSuccess();

        (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "unique-name" })).ShouldBeSuccess();
        var duplicate = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "unique-name" });
        duplicate.ShouldBeFailure();
        duplicate.ShouldHaveError<DuplicateKeyError>();
    }

    [Test]
    public async Task GetList_HonorsFindOptionsSortAndLimit()
    {
        var repository = CreateRepository("find-options", enableSoftDelete: false);
        foreach (var name in new[] { "c", "a", "b" })
        {
            (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = name })).ShouldBeSuccess();
        }

        var result = await repository.GetListAsync(
            Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            new FindOptions<GenericRepositoryTests.TestEntity1, GenericRepositoryTests.TestEntity1>
            {
                Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Name),
                Limit = 2
            });
        result.ShouldBeSuccess();
        result.Value.Count.ShouldBe(2);
        result.Value[0].Name.ShouldBe("a");
        result.Value[1].Name.ShouldBe("b");
    }

    [Test]
    public async Task GetList_HonorsFindOptionsProjection()
    {
        var repository = CreateRepository("find-options-proj", enableSoftDelete: false);
        (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1
        {
            Name = "visible",
            Value = "hidden"
        })).ShouldBeSuccess();

        var result = await repository.GetListAsync(
            Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            new FindOptions<GenericRepositoryTests.TestEntity1, GenericRepositoryTests.TestEntity1>
            {
                Projection = Builders<GenericRepositoryTests.TestEntity1>.Projection.Exclude(x => x.Value)
            });
        result.ShouldBeSuccess();
        result.Value.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("visible");
        result.Value[0].Value.ShouldBeNull();
    }

    [Test]
    public async Task GetListAsync_DerivedType_ReturnsOnlyDerivedDocuments()
    {
        var repository = CreateRepository("find-derived", enableSoftDelete: false);
        (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "base" }))
            .ShouldBeSuccess();
        (await repository.StoreAsync(new GenericRepositoryTests.DerivedTestEntity1
        {
            Name = "derived",
            SomeValue = "extra"
        })).ShouldBeSuccess();

        var derived = await repository.GetListAsync(
            Builders<GenericRepositoryTests.DerivedTestEntity1>.Filter.Empty);
        derived.ShouldBeSuccess();
        derived.Value.Count.ShouldBe(1);
        derived.Value[0].Name.ShouldBe("derived");
        derived.Value[0].SomeValue.ShouldBe("extra");
    }

    [Test]
    public async Task FindAsync_WithSession_ReturnsMatchingDocuments()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("PolicyDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<GenericRepositoryTests.TestEntity1>("e1", d =>
                    d.WithCollectionName("find-session"));
            }));

        var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider
            .GetRequiredService<IGenericRepository<GenericRepositoryTests.TestEntity1>>();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var client = scope.ServiceProvider.GetRequiredKeyedService<IMongoClient>("primary");

        (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "sessioned" }))
            .ShouldBeSuccess();

        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(
            new MongoDocumentBindingKey("e1"))).Value;

        using var session = await client.StartSessionAsync();
        using var cursor = await MongoCollectionCalls.FindAsync(
            collection,
            session,
            Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Name, "sessioned"),
            options: null,
            CancellationToken.None);
        var docs = await cursor.ToListAsync();

        docs.Count.ShouldBe(1);
        docs[0].Name.ShouldBe("sessioned");
    }

    private static async Task<List<string?>> CollectAllNames(
        IGenericRepository<GenericRepositoryTests.TestEntity1> repository,
        SortDefinition<GenericRepositoryTests.TestEntity1> sort)
    {
        var names = new List<string?>();
        string? cursor = null;
        var hasMore = true;
        while (hasMore)
        {
            var page = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
                Sort = sort,
                PageSize = 2,
                Cursor = cursor
            });
            page.ShouldBeSuccess();
            names.AddRange(page.Value.Items.Select(item => item.Name));
            hasMore = page.Value.HasMore;
            cursor = page.Value.NextCursor;
        }

        return names;
    }

    private IGenericRepository<GenericRepositoryTests.TestEntity1> CreateRepository(string collection, bool enableSoftDelete)
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("PolicyDB", db =>
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

        return AcceptanceServiceProviderFactory.Create(services)
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<IGenericRepository<GenericRepositoryTests.TestEntity1>>();
    }
}
