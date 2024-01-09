using AutoFixture;
using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Extensions;
using Dilcore.DocumentDb.MongoDb.Repositories.Abstractions;
using Dilcore.DocumentDb.MongoDb.Repositories.Extensions;
using Dilcore.DocumentDb.MongoDb.Repositories.IntegrationTests.Infrastructure;
using FluentAssertions;
using FluentResults.Extensions.FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.DocumentDb.MongoDb.Repositories.IntegrationTests;

public class GenericRepositoryTests : BaseIntegrationTests
{
    private static Fixture _fixture = new Fixture();
    private IGenericRepository<TestEntity1> _repository;
    
    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();
        
        services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
        {
            builder.AddDatabase("TestDB1", 
                    db =>
                    {
                        db.AddGenericRepository<TestEntity1>(options => options.WithCollectionName("testEntity1")
                            .WithDatabaseName("TestDB1"));
                    });
        });

        _repository = services.BuildServiceProvider().GetRequiredService<IGenericRepository<TestEntity1>>();
    }
    
    [Test]
    public async Task GenericRepository_Store_CreateDocument()
    {
        var entity = _fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();
        
        var result = await _repository.StoreAsync(entity);

        result.Should().BeSuccess();
        
        var result2 = await _repository.GetAsync(result.ValueOrDefault.Id);
        result2.Should().BeSuccess();

        var createdEntity = result2.ValueOrDefault;
        createdEntity.Should().NotBeNull();
        
        createdEntity.Id.Should().NotBeEmpty();
        createdEntity.ETag.Should().NotBe(0);
        
        createdEntity.Name.Should().Be(entity.Name);
        createdEntity.Value.Should().Be(entity.Value);
    }
    
    [Test]
    public async Task GenericRepository_Store_UpdateDocument()
    {
        var entity = _fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();
        
        var createResult = await _repository.StoreAsync(entity);

        createResult.Should().BeSuccess();
        
        var result2 = await _repository.GetAsync(createResult.ValueOrDefault.Id);
        result2.Should().BeSuccess();
        
        var entityToUpdate = result2.ValueOrDefault;
        
        entityToUpdate.Name = "UpdatedName";
        entityToUpdate.Value = "UpdatedValue";
        
        var updateResult = await _repository.StoreAsync(entityToUpdate);
        updateResult.Should().BeSuccess();
        
        var result3 = await _repository.GetAsync(createResult.ValueOrDefault.Id);
        var updatedEntity = result3.ValueOrDefault;
        
        updatedEntity.Id.Should().NotBeEmpty();
        updatedEntity.Id.Should().Be(createResult.ValueOrDefault.Id);
        
        updatedEntity.ETag.Should().NotBe(0);
        updatedEntity.ETag.Should().NotBe(createResult.ValueOrDefault.ETag);
        
        updatedEntity.Name.Should().Be(entityToUpdate.Name);
        updatedEntity.Value.Should().Be(entityToUpdate.Value);
    }

    private class TestEntity1 : IDocumentEntity
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime UpdateAt { get; set; }
        public DateTime? ExpireAt { get; set; }
        
        public string Name { get; set; }
        public string Value { get; set; }
    }
}