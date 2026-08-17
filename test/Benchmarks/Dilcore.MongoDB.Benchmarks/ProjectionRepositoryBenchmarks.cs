using BenchmarkDotNet.Attributes;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Benchmarks.Infrastructure;
using Dilcore.MongoDB.Benchmarks.Models;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Benchmarks;

/// <summary>
/// Typed projection queries vs raw driver projection pipelines.
/// </summary>
public class ProjectionRepositoryBenchmarks
{
    private const int SeedCount = 100;

    private MongoBenchmarkFixture _fixture = null!;
    private IGenericProjectionRepository<BenchmarkEntity> _projectionRepository = null!;
    private IMongoCollection<BenchmarkEntity> _collection = null!;
    private Guid _existingId;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _fixture = await MongoBenchmarkFixture.StartAsync().ConfigureAwait(false);
        _fixture.ConfigureServices(
            databaseName: "bench-projection",
            collectionName: "projection-entities",
            bindingKey: "projection",
            withBulk: true,
            withProjection: true);

        _projectionRepository = _fixture.ProjectionRepository;
        _collection = _fixture.Collection;

        var entities = new BenchmarkEntity[SeedCount];
        for (var i = 0; i < SeedCount; i++)
        {
            entities[i] = MongoBenchmarkFixture.NewEntity(name: $"proj-{i}", value: i);
        }

        await _fixture.BulkRepository.BulkStoreAsync(entities).ConfigureAwait(false);
        _existingId = entities[0].Id;
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await _fixture.DisposeAsync().ConfigureAwait(false);
    }

    [Benchmark]
    public async Task RawDriver_ProjectOne()
    {
        await _collection.Find(Builders<BenchmarkEntity>.Filter.Eq(x => x.Id, _existingId))
            .Project(x => new BenchmarkEntityProjection { Id = x.Id, Name = x.Name })
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Library_GetProjectedAsync()
    {
        var filter = Builders<BenchmarkEntity>.Filter.Eq(x => x.Id, _existingId);
        await _projectionRepository.GetAsync(
                filter,
                x => new BenchmarkEntityProjection { Id = x.Id, Name = x.Name })
            .ConfigureAwait(false);
    }

    [Benchmark]
    public async Task RawDriver_ProjectList()
    {
        await _collection.Find(FilterDefinition<BenchmarkEntity>.Empty)
            .Project(x => new BenchmarkEntityProjection { Id = x.Id, Name = x.Name })
            .ToListAsync()
            .ConfigureAwait(false);
    }

    [Benchmark]
    public async Task Library_GetListProjectedAsync()
    {
        await _projectionRepository.GetListAsync(
                x => new BenchmarkEntityProjection { Id = x.Id, Name = x.Name })
            .ConfigureAwait(false);
    }
}
