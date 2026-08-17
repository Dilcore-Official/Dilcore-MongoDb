using BenchmarkDotNet.Attributes;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Benchmarks.Infrastructure;
using Dilcore.MongoDB.Benchmarks.Models;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.Repositories;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Benchmarks;

/// <summary>
/// Steady-state repository CRUD vs raw driver against Testcontainers MongoDB.
/// Soft-delete and hard-delete bindings share one DI container (one Guid serializer registration).
/// </summary>
public class RepositoryCrudBenchmarks
{
    private const int SeedCount = 100;
    private const string SoftBindingKey = "crud-soft";
    private const string HardBindingKey = "crud-hard";

    private MongoBenchmarkFixture _fixture = null!;
    private IGenericRepository<BenchmarkEntity> _repository = null!;
    private IMongoCollection<BenchmarkEntity> _collection = null!;
    private IGenericRepository<BenchmarkEntity> _hardDeleteRepository = null!;
    private IMongoCollection<BenchmarkEntity> _hardDeleteCollection = null!;
    private Guid _existingId;
    private BenchmarkEntity _updateTarget = null!;
    private BenchmarkEntity _softDeleteTarget = null!;
    private BenchmarkEntity _hardDeleteTarget = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _fixture = await MongoBenchmarkFixture.StartAsync().ConfigureAwait(false);
        _fixture.ConfigureServices(
            mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString(_fixture.ConnectionString))
                .AddDatabase("bench-crud", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<BenchmarkEntity>(SoftBindingKey, d => d
                        .WithCollectionName("crud-soft")
                        .WithSoftDelete());
                    db.AddDocumentBinding<BenchmarkEntity>(HardBindingKey, d => d
                        .WithCollectionName("crud-hard"));
                }),
            databaseName: "bench-crud",
            collectionName: "crud-soft");

        var resolver = _fixture.Resolver;
        _repository = resolver.GetRepository<BenchmarkEntity>(SoftBindingKey);
        _hardDeleteRepository = resolver.GetRepository<BenchmarkEntity>(HardBindingKey);
        _collection = _fixture.Collection;
        _hardDeleteCollection = _fixture.GetCollection("bench-crud", "crud-hard");

        for (var i = 0; i < SeedCount; i++)
        {
            var entity = MongoBenchmarkFixture.NewEntity(name: $"seed-{i}", value: i);
            var result = await _repository.StoreAsync(entity).ConfigureAwait(false);
            if (i == 0)
            {
                _existingId = result.Value.Id;
            }
        }
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await _fixture.DisposeAsync().ConfigureAwait(false);
    }

    [Benchmark]
    public async Task RawDriver_Insert()
    {
        var entity = MongoBenchmarkFixture.NewEntity();
        entity.Id = Guid.NewGuid();
        entity.ETag = 1;
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = entity.CreatedAt;
        await _collection.InsertOneAsync(entity).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Library_Store_Insert()
    {
        var entity = MongoBenchmarkFixture.NewEntity();
        await _repository.StoreAsync(entity).ConfigureAwait(false);
    }

    [IterationSetup(Targets = [nameof(RawDriver_Replace), nameof(Library_Store_Update)])]
    public void SetupUpdateTarget()
    {
        _updateTarget = MongoBenchmarkFixture.NewEntity();
        var stored = _repository.StoreAsync(_updateTarget).GetAwaiter().GetResult();
        _updateTarget = stored.Value;
        _updateTarget.Name = "updated-" + Guid.NewGuid().ToString("N");
        _updateTarget.Value++;
    }

    [Benchmark]
    public async Task RawDriver_Replace()
    {
        _updateTarget.ETag++;
        _updateTarget.UpdatedAt = DateTime.UtcNow;
        await _collection.ReplaceOneAsync(
                Builders<BenchmarkEntity>.Filter.Eq(x => x.Id, _updateTarget.Id),
                _updateTarget)
            .ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Library_Store_Update()
    {
        await _repository.StoreAsync(_updateTarget).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task RawDriver_FindById()
    {
        await _collection.Find(Builders<BenchmarkEntity>.Filter.Eq(x => x.Id, _existingId))
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Library_GetAsync()
    {
        await _repository.GetAsync(_existingId).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task RawDriver_FindList()
    {
        await _collection.Find(FilterDefinition<BenchmarkEntity>.Empty)
            .Limit(SeedCount)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Library_GetListAsync()
    {
        await _repository.GetListAsync().ConfigureAwait(false);
    }

    [Benchmark]
    public async Task RawDriver_FindEnumerable()
    {
        using var cursor = await _collection
            .Find(FilterDefinition<BenchmarkEntity>.Empty)
            .Limit(SeedCount)
            .ToCursorAsync()
            .ConfigureAwait(false);

        while (await cursor.MoveNextAsync().ConfigureAwait(false))
        {
            foreach (var _ in cursor.Current)
            {
            }
        }
    }

    [Benchmark]
    public async Task Library_GetAsyncEnumerable()
    {
        await foreach (var _ in _repository.GetAsyncEnumerable(FilterDefinition<BenchmarkEntity>.Empty)
                           .ConfigureAwait(false))
        {
        }
    }

    [Benchmark]
    public async Task RawDriver_Count()
    {
        await _collection.CountDocumentsAsync(FilterDefinition<BenchmarkEntity>.Empty).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Library_CountAsync()
    {
        await _repository.CountAsync(FilterDefinition<BenchmarkEntity>.Empty).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task RawDriver_Any()
    {
        await _collection.Find(FilterDefinition<BenchmarkEntity>.Empty)
            .Limit(1)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Library_HasAnyAsync()
    {
        await _repository.HasAnyAsync(FilterDefinition<BenchmarkEntity>.Empty).ConfigureAwait(false);
    }

    [IterationSetup(Targets = [nameof(Library_Delete_Soft), nameof(RawDriver_Delete_Soft)])]
    public void SetupSoftDeleteTarget()
    {
        _softDeleteTarget = MongoBenchmarkFixture.NewEntity();
        var stored = _repository.StoreAsync(_softDeleteTarget).GetAwaiter().GetResult();
        _softDeleteTarget = stored.Value;
    }

    [Benchmark]
    public async Task RawDriver_Delete_Soft()
    {
        var update = Builders<BenchmarkEntity>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(
                Builders<BenchmarkEntity>.Filter.Eq(x => x.Id, _softDeleteTarget.Id),
                update)
            .ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Library_Delete_Soft()
    {
        await _repository.DeleteAsync(_softDeleteTarget.Id, _softDeleteTarget.ETag).ConfigureAwait(false);
    }

    [IterationSetup(Targets = [nameof(Library_Delete_Hard), nameof(RawDriver_Delete_Hard)])]
    public void SetupHardDeleteTarget()
    {
        _hardDeleteTarget = MongoBenchmarkFixture.NewEntity();
        var stored = _hardDeleteRepository.StoreAsync(_hardDeleteTarget).GetAwaiter().GetResult();
        _hardDeleteTarget = stored.Value;
    }

    [Benchmark]
    public async Task RawDriver_Delete_Hard()
    {
        await _hardDeleteCollection.DeleteOneAsync(
                Builders<BenchmarkEntity>.Filter.Eq(x => x.Id, _hardDeleteTarget.Id))
            .ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Library_Delete_Hard()
    {
        await _hardDeleteRepository.DeleteAsync(_hardDeleteTarget.Id, _hardDeleteTarget.ETag)
            .ConfigureAwait(false);
    }
}
