using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Repositories.Abstractions;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
// ReSharper disable CheckNamespace

namespace Dilcore.DocumentDb.MongoDb.Repositories;

public static class MongoDatabaseContainerExtensions
{
    public static MongoDatabaseContainer AddGenericRepository<TDocument>(this MongoDatabaseContainer databaseContainer,
        Action<GetCollectionOptions<TDocument>> options)
        where TDocument : class, IDocumentEntity
    {
        databaseContainer.AddMongoCollection(options);
        databaseContainer.Services.AddScoped<IGenericRepository<TDocument>, GenericMongoDbRepository<TDocument>>(
            provider =>
                GetRepository(provider, databaseContainer, options));

        return databaseContainer;
    }

    public static MongoDatabaseContainer AddGenericRepository<TDocument>(this MongoDatabaseContainer databaseContainer,
        Action<RegisterRepositoryOptions> registerRepositoryAction,
        Action<GetCollectionOptions<TDocument>> options)
        where TDocument : class, IDocumentEntity
    {
        var repositoryOptions = RegisterRepositoryOptions.Create();
        databaseContainer.AddGenericRepository(options);
        registerRepositoryAction(repositoryOptions);

        if (repositoryOptions.RegisterBulkRepository)
        {
            databaseContainer.Services
                .AddScoped<IGenericBulkRepository<TDocument>, GenericMongoDbBulkRepository<TDocument>>(
                    provider =>
                        GetBulkRepository(provider, databaseContainer, options));
        }

        if (repositoryOptions.RegisterProjectionRepository)
        {
            databaseContainer.Services
                .AddScoped<IGenericProjectionRepository<TDocument>, GenericMongoDbProjectionRepository<TDocument>>(
                    provider =>
                        GetProjectionRepository(provider, databaseContainer, options));
        }

        return databaseContainer;
    }

    private static Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync<TDocument>(
        IMongoDbCollectionFactory collectionFactory,
        string dbName, CancellationToken token)
        where TDocument : class, IDocumentEntity
        => collectionFactory.GetCollectionAsync<TDocument>(dbName, token);

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

    private static GenericMongoDbProjectionRepository<TDocument> GetProjectionRepository<TDocument>(
        IServiceProvider provider,
        MongoDatabaseContainer databaseContainer,
        Action<GetCollectionOptions<TDocument>> options)
        where TDocument : class, IDocumentEntity
    {
        var collectionFactory = provider.GetRequiredService<IMongoDbCollectionFactory>();

        return new GenericMongoDbProjectionRepository<TDocument>(options,
            token => GetCollectionAsync<TDocument>(collectionFactory, databaseContainer.DbName, token));
    }
}