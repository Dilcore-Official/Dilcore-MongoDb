using System.Linq.Expressions;
using Dilcore.DocumentDb.Abstractions;
using FluentResults;

namespace Dilcore.DocumentDb.MongoDb.Repositories.Abstractions;

public interface IGenericBulkRepository<TDocument>
    where TDocument : IDocumentEntity
{
    Task<Result<IReadOnlyList<TDocument>>> BulkStoreAsync(TDocument[] entities,
        CancellationToken cancellationToken = default);
    Task<Result> BulkDeleteAsync(Expression<Func<TDocument, bool>> expression, CancellationToken cancellationToken = default);
}