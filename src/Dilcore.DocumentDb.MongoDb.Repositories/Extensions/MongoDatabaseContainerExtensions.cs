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
        databaseContainer.Services.AddSingleton<IGenericRepository<T>, GenericMongoDbRepository<T>>(provider =>
            GetRepository(provider, databaseContainer, options));

        return databaseContainer;
    }

    public static MongoDatabaseContainer AddGenericRepository<T>(this MongoDatabaseContainer databaseContainer,
        Action<RegisterRepositoryOptions> registerRepositoryAction,
        Action<GetCollectionOptions<T>> options)
        where T : class, IDocumentEntity
    {
        var repositoryOptions = RegisterRepositoryOptions.Create();
        databaseContainer.AddGenericRepository(options);
        registerRepositoryAction(repositoryOptions);

        if (repositoryOptions.RegisterBulkRepository)
        {
            databaseContainer.Services.AddSingleton<IGenericBulkRepository<T>, GenericMongoDbBulkRepository<T>>(
                provider =>
                    GetBulkRepository(provider, databaseContainer, options));
        }

        return databaseContainer;
    }

    private static Task<Result<IMongoCollection<T>>> GetCollectionAsync<T>(IMongoDbCollectionFactory collectionFactory,
        string dbName, CancellationToken token)
        where T : class, IDocumentEntity
        => collectionFactory.GetCollectionAsync<T>(dbName, token);

    private static GenericMongoDbRepository<TDocument> GetRepository<TDocument>(IServiceProvider provider,
        MongoDatabaseContainer databaseContainer,
        Action<GetCollectionOptions<TDocument>> options)
        where TDocument : class, IDocumentEntity
    {
        var collectionFactory = provider.GetRequiredService<IMongoDbCollectionFactory>();

        return new GenericMongoDbRepository<TDocument>(options,
            token => GetCollectionAsync<TDocument>(collectionFactory, databaseContainer.DbName, token));
    }

    private static GenericMongoDbBulkRepository<TDocument> GetBulkRepository<TDocument>(IServiceProvider provider,
        MongoDatabaseContainer databaseContainer,
        Action<GetCollectionOptions<TDocument>> options)
        where TDocument : class, IDocumentEntity
    {
        var collectionFactory = provider.GetRequiredService<IMongoDbCollectionFactory>();

        return new GenericMongoDbBulkRepository<TDocument>(options,
            token => GetCollectionAsync<TDocument>(collectionFactory, databaseContainer.DbName, token));
    }
}