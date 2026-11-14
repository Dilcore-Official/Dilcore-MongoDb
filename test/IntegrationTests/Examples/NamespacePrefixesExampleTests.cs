using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Extensions;
using FluentResults;

namespace Dilcore.MongoDB.IntegrationTests.Examples;

/// <summary>Proves docs/guides/namespace-prefixes.md.</summary>
public class NamespacePrefixesExampleTests : ExampleIntegrationTests
{
    public sealed class User : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
    }

    public sealed class NameInspector(IMongoDbCollectionFactory collections)
    {
        public async Task<string?> CollectionNameAsync(CancellationToken cancellationToken)
        {
            var collection = await collections.GetCollectionAsync<User>(
                new MongoDocumentBindingKey("users"), cancellationToken);

            return collection.IsSuccess
                ? $"{collection.Value.Database.DatabaseNamespace.DatabaseName}.{collection.Value.CollectionNamespace.CollectionName}"
                : null;
        }
    }

    public sealed class EnvironmentSegmentContributor : INamespaceSegmentContributor
    {
        public int Order => 50;

        public Task<Result<string?>> ContributeAsync(
            NamespaceResolutionRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok<string?>("staging"));
    }

    private ServiceProvider BuildProvider(bool withContributor = false)
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("UserDB", db =>
            {
                db.OnCluster("primary");

                db.WithNamespacePrefix("catalog");

                db.AddDocumentBinding<User>("users", d => d
                    .WithCollectionName("users")
                    .WithNamespacePrefix("feature"));
            }));

        if (withContributor)
        {
            services.AddScoped<INamespaceSegmentContributor, EnvironmentSegmentContributor>();
        }

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task StaticPrefixes_ApplyToDatabaseAndCollection()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var inspector = ActivatorUtilities.CreateInstance<NameInspector>(scope.ServiceProvider);

        // Act
        var name = await inspector.CollectionNameAsync(CancellationToken.None);

        // Assert
        name.ShouldBe("catalog_UserDB.feature_users");
    }

    [Test]
    public async Task Contributor_RunsBeforeStaticPrefix()
    {
        // Arrange
        await using var provider = BuildProvider(withContributor: true);
        using var scope = provider.CreateScope();
        var inspector = ActivatorUtilities.CreateInstance<NameInspector>(scope.ServiceProvider);

        // Act
        var name = await inspector.CollectionNameAsync(CancellationToken.None);

        // Assert
        name.ShouldBe("staging_catalog_UserDB.staging_feature_users");
    }

    [Test]
    public void EmptyPrefix_IsRejectedAtRegistration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("UserDB", db =>
            {
                db.OnCluster("primary");
                db.WithNamespacePrefix(" ");
            }));

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Test]
    public async Task FailingContributor_ReturnsFailedResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("UserDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<User>("users", d => d.WithCollectionName("users"));
            }));
        services.AddScoped<INamespaceSegmentContributor, FailingContributor>();
        await using var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var collections = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();

        // Act
        var resolved = await collections.GetCollectionAsync<User>(new MongoDocumentBindingKey("users"));

        // Assert
        resolved.ShouldBeFailure();
    }

    public sealed class FailingContributor : INamespaceSegmentContributor
    {
        public int Order => 1;

        public Task<Result<string?>> ContributeAsync(
            NamespaceResolutionRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Fail<string?>("no segment"));
    }
}