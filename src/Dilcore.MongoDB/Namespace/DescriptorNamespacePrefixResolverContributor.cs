using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Descriptors;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.MongoDB.Namespace;

internal sealed class DescriptorNamespacePrefixResolverContributor(
    MongoRegistrationGraph graph,
    IServiceProvider serviceProvider) : INamespaceSegmentContributor
{
    public int Order => 90;

    public async Task<Result<string?>> ContributeAsync(
        NamespaceResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var resolverType = ResolvePrefixResolverType(request);
        if (resolverType is null)
        {
            return Result.Ok((string?)null);
        }

        var resolver = (INamespacePrefixResolver)serviceProvider.GetRequiredService(resolverType);
        return await resolver.ResolveAsync(request, cancellationToken);
    }

    private Type? ResolvePrefixResolverType(NamespaceResolutionRequest request)
    {
        return request.Target switch
        {
            NamespaceTarget.Database when request.DatabaseKey is { } databaseKey
                => TryGetDatabaseResolverType(databaseKey),
            NamespaceTarget.Collection when request.BindingKey is { } bindingKey
                => TryGetBindingResolverType(bindingKey),
            _ => null
        };
    }

    private Type? TryGetDatabaseResolverType(MongoDatabaseKey databaseKey)
    {
        try
        {
            return graph.GetDatabase(databaseKey).NamespacePrefixResolverType;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private Type? TryGetBindingResolverType(MongoDocumentBindingKey bindingKey)
    {
        try
        {
            return graph.GetBinding(bindingKey).NamespacePrefixResolverType;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
