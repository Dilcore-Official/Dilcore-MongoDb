using FluentResults;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Descriptors;
using Dilcore.MongoDB.Namespace;

namespace Dilcore.MongoDB.UnitTests;

public class NamespacePipelineTests
{
    private static readonly MongoRegistrationGraph EmptyGraph = new()
    {
        Clusters = [],
        Databases = [],
        Bindings = [],
        Conventions = ConventionsDescriptor.CreateDefault()
    };

    [Test]
    public async Task Resolve_AppliesStaticPrefix_WithDefaultSeparator()
    {
        // Arrange
        var resolver = new DefaultNamespaceResolver(
        [
            new PrefixNamespaceSegmentContributor()
        ], EmptyGraph);

        // Act
        var result = await resolver.ResolveAsync(new NamespaceResolutionRequest
        {
            LogicalName = "orders",
            Target = NamespaceTarget.Collection,
            StaticPrefix = "acme"
        });

        // Assert
        result.ShouldBeSuccess();
        result.Value.ShouldBe("acme_orders");
    }

    [Test]
    public async Task Resolve_CustomContributor_CanFailClosed()
    {
        // Arrange
        var resolver = new DefaultNamespaceResolver(
        [
            new PrefixNamespaceSegmentContributor(),
            new FailClosedContributor()
        ], EmptyGraph);

        // Act
        var result = await resolver.ResolveAsync(new NamespaceResolutionRequest
        {
            LogicalName = "orders",
            Target = NamespaceTarget.Collection
        });

        // Assert
        result.ShouldBeFailure();
        result.Errors[0].Message.ShouldContain("required prefix");
    }

    [Test]
    public async Task Resolve_CachesWithinScopedResolver()
    {
        // Arrange
        var resolver = new DefaultNamespaceResolver(
        [
            new PrefixNamespaceSegmentContributor()
        ], EmptyGraph);
        var request = new NamespaceResolutionRequest
        {
            LogicalName = "orders",
            Target = NamespaceTarget.Collection,
            StaticPrefix = "p"
        };

        // Act
        var first = await resolver.ResolveAsync(request);
        var second = await resolver.ResolveAsync(request);

        // Assert
        first.ShouldBeSuccess();
        second.ShouldBeSuccess();
        first.Value.ShouldBe(second.Value);
    }

    [Test]
    public async Task Resolve_InvalidDatabaseName_Fails()
    {
        // Arrange
        var resolver = new DefaultNamespaceResolver(
        [
            new PrefixNamespaceSegmentContributor()
        ], EmptyGraph);

        // Act
        var result = await resolver.ResolveAsync(new NamespaceResolutionRequest
        {
            LogicalName = "bad/name",
            Target = NamespaceTarget.Database
        });

        // Assert
        result.ShouldBeFailure();
    }

    [Test]
    public async Task Resolve_InvalidCollectionName_Fails()
    {
        // Arrange
        var resolver = new DefaultNamespaceResolver(
        [
            new PrefixNamespaceSegmentContributor()
        ], EmptyGraph);

        // Act
        var result = await resolver.ResolveAsync(new NamespaceResolutionRequest
        {
            LogicalName = "system.indexes",
            Target = NamespaceTarget.Collection
        });

        // Assert
        result.ShouldBeFailure();
        result.Errors[0].Message.ShouldContain("Invalid MongoDB collection name");
    }

    [Test]
    public async Task Resolve_UnknownNamespaceTarget_Fails()
    {
        // Arrange
        var resolver = new DefaultNamespaceResolver(
        [
            new PrefixNamespaceSegmentContributor()
        ], EmptyGraph);

        // Act
        var result = await resolver.ResolveAsync(new NamespaceResolutionRequest
        {
            LogicalName = "orders",
            Target = (NamespaceTarget)999
        });

        // Assert
        result.ShouldBeFailure();
        result.Errors[0].Message.ShouldContain("Unknown namespace target");
    }

    private sealed class FailClosedContributor : INamespaceSegmentContributor
    {
        public int Order => 50;

        public Task<Result<string?>> ContributeAsync(
            NamespaceResolutionRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Fail<string?>("A required prefix is missing."));
        }
    }
}