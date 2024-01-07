using Dilcore.DocumentDb.Abstractions;
using FluentResults;

namespace Dilcore.DocumentDb.MongoDb.Defaults;

internal class DefaultDocumentDatabasePrefixProvider : IDocumentDatabasePrefixProvider
{
    public Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok(string.Empty));
    }
}