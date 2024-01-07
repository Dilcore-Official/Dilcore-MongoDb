using FluentResults;

namespace Dilcore.DocumentDb.Abstractions;

public interface IDocumentDatabasePrefixProvider : IDocumentPrefixProvider
{ }

public interface IDocumentPrefixProvider
{
    Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default);
}