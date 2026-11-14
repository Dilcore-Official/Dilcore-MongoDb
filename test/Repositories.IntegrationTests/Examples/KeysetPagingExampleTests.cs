using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/guides/keyset-paging.md.</summary>
public class KeysetPagingExampleTests : ExampleIntegrationTests
{
    public sealed class Post : IDocumentEntity<Guid>, IAuditableDocument
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Title { get; set; } = "";
    }

    public sealed class PostService(IGenericRepository<Post> posts)
    {
        public Task<Result<KeysetPage<Post>>> ListAsync(string? cursor, CancellationToken cancellationToken)
            => posts.GetPageAsync(
                new KeysetPageRequest<Post>
                {
                    Filter = Builders<Post>.Filter.Empty,
                    Sort = Builders<Post>.Sort.Descending(x => x.CreatedAt),
                    PageSize = 2,
                    Cursor = cursor
                },
                cancellationToken);
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("blog", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<Post>("posts", d => d
                    .WithCollectionName("posts")
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
            }));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task Pages_WalkAllItemsWithoutRepeats()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var posts = scope.ServiceProvider.GetRequiredService<IGenericRepository<Post>>();
        var service = ActivatorUtilities.CreateInstance<PostService>(scope.ServiceProvider);
        for (var i = 0; i < 5; i++)
        {
            (await posts.StoreAsync(new Post { Title = $"p{i}" })).ShouldBeSuccess();
        }

        var seen = new List<Guid>();
        string? cursor = null;

        // Act
        bool more;
        do
        {
            var page = await service.ListAsync(cursor, CancellationToken.None);
            page.ShouldBeSuccess();
            seen.AddRange(page.Value.Items.Select(x => x.Id));
            cursor = page.Value.NextCursor;
            more = page.Value.HasMore;
        }
        while (more);

        // Assert
        seen.Count.ShouldBe(5);
        seen.Distinct().Count().ShouldBe(5);
    }

    [Test]
    public async Task MalformedCursor_ReturnsFailedResult()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = ActivatorUtilities.CreateInstance<PostService>(scope.ServiceProvider);

        // Act
        var page = await service.ListAsync("not-a-cursor", CancellationToken.None);

        // Assert
        page.ShouldBeFailure();
    }

    [Test]
    public async Task CursorFromAnotherSort_ReturnsFailedResult()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var posts = scope.ServiceProvider.GetRequiredService<IGenericRepository<Post>>();
        var service = ActivatorUtilities.CreateInstance<PostService>(scope.ServiceProvider);
        for (var i = 0; i < 3; i++)
        {
            (await posts.StoreAsync(new Post { Title = $"p{i}" })).ShouldBeSuccess();
        }

        var first = await service.ListAsync(null, CancellationToken.None);

        // Act
        var other = await posts.GetPageAsync(new KeysetPageRequest<Post>
        {
            Filter = Builders<Post>.Filter.Empty,
            Sort = Builders<Post>.Sort.Ascending(x => x.CreatedAt),
            PageSize = 2,
            Cursor = first.Value.NextCursor
        });

        // Assert
        other.ShouldBeFailure();
    }
}