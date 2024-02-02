using Dilcore.DocumentDb.Abstractions;
using FluentResults;

namespace Dilcore.DocumentDb.MongoDb.Defaults;

internal class DefaultDocumentCollectionPrefixProvider : IDocumentCollectionPrefixProvider
{
    public Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok(string.Empty));
    }
}