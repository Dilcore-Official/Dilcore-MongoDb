using FluentResults;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Descriptors;
using Dilcore.MongoDB.Namespace;
using Dilcore.MongoDB.TestSupport;

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
        var resolver = new DefaultNamespaceResolver(
        [
            new PrefixNamespaceSegmentContributor()
        ], EmptyGraph);

        var result = await resolver.ResolveAsync(new NamespaceResolutionRequest
        {
            LogicalName = "orders",
            Target = NamespaceTarget.Collection,
            StaticPrefix = "acme"
        });

        result.ShouldBeSuccess();
        result.Value.ShouldBe("acme_orders");
    }

    [Test]
    public async Task Resolve_CustomContributor_CanFailClosed()
    {
        var resolver = new DefaultNamespaceResolver(
        [
            new PrefixNamespaceSegmentContributor(),
            new FailClosedContributor()
        ], EmptyGraph);

        var result = await resolver.ResolveAsync(new NamespaceResolutionRequest
        {
            LogicalName = "orders",
            Target = NamespaceTarget.Collection
        });

        result.ShouldBeFailure();
        result.Errors[0].Message.ShouldContain("required prefix");
    }

    [Test]
    public async Task Resolve_CachesWithinScopedResolver()
    {
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

        var first = await resolver.ResolveAsync(request);
        var second = await resolver.ResolveAsync(request);

        first.ShouldBeSuccess();
        second.ShouldBeSuccess();
        first.Value.ShouldBe(second.Value);
    }

    [Test]
    public async Task Resolve_InvalidDatabaseName_Fails()
    {
        var resolver = new DefaultNamespaceResolver(
        [
            new PrefixNamespaceSegmentContributor()
        ], EmptyGraph);

        var result = await resolver.ResolveAsync(new NamespaceResolutionRequest
        {
            LogicalName = "bad/name",
            Target = NamespaceTarget.Database
        });

        result.ShouldBeFailure();
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
