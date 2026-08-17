using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.MongoDB.Repositories;

internal sealed class RepositoryResolver(IServiceProvider serviceProvider) : IRepositoryResolver
{
    public IGenericRepository<TDocument> GetRepository<TDocument>()
        where TDocument : class, IDocumentEntity =>
        serviceProvider.GetRequiredService<IGenericRepository<TDocument>>();

    public IGenericRepository<TDocument> GetRepository<TDocument>(string bindingKey)
        where TDocument : class, IDocumentEntity
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bindingKey);
        return serviceProvider.GetRequiredKeyedService<IGenericRepository<TDocument>>(bindingKey);
    }

    public IGenericBulkRepository<TDocument> GetBulkRepository<TDocument>()
        where TDocument : class, IDocumentEntity =>
        serviceProvider.GetRequiredService<IGenericBulkRepository<TDocument>>();

    public IGenericBulkRepository<TDocument> GetBulkRepository<TDocument>(string bindingKey)
        where TDocument : class, IDocumentEntity
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bindingKey);
        return serviceProvider.GetRequiredKeyedService<IGenericBulkRepository<TDocument>>(bindingKey);
    }

    public IGenericProjectionRepository<TDocument> GetProjectionRepository<TDocument>()
        where TDocument : class, IDocumentEntity =>
        serviceProvider.GetRequiredService<IGenericProjectionRepository<TDocument>>();

    public IGenericProjectionRepository<TDocument> GetProjectionRepository<TDocument>(string bindingKey)
        where TDocument : class, IDocumentEntity
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bindingKey);
        return serviceProvider.GetRequiredKeyedService<IGenericProjectionRepository<TDocument>>(bindingKey);
    }
}
