using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Provisioning;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.Internal;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

[Category("M3Matrix")]
public class ProductionPolicyTests : BaseIntegrationTests
{
    [Test]
    public async Task GetPageAsync_ReturnsStableKeysetPages()
    {
        // Arrange
        var repository = CreateRepository("pages", enableSoftDelete: false);
        var stored = new List<Result<GenericRepositoryTests.TestEntity1>>();
        for (var i = 0; i < 5; i++)
        {
            stored.Add(await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = $"n{i}" }));
        }

        var sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Id);
        var filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty;
        var ready = stored.TrueForAll(static item => item.IsSuccess);

        // Act
        var first = ready
            ? await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = filter,
                Sort = sort,
                PageSize = 2
            })
            : null;
        var second = first is { IsSuccess: true } && first.Value.NextCursor is not null
            ? await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = filter,
                Sort = sort,
                PageSize = 2,
                Cursor = first.Value.NextCursor
            })
            : null;

        // Assert
        foreach (var item in stored)
        {
            item.ShouldBeSuccess();
        }

        first!.ShouldBeSuccess();
        first!.Value.Items.Count.ShouldBe(2);
        first.Value.HasMore.ShouldBeTrue();
        first.Value.NextCursor.ShouldNotBeNull();
        second!.ShouldBeSuccess();
        second!.Value.Items.Count.ShouldBe(2);
        second.Value.Items[0].Id.ShouldNotBe(first.Value.Items[0].Id);
        second.Value.Items[0].Id.ShouldNotBe(first.Value.Items[1].Id);
    }

    [Test]
    public async Task GetPageAsync_DescendingNonIdSort_HasNoRepeatsOrGaps()
    {
        // Arrange
        var repository = CreateRepository("pages-desc", enableSoftDelete: false);
        var names = new[] { "a", "b", "c", "d", "e" };
        var stored = new List<Result<GenericRepositoryTests.TestEntity1>>();
        foreach (var name in names)
        {
            stored.Add(await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = name }));
        }

        var sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Descending(x => x.Name);
        var filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty;
        var seen = new List<string>();
        var pages = new List<Result<KeysetPage<GenericRepositoryTests.TestEntity1>>>();
        string? cursor = null;
        var ready = stored.TrueForAll(static item => item.IsSuccess);

        // Act
        for (var page = 0; ready && page < 3; page++)
        {
            var result = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = filter,
                Sort = sort,
                PageSize = 2,
                Cursor = cursor
            });
            pages.Add(result);
            if (!result.IsSuccess)
            {
                break;
            }

            var pageNames = result.Value.Items.Select(item => item.Name!);
            seen.AddRange(pageNames);
            if (!result.Value.HasMore)
            {
                break;
            }

            cursor = result.Value.NextCursor;
        }

        // Assert
        foreach (var item in stored)
        {
            item.ShouldBeSuccess();
        }

        foreach (var page in pages)
        {
            page.ShouldBeSuccess();
        }

        seen.ShouldBe(["e", "d", "c", "b", "a"]);
    }

    [Test]
    public async Task GetPageAsync_CompoundSort_PagesWithoutOverlap()
    {
        // Arrange
        var repository = CreateRepository("pages-compound", enableSoftDelete: false);
        var names = new[] { "a", "a", "b", "b", "c" };
        var stored = new List<Result<GenericRepositoryTests.TestEntity1>>();
        foreach (var name in names)
        {
            stored.Add(await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = name }));
        }

        var sort = Builders<GenericRepositoryTests.TestEntity1>.Sort
            .Ascending(x => x.Name)
            .Descending(x => x.Id);
        var filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty;
        var ready = stored.TrueForAll(static item => item.IsSuccess);

        // Act
        var first = ready
            ? await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = filter,
                Sort = sort,
                PageSize = 2
            })
            : null;
        var second = first is { IsSuccess: true }
            ? await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = filter,
                Sort = sort,
                PageSize = 2,
                Cursor = first.Value.NextCursor
            })
            : null;
        var ids = first is { IsSuccess: true } && second is { IsSuccess: true }
            ? first.Value.Items.Select(x => x.Id)
                .Concat(second.Value.Items.Select(x => x.Id))
                .ToList()
            : null;

        // Assert
        foreach (var item in stored)
        {
            item.ShouldBeSuccess();
        }

        first!.ShouldBeSuccess();
        second!.ShouldBeSuccess();
        var pageIds = ids!;
        pageIds.Distinct().Count().ShouldBe(pageIds.Count);
    }

    [Test]
    public async Task GetPageAsync_TiedSortValues_UsesIdTieBreaker()
    {
        // Arrange
        var repository = CreateRepository("pages-ties", enableSoftDelete: false);
        var stored = new List<Result<GenericRepositoryTests.TestEntity1>>();
        for (var i = 0; i < 6; i++)
        {
            stored.Add(await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "same" }));
        }

        var sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Name);
        var filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty;
        var seen = new HashSet<Guid>();
        var additions = new List<bool>();
        var pages = new List<Result<KeysetPage<GenericRepositoryTests.TestEntity1>>>();
        string? cursor = null;
        var hasMore = true;
        var ready = stored.TrueForAll(static item => item.IsSuccess);

        // Act
        while (ready && hasMore)
        {
            var page = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = filter,
                Sort = sort,
                PageSize = 2,
                Cursor = cursor
            });
            pages.Add(page);
            if (!page.IsSuccess)
            {
                break;
            }

            var added = page.Value.Items.Select(item => seen.Add(item.Id)).ToList();
            additions.AddRange(added);
            hasMore = page.Value.HasMore;
            cursor = page.Value.NextCursor;
        }

        // Assert
        foreach (var item in stored)
        {
            item.ShouldBeSuccess();
        }

        foreach (var page in pages)
        {
            page.ShouldBeSuccess();
        }

        foreach (var added in additions)
        {
            added.ShouldBeTrue();
        }

        seen.Count.ShouldBe(6);
    }

    [Test]
    public async Task GetPageAsync_NullSortValues_AscendingAndDescending()
    {
        // Arrange
        var repository = CreateRepository("pages-nulls", enableSoftDelete: false);
        var stored = new List<Result<GenericRepositoryTests.TestEntity1>>
        {
            await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = null }),
            await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "b" }),
            await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "a" }),
            await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = null })
        };
        var ascendingSort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Name);
        var descendingSort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Descending(x => x.Name);
        var ready = stored.TrueForAll(static item => item.IsSuccess);

        // Act
        var ascending = ready
            ? await CollectAllNames(repository, ascendingSort)
            : null;
        var descending = ascending is not null
            ? await CollectAllNames(repository, descendingSort)
            : null;

        // Assert
        foreach (var item in stored)
        {
            item.ShouldBeSuccess();
        }

        ascending!.Count.ShouldBe(4);
        ascending[0].ShouldBeNull();
        ascending[1].ShouldBeNull();
        ascending.Skip(2).ShouldBe(["a", "b"]);
        descending!.Count.ShouldBe(4);
        descending.Take(2).ShouldBe(["b", "a"]);
        descending[2].ShouldBeNull();
        descending[3].ShouldBeNull();
    }

    [Test]
    public async Task GetPageAsync_CursorFromDifferentSort_Fails()
    {
        // Arrange
        var repository = CreateRepository("pages-mismatch", enableSoftDelete: false);
        var stored = new List<Result<GenericRepositoryTests.TestEntity1>>();
        for (var i = 0; i < 3; i++)
        {
            stored.Add(await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = $"n{i}" }));
        }

        var filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty;
        var ready = stored.TrueForAll(static item => item.IsSuccess);

        // Act
        var first = ready
            ? await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = filter,
                Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Name),
                PageSize = 1
            })
            : null;
        var mismatched = first is { IsSuccess: true }
            ? await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = filter,
                Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Descending(x => x.Name),
                PageSize = 1,
                Cursor = first.Value.NextCursor
            })
            : null;

        // Assert
        foreach (var item in stored)
        {
            item.ShouldBeSuccess();
        }

        first!.ShouldBeSuccess();
        mismatched!.ShouldBeFailure();
        mismatched!.Errors[0].Message.ShouldContain("sort");
    }

    [Test]
    public async Task GetPageAsync_MalformedCursor_ReturnsFailedResult()
    {
        // Arrange
        var repository = CreateRepository("pages-bad-cursor", enableSoftDelete: false);
        var stored = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "x" });

        // Act
        var result = stored.IsSuccess
            ? await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
                Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Id),
                PageSize = 1,
                Cursor = "!"
            })
            : null;

        // Assert
        stored.ShouldBeSuccess();
        result!.ShouldBeFailure();
        result!.Errors[0].Message.ShouldBe("Invalid cursor.");
    }

    [Test]
    public async Task GetPageAsync_PageSizeMaxValue_ReturnsFailedResult()
    {
        // Arrange
        var repository = CreateRepository("pages-max-size", enableSoftDelete: false);
        var request = new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
        {
            Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Id),
            PageSize = int.MaxValue
        };

        // Act
        var result = await repository.GetPageAsync(request);

        // Assert
        result.ShouldBeFailure();
        result.Errors[0].Message.ShouldContain("PageSize");
    }

    [Test]
    public async Task GetPageAsync_SoftDeletedDocuments_AreExcluded()
    {
        // Arrange
        var repository = CreateRepository("pages-soft", enableSoftDelete: true);
        var keep = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "keep" });
        var gone = keep.IsSuccess
            ? await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "gone" })
            : null;
        var deleted = gone is { IsSuccess: true }
            ? await repository.DeleteAsync(gone.Value.Id, gone.Value.ETag)
            : null;

        // Act
        var page = deleted is { IsSuccess: true }
            ? await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
                Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Name),
                PageSize = 10
            })
            : null;

        // Assert
        keep.ShouldBeSuccess();
        gone!.ShouldBeSuccess();
        deleted!.ShouldBeSuccess();
        page!.ShouldBeSuccess();
        page!.Value.Items.Count.ShouldBe(1);
        page.Value.Items[0].Id.ShouldBe(keep.Value.Id);
    }

    [Test]
    public async Task GetPageAsync_Projection_ExcludesFieldsAndKeepsCursor()
    {
        // Arrange
        var repository = CreateRepository("pages-proj", enableSoftDelete: false);
        var names = new[] { "a", "b", "c" };
        var stored = new List<Result<GenericRepositoryTests.TestEntity1>>();
        foreach (var name in names)
        {
            stored.Add(await repository.StoreAsync(new GenericRepositoryTests.TestEntity1
            {
                Name = name,
                Value = $"v-{name}"
            }));
        }

        var sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Name);
        var projection = Builders<GenericRepositoryTests.TestEntity1>.Projection.Exclude(x => x.Value);
        var filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty;
        var ready = stored.TrueForAll(static item => item.IsSuccess);

        // Act
        var first = ready
            ? await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = filter,
                Sort = sort,
                PageSize = 2,
                Projection = projection
            })
            : null;
        var second = first is { IsSuccess: true } && first.Value.NextCursor is not null
            ? await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = filter,
                Sort = sort,
                PageSize = 2,
                Cursor = first.Value.NextCursor,
                Projection = projection
            })
            : null;

        // Assert
        foreach (var item in stored)
        {
            item.ShouldBeSuccess();
        }

        first!.ShouldBeSuccess();
        foreach (var item in first!.Value.Items)
        {
            item.Value.ShouldBeNull();
        }

        first.Value.NextCursor.ShouldNotBeNull();
        second!.ShouldBeSuccess();
        second!.Value.Items.Count.ShouldBe(1);
        second.Value.Items[0].Name.ShouldBe("c");
        second.Value.Items[0].Value.ShouldBeNull();
    }

    [Test]
    public async Task GetPageAsync_ProjectionExcludingSortKey_Fails()
    {
        // Arrange
        var repository = CreateRepository("pages-proj-bad", enableSoftDelete: false);
        var stored = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "a" });

        // Act
        var result = stored.IsSuccess
            ? await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
            {
                Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
                Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Name),
                PageSize = 1,
                Projection = Builders<GenericRepositoryTests.TestEntity1>.Projection.Exclude(x => x.Name)
            })
            : null;

        // Assert
        stored.ShouldBeSuccess();
        result!.ShouldBeFailure();
        result!.Errors[0].Message.ShouldContain("Name");
    }

    [Test]
    public async Task RestoreAndPurge_SoftDeletedDocuments()
    {
        // Arrange
        var repository = CreateRepository("restore-purge", enableSoftDelete: true);
        var stored = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "gone" });
        var deleted = stored.IsSuccess
            ? await repository.DeleteAsync(stored.Value.Id, stored.Value.ETag)
            : null;
        var filter = stored.IsSuccess
            ? Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id)
            : null;

        // Act
        var hidden = deleted is { IsSuccess: true } && filter is not null
            ? await repository.GetAsync(filter)
            : null;
        var restored = hidden is { IsFailed: true } && filter is not null
            ? await repository.RestoreAsync(filter)
            : null;
        var visible = restored is { IsSuccess: true } && filter is not null
            ? await repository.GetAsync(filter)
            : null;
        var deletedAgain = visible is { IsSuccess: true }
            ? await repository.DeleteAsync(stored.Value.Id, visible.Value.ETag)
            : null;
        var purged = deletedAgain is { IsSuccess: true } && filter is not null
            ? await repository.PurgeAsync(filter)
            : null;
        var restoreAfterPurge = purged is { IsSuccess: true } && filter is not null
            ? await repository.RestoreAsync(filter)
            : null;

        // Assert
        stored.ShouldBeSuccess();
        deleted!.ShouldBeSuccess();
        hidden!.ShouldBeFailure();
        restored!.ShouldBeSuccess();
        visible!.ShouldBeSuccess();
        deletedAgain!.ShouldBeSuccess();
        purged!.ShouldBeSuccess();
        purged!.Value.ShouldBe(1);
        restoreAfterPurge!.ShouldBeFailure();
    }

    [Test]
    public async Task RestoreAsync_WhenFilterMatchesActiveAndDeleted_RestoresOnlyDeleted()
    {
        // Arrange
        var repository = CreateRepository("restore-select", enableSoftDelete: true);
        var active = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "shared" });
        var deleted = active.IsSuccess
            ? await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "shared" })
            : null;
        var removed = deleted is { IsSuccess: true }
            ? await repository.DeleteAsync(deleted.Value.Id, deleted.Value.ETag)
            : null;
        var nameFilter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Name, "shared");

        // Act
        var restore = removed is { IsSuccess: true }
            ? await repository.RestoreAsync(nameFilter)
            : null;
        var restored = restore is { IsSuccess: true }
            ? await repository.GetAsync(
                Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, deleted!.Value.Id))
            : null;
        var stillActive = restored is { IsSuccess: true }
            ? await repository.GetAsync(
                Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, active.Value.Id))
            : null;

        // Assert
        active.ShouldBeSuccess();
        deleted!.ShouldBeSuccess();
        removed!.ShouldBeSuccess();
        restore!.ShouldBeSuccess();
        restored!.ShouldBeSuccess();
        restored!.Value.IsDeleted.ShouldBeFalse();
        stillActive!.ShouldBeSuccess();
        stillActive!.Value.ETag.ShouldBe(active.Value.ETag);
        stillActive.Value.IsDeleted.ShouldBeFalse();
    }

    [Test]
    public async Task DuplicateKey_MapsToTypedError()
    {
        // Arrange
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
                        new CreateIndexOptions { Unique = true, Name = "dup_names_name_unique" })));
            }));

        var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
        var repository = scope.ServiceProvider
            .GetRequiredService<IGenericRepository<GenericRepositoryTests.TestEntity1>>();

        // Indexes are applied by the provisioner, not on collection resolution.
        var provisioned = await provisioner.ApplyAsync();
        var first = provisioned.IsSuccess
            ? await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "unique-name" })
            : null;

        // Act
        var duplicate = first is { IsSuccess: true }
            ? await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "unique-name" })
            : null;

        // Assert
        provisioned.ShouldBeSuccess();
        first!.ShouldBeSuccess();
        duplicate!.ShouldBeFailure();
        duplicate!.ShouldHaveError<DuplicateKeyError>();
    }

    [Test]
    public async Task GetList_HonorsFindOptionsSortAndLimit()
    {
        // Arrange
        var repository = CreateRepository("find-options", enableSoftDelete: false);
        var names = new[] { "c", "a", "b" };
        var stored = new List<Result<GenericRepositoryTests.TestEntity1>>();
        foreach (var name in names)
        {
            stored.Add(await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = name }));
        }

        var filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty;
        var options = new FindOptions<GenericRepositoryTests.TestEntity1, GenericRepositoryTests.TestEntity1>
        {
            Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Name),
            Limit = 2
        };
        var ready = stored.TrueForAll(static item => item.IsSuccess);

        // Act
        var result = ready
            ? await repository.GetListAsync(filter, options)
            : null;

        // Assert
        foreach (var item in stored)
        {
            item.ShouldBeSuccess();
        }

        result!.ShouldBeSuccess();
        result!.Value.Count.ShouldBe(2);
        result.Value[0].Name.ShouldBe("a");
        result.Value[1].Name.ShouldBe("b");
    }

    [Test]
    public async Task GetList_HonorsFindOptionsProjection()
    {
        // Arrange
        var repository = CreateRepository("find-options-proj", enableSoftDelete: false);
        var stored = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1
        {
            Name = "visible",
            Value = "hidden"
        });
        var filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty;
        var options = new FindOptions<GenericRepositoryTests.TestEntity1, GenericRepositoryTests.TestEntity1>
        {
            Projection = Builders<GenericRepositoryTests.TestEntity1>.Projection.Exclude(x => x.Value)
        };

        // Act
        var result = stored.IsSuccess
            ? await repository.GetListAsync(filter, options)
            : null;

        // Assert
        stored.ShouldBeSuccess();
        result!.ShouldBeSuccess();
        result!.Value.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("visible");
        result.Value[0].Value.ShouldBeNull();
    }

    [Test]
    public async Task GetListAsync_DerivedType_ReturnsOnlyDerivedDocuments()
    {
        // Arrange
        var repository = CreateRepository("find-derived", enableSoftDelete: false);
        var baseDocument = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "base" });
        var derivedDocument = baseDocument.IsSuccess
            ? await repository.StoreAsync(new GenericRepositoryTests.DerivedTestEntity1
            {
                Name = "derived",
                SomeValue = "extra"
            })
            : null;
        var filter = Builders<GenericRepositoryTests.DerivedTestEntity1>.Filter.Empty;

        // Act
        var derived = derivedDocument is { IsSuccess: true }
            ? await repository.GetListAsync(filter)
            : null;

        // Assert
        baseDocument.ShouldBeSuccess();
        derivedDocument!.ShouldBeSuccess();
        derived!.ShouldBeSuccess();
        derived!.Value.Count.ShouldBe(1);
        derived.Value[0].Name.ShouldBe("derived");
        derived.Value[0].SomeValue.ShouldBe("extra");
    }

    [Test]
    public async Task FindAsync_WithSession_ReturnsMatchingDocuments()
    {
        // Arrange
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
        var stored = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "sessioned" });
        var collection = stored.IsSuccess
            ? (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(
                new MongoDocumentBindingKey("e1"))).Value
            : null;
        var filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Name, "sessioned");

        // Act
        List<GenericRepositoryTests.TestEntity1>? docs = null;
        if (collection is not null)
        {
            using var session = await client.StartSessionAsync();
            using var cursor = await MongoCollectionCalls.FindAsync(
                collection,
                session,
                filter,
                options: null,
                CancellationToken.None);
            docs = await cursor.ToListAsync();
        }

        // Assert
        stored.ShouldBeSuccess();
        docs!.Count.ShouldBe(1);
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