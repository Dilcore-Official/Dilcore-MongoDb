using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Repositories.Abstractions;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Repositories.Extensions;

public static class MongoDatabaseContainerExtensions
{
    public static MongoDatabaseContainer AddGenericRepository<T>(this MongoDatabaseContainer databaseContainer,
        Action<GetCollectionOptions<T>> options)
        where T : class, IDocumentEntity
    {
        databaseContainer.AddMongoCollection(options);
        databaseContainer.Services.AddSingleton<IGenericRepository<T>, GenericMongoDbRepository<T>>((provider) =>
        {
            var collectionFactory = provider.GetRequiredService<IMongoDbCollectionFactory>();

            return new GenericMongoDbRepository<T>(options, GetCollectionAsync);

            Task<Result<IMongoCollection<T>>> GetCollectionAsync(CancellationToken token) =>
                collectionFactory.GetCollectionAsync<T>(databaseContainer.DbName, token);
        });

        return databaseContainer;
    }
}