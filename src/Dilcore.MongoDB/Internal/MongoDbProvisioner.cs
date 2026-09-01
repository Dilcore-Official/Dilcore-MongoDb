using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Provisioning;
using Dilcore.MongoDB.Descriptors;
using FluentResults;
using MongoDB.Bson;
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

            var indexModels = binding.Indices ?? Array.Empty<object>();
            if (indexModels.Count > 0 || binding.CollectionItemsTimeToLive is not null)
            {
                var applyIndexes = typeof(MongoDbProvisioner)
                    .GetMethod(nameof(ApplyIndexesAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(binding.DocumentType);
                var task = (Task<Result>)applyIndexes.Invoke(this, [database, collectionName, binding, apply, steps, cancellationToken])!;
                var indexResult = await task;
                if (indexResult.IsFailed)
                {
                    return indexResult;
                }
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
        List<ProvisioningStepResult> steps,
        CancellationToken cancellationToken)
        where TDocument : class, IDocumentEntity
    {
        var collection = database.GetCollection<TDocument>(collectionName);
        var existing = await (await collection.Indexes.ListAsync(cancellationToken)).ToListAsync(cancellationToken);
        var existingNames = existing.Select(doc => doc.GetValue("name", "").AsString).ToHashSet(StringComparer.Ordinal);

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

        foreach (var model in models)
        {
            var name = model.Options?.Name ?? $"index-{models.IndexOf(model)}";
            var action = existingNames.Contains(name) ? "skip" : apply ? "create" : "would-create";
            steps.Add(new ProvisioningStepResult
            {
                Name = $"{binding.Key.Name}:index:{name}",
                Action = action
            });
        }

        var missing = models.Where(model =>
        {
            var name = model.Options?.Name;
            return name is null || !existingNames.Contains(name);
        }).ToList();

        if (apply && missing.Count > 0)
        {
            await collection.Indexes.CreateManyAsync(missing, cancellationToken);
        }

        return Result.Ok();
    }
}
