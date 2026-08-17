using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions.Keys;

namespace Dilcore.MongoDB.Descriptors;

internal sealed record DocumentBindingDescriptor(
    MongoDocumentBindingKey Key,
    Type DocumentType,
    MongoDatabaseKey DatabaseKey,
    string CollectionName,
    bool SoftDeleteEnabled,
    bool RegisterBulkRepository,
    bool RegisterProjectionRepository,
    string? NamespacePrefix,
    IReadOnlyList<object>? Indices,
    TimeSpan? CollectionItemsTimeToLive,
    LambdaExpression? TimeToLeavePropertySelector,
    Type? NamespacePrefixResolverType = null);
