using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Internal;

internal sealed class DocumentIdAccessor<TDocument, TId> : IDocumentIdAccessor<TDocument>
    where TDocument : class, IDocumentEntity
{
    private static readonly Func<TDocument, TId> GetId = CompileGetter();
    private static readonly Action<TDocument, TId> SetId = CompileSetter();
    private static readonly Func<TId, bool> IsEmptyValue = CompileIsEmpty();
    private static readonly Func<GuidIdGenerationStrategy, TId>? GenerateId = CompileGenerator();
    private static readonly Expression<Func<TDocument, TId>> IdExpression = CompileIdExpression();

    public Type IdentifierType => typeof(TId);

    public bool IsGuidIdentifier { get; } = typeof(TId) == typeof(Guid);

    public bool IsEmpty(TDocument document) => IsEmptyValue(GetId(document));

    public void EnsureNewId(TDocument document, GuidIdGenerationStrategy guidStrategy = GuidIdGenerationStrategy.Random)
    {
        if (!IsEmpty(document))
        {
            return;
        }

        if (GenerateId is null)
        {
            throw new UnsupportedIdentifierTypeException(typeof(TId));
        }

        SetId(document, GenerateId(guidStrategy));
    }

    public void ClearId(TDocument document) => SetId(document, default!);

    public FilterDefinition<TDocument> BuildIdFilter(TDocument document)
        => Builders<TDocument>.Filter.Eq(IdExpression, GetId(document));

    private static Expression<Func<TDocument, TId>> CompileIdExpression()
    {
        var document = Expression.Parameter(typeof(TDocument), "document");
        var body = Expression.Property(
            Expression.Convert(document, typeof(IDocumentEntity<TId>)),
            nameof(IDocumentEntity<TId>.Id));
        return Expression.Lambda<Func<TDocument, TId>>(body, document);
    }

    private static Func<TDocument, TId> CompileGetter() => CompileIdExpression().Compile();

    private static Action<TDocument, TId> CompileSetter()
    {
        var document = Expression.Parameter(typeof(TDocument), "document");
        var value = Expression.Parameter(typeof(TId), "value");
        var property = Expression.Property(
            Expression.Convert(document, typeof(IDocumentEntity<TId>)),
            nameof(IDocumentEntity<TId>.Id));
        var body = Expression.Assign(property, value);
        return Expression.Lambda<Action<TDocument, TId>>(body, document, value).Compile();
    }

    private static Func<TId, bool> CompileIsEmpty()
    {
        var id = Expression.Parameter(typeof(TId), "id");

        Expression body;
        if (typeof(TId) == typeof(string))
        {
            body = Expression.Call(
                typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!,
                Expression.Convert(id, typeof(string)));
        }
        else
        {
            Expression empty;
            if (typeof(TId) == typeof(Guid))
            {
                empty = Expression.Constant(Guid.Empty, typeof(TId));
            }
            else if (typeof(TId) == typeof(ObjectId))
            {
                empty = Expression.Constant(ObjectId.Empty, typeof(TId));
            }
            else
            {
                empty = Expression.Default(typeof(TId));
            }

            body = Expression.Equal(id, empty);
        }

        return Expression.Lambda<Func<TId, bool>>(body, id).Compile();
    }

    private static Func<GuidIdGenerationStrategy, TId>? CompileGenerator()
    {
        if (typeof(TId) == typeof(Guid))
        {
            return strategy =>
            {
                var guid = strategy switch
                {
                    GuidIdGenerationStrategy.Random => Guid.NewGuid(),
                    GuidIdGenerationStrategy.SequentialVersion7 => Guid.CreateVersion7(),
                    _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null)
                };
                return (TId)(object)guid;
            };
        }

        if (typeof(TId) == typeof(ObjectId))
        {
            return _ => (TId)(object)ObjectId.GenerateNewId();
        }

        return null;
    }
}