using BenchmarkDotNet.Attributes;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Benchmarks.Infrastructure;
using Dilcore.MongoDB.Benchmarks.Models;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Benchmarks;

/// <summary>
/// Bulk store/delete vs raw <see cref="IMongoCollection{TDocument}.BulkWriteAsync"/>.
/// </summary>
public class BulkRepositoryBenchmarks
{
    private MongoBenchmarkFixture _fixture = null!;
    private IGenericBulkRepository<BenchmarkEntity> _bulkRepository = null!;
    private IMongoCollection<BenchmarkEntity> _collection = null!;
    private BenchmarkEntity[] _batch = null!;
    private HashSet<Guid> _batchIds = null!;

    [Params(100, 1000)]
    public int BatchSize { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _fixture = await MongoBenchmarkFixture.StartAsync().ConfigureAwait(false);
        _fixture.ConfigureServices(
            databaseName: "bench-bulk",
            collectionName: "bulk-entities",
            bindingKey: "bulk",
            softDelete: true,
            withBulk: true);

        _bulkRepository = _fixture.BulkRepository;
        _collection = _fixture.Collection;
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await _fixture.DisposeAsync().ConfigureAwait(false);
    }

    [IterationSetup(Targets = [nameof(RawDriver_BulkInsert), nameof(Library_BulkStoreAsync)])]
    public void SetupInsertBatch()
    {
        _batch = new BenchmarkEntity[BatchSize];
        for (var i = 0; i < BatchSize; i++)
        {
            _batch[i] = MongoBenchmarkFixture.NewEntity(name: $"bulk-{i}", value: i);
        }
    }

    [Benchmark]
    public async Task RawDriver_BulkInsert()
    {
        var writes = new WriteModel<BenchmarkEntity>[_batch.Length];
        for (var i = 0; i < _batch.Length; i++)
        {
            var entity = _batch[i];
            entity.Id = Guid.NewGuid();
            entity.ETag = 1;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = entity.CreatedAt;
            writes[i] = new InsertOneModel<BenchmarkEntity>(entity);
        }

        await _collection.BulkWriteAsync(writes).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Library_BulkStoreAsync()
    {
        await _bulkRepository.BulkStoreAsync(_batch).ConfigureAwait(false);
    }

    [IterationSetup(Targets = [nameof(RawDriver_BulkDelete), nameof(Library_BulkDeleteAsync)])]
    public void SetupDeleteBatch()
    {
        _batch = new BenchmarkEntity[BatchSize];
        for (var i = 0; i < BatchSize; i++)
        {
            _batch[i] = MongoBenchmarkFixture.NewEntity(name: $"bulk-del-{i}", value: i);
        }

        _bulkRepository.BulkStoreAsync(_batch).GetAwaiter().GetResult();
        _batchIds = _batch.Select(x => x.Id).ToHashSet();
    }

    [Benchmark]
    public async Task RawDriver_BulkDelete()
    {
        var update = Builders<BenchmarkEntity>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateManyAsync(
                Builders<BenchmarkEntity>.Filter.In(x => x.Id, _batchIds),
                update)
            .ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Library_BulkDeleteAsync()
    {
        await _bulkRepository.BulkDeleteAsync(x => _batchIds.Contains(x.Id)).ConfigureAwait(false);
    }
}
