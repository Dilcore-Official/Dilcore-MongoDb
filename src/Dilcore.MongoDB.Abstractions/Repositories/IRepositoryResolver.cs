namespace Dilcore.MongoDB.Abstractions.Repositories;

/// <summary>
/// Resolves generic, bulk, and projection repositories for document types registered in DI.
/// </summary>
public interface IRepositoryResolver
{
    /// <summary>
    /// Returns the default <see cref="IGenericRepository{TDocument}"/> for <typeparamref name="TDocument"/>.
    /// </summary>
    /// <typeparam name="TDocument">Document type.</typeparam>
    /// <returns>Scoped repository instance from the service provider.</returns>
    IGenericRepository<TDocument> GetRepository<TDocument>()
        where TDocument : class, IDocumentEntity;

    /// <summary>
    /// Returns a named-binding <see cref="IGenericRepository{TDocument}"/> for <typeparamref name="TDocument"/>.
    /// </summary>
    /// <typeparam name="TDocument">Document type.</typeparam>
    /// <param name="bindingKey">Keyed service name from <c>AddMongoDb</c> registration.</param>
    /// <returns>Scoped repository for the binding.</returns>
    /// <exception cref="ArgumentException"><paramref name="bindingKey"/> is null or whitespace.</exception>
    IGenericRepository<TDocument> GetRepository<TDocument>(string bindingKey)
        where TDocument : class, IDocumentEntity;

    /// <summary>
    /// Returns the default <see cref="IGenericBulkRepository{TDocument}"/> for <typeparamref name="TDocument"/>.
    /// </summary>
    /// <typeparam name="TDocument">Document type.</typeparam>
    /// <returns>Scoped bulk repository instance.</returns>
    IGenericBulkRepository<TDocument> GetBulkRepository<TDocument>()
        where TDocument : class, IDocumentEntity;

    /// <summary>
    /// Returns a named-binding bulk repository for <typeparamref name="TDocument"/>.
    /// </summary>
    /// <typeparam name="TDocument">Document type.</typeparam>
    /// <param name="bindingKey">Keyed service name from registration.</param>
    /// <returns>Scoped bulk repository for the binding.</returns>
    /// <exception cref="ArgumentException"><paramref name="bindingKey"/> is null or whitespace.</exception>
    IGenericBulkRepository<TDocument> GetBulkRepository<TDocument>(string bindingKey)
        where TDocument : class, IDocumentEntity;

    /// <summary>
    /// Returns the default <see cref="IGenericProjectionRepository{TDocument}"/> for <typeparamref name="TDocument"/>.
    /// </summary>
    /// <typeparam name="TDocument">Document type.</typeparam>
    /// <returns>Scoped projection repository instance.</returns>
    IGenericProjectionRepository<TDocument> GetProjectionRepository<TDocument>()
        where TDocument : class, IDocumentEntity;

    /// <summary>
    /// Returns a named-binding projection repository for <typeparamref name="TDocument"/>.
    /// </summary>
    /// <typeparam name="TDocument">Document type.</typeparam>
    /// <param name="bindingKey">Keyed service name from registration.</param>
    /// <returns>Scoped projection repository for the binding.</returns>
    /// <exception cref="ArgumentException"><paramref name="bindingKey"/> is null or whitespace.</exception>
    IGenericProjectionRepository<TDocument> GetProjectionRepository<TDocument>(string bindingKey)
        where TDocument : class, IDocumentEntity;
}