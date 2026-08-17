using Dilcore.MongoDB.Abstractions;

namespace Dilcore.MongoDB.Abstractions.Repositories;

public interface IRepositoryResolver
{
    IGenericRepository<TDocument> GetRepository<TDocument>()
        where TDocument : class, IDocumentEntity;

    IGenericRepository<TDocument> GetRepository<TDocument>(string bindingKey)
        where TDocument : class, IDocumentEntity;

    IGenericBulkRepository<TDocument> GetBulkRepository<TDocument>()
        where TDocument : class, IDocumentEntity;

    IGenericBulkRepository<TDocument> GetBulkRepository<TDocument>(string bindingKey)
        where TDocument : class, IDocumentEntity;

    IGenericProjectionRepository<TDocument> GetProjectionRepository<TDocument>()
        where TDocument : class, IDocumentEntity;

    IGenericProjectionRepository<TDocument> GetProjectionRepository<TDocument>(string bindingKey)
        where TDocument : class, IDocumentEntity;
}
