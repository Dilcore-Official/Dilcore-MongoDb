using FluentResults;

namespace Dilcore.DocumentDb.Abstractions;

public interface IDocumentPrefixProvider
{
    Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default);
}