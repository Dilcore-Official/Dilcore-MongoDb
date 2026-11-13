using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Provisioning;
using Dilcore.MongoDB.Descriptors;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Internal;

internal sealed class MongoDbProvisioner(
    MongoRegistrationGraph graph,
    IMongoDatabaseResolver databaseResolver,
    IMongoDbCollectionFactory collectionFactory,
    IEnumerable<IProvisioningStep> extraSteps) : IMongoDbProvisioner
{
    private const string LedgerCollection = "_dilcore_provisioning";
    private const string PlanVersion = "m3-1";
    private const string IdIndexName = "_id_";

    public Task<Result<ProvisioningReport>> DryRunAsync(CancellationToken cancellationToken = default)
        => RunAsync(apply: false, cancellationToken);

    public Task<Result<ProvisioningReport>> ApplyAsync(CancellationToken cancellationToken = default)
        => RunAsync(apply: true, cancellationToken);

    private async Task<Result<ProvisioningReport>> RunAsync(bool apply, CancellationToken cancellationToken)
    {
        var steps = new List<ProvisioningStepResult>();

        foreach (var binding in graph.Bindings)
        {
            var databaseResult = await databaseResolver.GetDatabaseAsync(binding.DatabaseKey, cancellationToken);
            if (databaseResult.IsFailed)
            {
                return databaseResult.ToResult();
            }

            var database = databaseResult.Value;
            var collectionNameResult = await collectionFactory.ResolveCollectionNameAsync(
                binding.DatabaseKey,
                binding.CollectionName,
                binding.NamespacePrefix,
                cancellationToken);
            if (collectionNameResult.IsFailed)
            {
                return collectionNameResult.ToResult();
            }

            var collectionName = collectionNameResult.Value;
            var collections = await (await database.ListCollectionNamesAsync(cancellationToken: cancellationToken))
                .ToListAsync(cancellationToken);
            var exists = collections.Contains(collectionName);

            if (!exists)
            {
                steps.Add(new ProvisioningStepResult
                {
                    Name = $"{binding.Key.Name}:collection",
                    Action = apply ? "create" : "would-create",
                    Details = collectionName
                });
                if (apply)
                {
                    await database.CreateCollectionAsync(collectionName, cancellationToken: cancellationToken);
                    exists = true;
                }
            }
            else
            {
                steps.Add(new ProvisioningStepResult
                {
                    Name = $"{binding.Key.Name}:collection",
                    Action = "skip",
                    Details = collectionName
                });
            }

            var applyIndexes = typeof(MongoDbProvisioner)
                .GetMethod(nameof(ApplyIndexesAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(binding.DocumentType);
            var task = (Task<Result>)applyIndexes.Invoke(
                this,
                [database, collectionName, binding, apply, exists, steps, cancellationToken])!;
            var indexResult = await task;
            if (indexResult.IsFailed)
            {
                return indexResult;
            }
        }

        foreach (var extraStep in extraSteps)
        {
            var extra = await extraStep.ExecuteAsync(databaseResolver, apply, cancellationToken);
            if (extra.IsFailed)
            {
                return extra.ToResult();
            }

            steps.Add(extra.Value);
        }

        if (apply && graph.Databases.Count > 0)
        {
            var firstDb = await databaseResolver.GetDatabaseAsync(graph.Databases[0].Key, cancellationToken);
            if (firstDb.IsSuccess)
            {
                var ledger = firstDb.Value.GetCollection<BsonDocument>(LedgerCollection);
                await ledger.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", PlanVersion),
                    Builders<BsonDocument>.Update
                        .SetOnInsert("appliedAt", DateTime.UtcNow)
                        .Set("lastRunAt", DateTime.UtcNow),
                    new UpdateOptions { IsUpsert = true },
                    cancellationToken);
            }
        }

        return Result.Ok(new ProvisioningReport { Applied = apply, Steps = steps });
    }

    private async Task<Result> ApplyIndexesAsync<TDocument>(
        IMongoDatabase database,
        string collectionName,
        DocumentBindingDescriptor binding,
        bool apply,
        bool collectionExists,
        List<ProvisioningStepResult> steps,
        CancellationToken cancellationToken)
        where TDocument : class, IDocumentEntity
    {
        var desired = BuildDesiredIndexModels<TDocument>(binding, collectionName);
        var renderArgs = CreateRenderArgs<TDocument>();

        IReadOnlyDictionary<string, BsonDocument> existingByName;
        if (!collectionExists)
        {
            existingByName = new Dictionary<string, BsonDocument>(StringComparer.Ordinal);
        }
        else
        {
            var collection = database.GetCollection<TDocument>(collectionName);
            var existing = await (await collection.Indexes.ListAsync(cancellationToken)).ToListAsync(cancellationToken);
            existingByName = existing
                .Select(doc => (Name: doc.GetValue("name", "").AsString, Doc: doc))
                .Where(x => !string.IsNullOrEmpty(x.Name))
                .ToDictionary(x => x.Name, x => x.Doc, StringComparer.Ordinal);
        }

        var desiredByName = new Dictionary<string, CreateIndexModel<TDocument>>(StringComparer.Ordinal);
        for (var i = 0; i < desired.Count; i++)
        {
            var model = desired[i];
            var name = ResolveIndexName(model, i);
            if (!desiredByName.TryAdd(name, model))
            {
                return Result.Fail($"Duplicate desired index name '{name}' on binding '{binding.Key.Name}'.");
            }
        }

        var toCreate = new List<CreateIndexModel<TDocument>>();
        var toRecreate = new List<(string Name, CreateIndexModel<TDocument> Model)>();
        var toRemove = new List<string>();

        foreach (var (name, model) in desiredByName)
        {
            if (!existingByName.TryGetValue(name, out var existing))
            {
                var action = apply ? "create" : "would-create";
                steps.Add(new ProvisioningStepResult
                {
                    Name = $"{binding.Key.Name}:index:{name}",
                    Action = action
                });
                toCreate.Add(EnsureNamed(model, name));
                continue;
            }

            if (IndexDefinitionMatches(existing, model, renderArgs))
            {
                steps.Add(new ProvisioningStepResult
                {
                    Name = $"{binding.Key.Name}:index:{name}",
                    Action = "skip"
                });
                continue;
            }

            steps.Add(new ProvisioningStepResult
            {
                Name = $"{binding.Key.Name}:index:{name}",
                Action = apply ? "recreate" : "would-recreate"
            });
            toRecreate.Add((name, EnsureNamed(model, name)));
        }

        foreach (var name in existingByName.Keys)
        {
            if (name == IdIndexName || desiredByName.ContainsKey(name))
            {
                continue;
            }

            steps.Add(new ProvisioningStepResult
            {
                Name = $"{binding.Key.Name}:index:{name}",
                Action = apply ? "remove" : "would-remove"
            });
            toRemove.Add(name);
        }

        if (!apply)
        {
            return Result.Ok();
        }

        if (!collectionExists && toCreate.Count == 0 && toRecreate.Count == 0 && toRemove.Count == 0)
        {
            return Result.Ok();
        }

        var collectionForMutate = database.GetCollection<TDocument>(collectionName);
        try
        {
            foreach (var name in toRemove)
            {
                await collectionForMutate.Indexes.DropOneAsync(name, cancellationToken);
            }

            foreach (var (name, _) in toRecreate)
            {
                await collectionForMutate.Indexes.DropOneAsync(name, cancellationToken);
            }

            var creates = toCreate.Concat(toRecreate.Select(x => x.Model)).ToList();
            if (creates.Count > 0)
            {
                await collectionForMutate.Indexes.CreateManyAsync(creates, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException or OperationCanceledException)
        {
            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            return Result.Fail($"Index reconciliation failed for '{collectionName}': {ex.Message}");
        }

        return Result.Ok();
    }

    private static List<CreateIndexModel<TDocument>> BuildDesiredIndexModels<TDocument>(
        DocumentBindingDescriptor binding,
        string collectionName)
        where TDocument : class, IDocumentEntity
    {
        var models = new List<CreateIndexModel<TDocument>>();
        if (binding.Indices is { Count: > 0 })
        {
            models.AddRange(binding.Indices.Cast<CreateIndexModel<TDocument>>());
        }

        if (binding is { CollectionItemsTimeToLive: { } ttl, TimeToLeavePropertySelector: not null })
        {
            var selector = (Expression<Func<TDocument, object>>)binding.TimeToLeavePropertySelector;
            models.Add(new CreateIndexModel<TDocument>(
                Builders<TDocument>.IndexKeys.Ascending(selector),
                new CreateIndexOptions { ExpireAfter = ttl, Name = $"{collectionName}_ttl" }));
        }

        return models;
    }

    private static string ResolveIndexName<TDocument>(CreateIndexModel<TDocument> model, int index)
        where TDocument : class, IDocumentEntity
        => model.Options?.Name ?? $"index-{index}";

    private static CreateIndexModel<TDocument> EnsureNamed<TDocument>(
        CreateIndexModel<TDocument> model,
        string name)
        where TDocument : class, IDocumentEntity
    {
        if (model.Options?.Name == name)
        {
            return model;
        }

        var options = model.Options is null
            ? new CreateIndexOptions { Name = name }
            : CloneOptionsWithName(model.Options, name);
        return new CreateIndexModel<TDocument>(model.Keys, options);
    }

    private static CreateIndexOptions CloneOptionsWithName(CreateIndexOptions source, string name)
        => new()
        {
            Name = name,
            Unique = source.Unique,
            Sparse = source.Sparse,
            ExpireAfter = source.ExpireAfter,
            Background = source.Background,
            Hidden = source.Hidden,
            Collation = source.Collation,
            DefaultLanguage = source.DefaultLanguage,
            LanguageOverride = source.LanguageOverride,
            TextIndexVersion = source.TextIndexVersion,
            Weights = source.Weights,
            Version = source.Version,
            SphereIndexVersion = source.SphereIndexVersion,
            Bits = source.Bits,
            Min = source.Min,
            Max = source.Max,
            StorageEngine = source.StorageEngine
        };

    private static bool IndexDefinitionMatches<TDocument>(
        BsonDocument existing,
        CreateIndexModel<TDocument> model,
        RenderArgs<TDocument> renderArgs)
        where TDocument : class, IDocumentEntity
    {
        var desiredKey = model.Keys.Render(renderArgs);
        var existingKey = existing.GetValue("key", new BsonDocument()).AsBsonDocument;
        if (!desiredKey.Equals(existingKey))
        {
            return false;
        }

        var desiredUnique = model.Options?.Unique ?? false;
        var existingUnique = existing.GetValue("unique", false).ToBoolean();
        if (desiredUnique != existingUnique)
        {
            return false;
        }

        var desiredSparse = model.Options?.Sparse ?? false;
        var existingSparse = existing.GetValue("sparse", false).ToBoolean();
        if (desiredSparse != existingSparse)
        {
            return false;
        }

        var desiredExpire = model.Options?.ExpireAfter;
        if (desiredExpire is { } ttl)
        {
            if (!existing.TryGetValue("expireAfterSeconds", out var expireValue) || !expireValue.IsNumeric)
            {
                return false;
            }

            return Math.Abs(expireValue.ToDouble() - ttl.TotalSeconds) < 0.001;
        }

        return !existing.Contains("expireAfterSeconds");
    }

    private static RenderArgs<TDocument> CreateRenderArgs<TDocument>()
        => new(
            BsonSerializer.LookupSerializer<TDocument>(),
            BsonSerializer.SerializerRegistry);
}
