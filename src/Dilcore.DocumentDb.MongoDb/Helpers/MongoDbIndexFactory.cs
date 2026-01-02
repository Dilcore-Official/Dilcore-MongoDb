using System.Linq.Expressions;
using Dilcore.DocumentDb.Abstractions;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Helpers;

public static class MongoDbIndexFactory
{
    public static CreateIndexModel<T> CreateAscendingIndex<T>(Expression<Func<T, object>> fieldSelector,
        Action<CreateIndexOptions>? optionsAction = null)
        where T : IDocumentEntity
    {
        CreateIndexOptions? options = null;

        if (optionsAction is not null)
        {
            options = new CreateIndexOptions();
            optionsAction.Invoke(options);
        }

        return new CreateIndexModel<T>(
            Builders<T>.IndexKeys.Ascending(fieldSelector), options);
    }

    public static CreateIndexModel<T> CreateCombinedIndex<T>(params IndexKeysDefinition<T>[] fieldSelectors)
        where T : IDocumentEntity
    {
        return new CreateIndexModel<T>(
            Builders<T>.IndexKeys.Combine(fieldSelectors));
    }
}