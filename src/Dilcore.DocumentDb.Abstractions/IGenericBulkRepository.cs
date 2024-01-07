using System.Linq.Expressions;
using FluentResults;

namespace Dilcore.DocumentDb.Abstractions;

public interface IGenericBulkRepository<TDocument>
    where TDocument : IDocumentEntity
{
    Task<Result<IReadOnlyList<TDocument>>> BulkStoreAsync(TDocument[] entities);
    Task<Result> BulkDeleteAsync(Expression<Func<TDocument, bool>> expression);
}