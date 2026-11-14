using Dilcore.MongoDB.Abstractions.Policies;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Internal;

internal static class SoftDeleteFilterCache
{
    public static FilterDefinition<TDocument> GetNotDeletedFilter<TDocument>()
        where TDocument : class, IDocumentEntity
        => SoftDeleteHolder<TDocument>.Filter;

    private static class SoftDeleteHolder<TDocument>
        where TDocument : class, IDocumentEntity
    {
        public static readonly FilterDefinition<TDocument> Filter = Create();

        private static FilterDefinition<TDocument> Create()
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(typeof(TDocument)))
            {
                return Builders<TDocument>.Filter.Empty;
            }

            return Builders<TDocument>.Filter.Eq(PolicyElementNames.IsDeleted<TDocument>(), false);
        }
    }
}