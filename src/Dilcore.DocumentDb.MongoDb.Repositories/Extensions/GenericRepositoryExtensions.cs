using System.Linq.Expressions;
using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Repositories.Abstractions;
using FluentResults;
using MongoDB.Driver;
// ReSharper disable CheckNamespace

namespace Dilcore.DocumentDb.MongoDb.Repositories;

public static class GenericRepositoryExtensions
{
    public static Task<Result<TDocument>> GetAsync<TDocument>(this IGenericRepository<TDocument> repository, Guid id,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity
    {
        var filter = Builders<TDocument>.Filter.Eq(x => x.Id, id);
        return repository.GetAsync(filter, cancellationToken);
    }

    public static Task<Result<TDerived>> GetAsync<TDocument, TDerived>(this IGenericRepository<TDocument> repository,
        Guid id,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity
        where TDerived : class, TDocument
    {
        var filter = Builders<TDerived>.Filter.Eq(x => x.Id, id);
        return repository.GetAsync(filter, cancellationToken);
    }

    public static Task<Result<TDocument>> GetAsync<TDocument>(this IGenericRepository<TDocument> repository,
        Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity
    {
        var filter = Builders<TDocument>.Filter.Where(expression);
        return repository.GetAsync(filter, cancellationToken);
    }

    public static Task<Result<IReadOnlyList<TDocument>>> GetListAsync<TDocument>(
        this IGenericRepository<TDocument> repository, Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity
    {
        var filter = Builders<TDocument>.Filter.Where(expression);
        return repository.GetListAsync(filter, cancellationToken);
    }

    public static Task<Result<IReadOnlyList<TDerived>>> GetListAsync<TDocument, TDerived>(
        this IGenericRepository<TDocument> repository, Expression<Func<TDerived, bool>> expression,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity
        where TDerived : class, TDocument
    {
        var filter = Builders<TDerived>.Filter.Where(expression);
        return repository.GetListAsync(filter, cancellationToken);
    }

    public static Task<Result<bool>> DeleteAsync<TDocument>(this IGenericRepository<TDocument> repository, Guid id,
        long eTag, CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity
    {
        var filter = Builders<TDocument>.Filter.Eq(x => x.Id, id);
        filter &= Builders<TDocument>.Filter.Eq(x => x.ETag, eTag);

        return repository.DeleteAsync(filter, cancellationToken);
    }

    public static Task<Result<bool>> DeleteAsync<TDocument>(this IGenericRepository<TDocument> repository,
        Expression<Func<TDocument, bool>> expression, CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity
    {
        var filter = Builders<TDocument>.Filter.Where(expression);
        return repository.DeleteAsync(filter, cancellationToken);
    }
}