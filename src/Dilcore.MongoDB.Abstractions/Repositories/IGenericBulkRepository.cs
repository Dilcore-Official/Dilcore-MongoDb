using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions.Options;
using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Repositories;

public interface IGenericBulkRepository<TDocument>
    where TDocument : IDocumentEntity
{
    Task<Result<IReadOnlyList<TDocument>>> BulkStoreAsync(
        TDocument[] entities,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<TDocument>>> BulkStoreAsync(
        TDocument[] entities,
        MongoBulkWriteOptions options,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<TDocument>>> BulkStoreRangeAsync(
        IEnumerable<TDocument> entities,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<TDocument>>> BulkStoreRangeAsync(
        IEnumerable<TDocument> entities,
        MongoBulkWriteOptions options,
        CancellationToken cancellationToken = default);

    Task<Result> BulkDeleteAsync(
        Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default);
}
