using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Abstractions.Ownership;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.IntegrationTests.Infrastructure;
using Dilcore.MongoDB.Internal;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.IntegrationTests;

public class DiAcceptanceTests : BaseIntegrationTests
{
    [Test]
    public void VerifyLifetimes_ClientsSingleton_ResolversScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity>("orders", d => d.WithCollectionName("orders"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);

        // Act
        var client1 = root.GetRequiredKeyedService<IMongoClient>("primary");
        var client2 = root.GetRequiredKeyedService<IMongoClient>("primary");

        using var scope1 = root.CreateScope();
        using var scope2 = root.CreateScope();

        var factory1 = scope1.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var factory2 = scope2.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();

        var resolver1 = scope1.ServiceProvider.GetRequiredService<INamespaceResolver>();
        var resolver2 = scope2.ServiceProvider.GetRequiredService<INamespaceResolver>();

        // Assert
        client1.ShouldBe(client2);
        factory1.ShouldNotBe(factory2);
        resolver1.ShouldNotBe(resolver2);
    }

    [Test]
    public async Task MultiDatabase_StoresInCorrectPhysicalDatabases()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("TestDB1", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity1>("e1", d => d.WithCollectionName("testEntity1"));
                db.AddDocumentBinding<TestEntity2>("e2", d => d.WithCollectionName("testEntity2"));
            })
            .AddDatabase("TestDB2", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity3>("e3", d => d.WithCollectionName("testEntity3"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();
        var sp = scope.ServiceProvider;

        // Act
        await sp.GetRequiredService<IGenericRepository<TestEntity1>>().StoreAsync(new TestEntity1 { Value = 1 });
        await sp.GetRequiredService<IGenericRepository<TestEntity2>>().StoreAsync(new TestEntity2 { Value = "2" });
        await sp.GetRequiredService<IGenericRepository<TestEntity3>>().StoreAsync(new TestEntity3 { Value = 3.3m });

        // Assert
        var client = sp.GetRequiredKeyedService<IMongoClient>("primary");
        var names = await (await client.ListDatabaseNamesAsync()).ToListAsync();
        names.ShouldContain("TestDB1");
        names.ShouldContain("TestDB2");

        var entity1 = await client.GetDatabase("TestDB1").GetCollection<TestEntity1>("testEntity1")
            .Find(x => x.Value == 1).FirstOrDefaultAsync();
        entity1.ShouldNotBeNull();

        var entity3WrongDb = await client.GetDatabase("TestDB1").GetCollection<TestEntity3>("testEntity3")
            .Find(x => x.Value == 3.3m).FirstOrDefaultAsync();
        entity3WrongDb.ShouldBeNull();
    }

    [Test]
    public async Task NamespacePrefix_AppliesToDatabaseAndCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("TestDB1", db =>
            {
                db.OnCluster("primary").WithNamespacePrefix("prefix1");
                db.AddDocumentBinding<TestEntity1>("e1", d => d
                    .WithCollectionName("testEntity1")
                    .WithNamespacePrefix("collectionPrefix"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();
        var sp = scope.ServiceProvider;

        // Act
        await sp.GetRequiredService<IGenericRepository<TestEntity1>>().StoreAsync(new TestEntity1 { Value = 1 });

        // Assert
        var client = sp.GetRequiredKeyedService<IMongoClient>("primary");
        var names = await (await client.ListDatabaseNamesAsync()).ToListAsync();
        names.ShouldContain("prefix1_TestDB1");

        var entity = await client.GetDatabase("prefix1_TestDB1")
            .GetCollection<TestEntity1>("collectionPrefix_testEntity1")
            .Find(x => x.Value == 1).FirstOrDefaultAsync();
        entity.ShouldNotBeNull();
    }

    [Test]
    public void DuplicateDatabaseRegistration_FailsAtStartup()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString(connectionString))
                .AddDatabase("TestDB1", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<TestEntity1>("e1", d => d.WithCollectionName("testEntity1"));
                })
                .AddDatabase("TestDB1", db => db.OnCluster("primary"))));
    }

    [Test]
    public void ExternalOwnedClient_IsNotDisposedWithContainer()
    {
        // Arrange
        var external = new MongoClient(MongoDbContainer.GetConnectionString());
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("shared", c => c.UseExistingClient(external, MongoClientOwnership.ExternalOwned))
            .AddDatabase("app", db =>
            {
                db.OnCluster("shared");
                db.AddDocumentBinding<TestEntity>("orders", d => d.WithCollectionName("orders"));
            }));

        var root = AcceptanceServiceProviderFactory.Create(services);
        var holder = root.GetRequiredKeyedService<MongoClientHolder>("shared");
        var client = holder.Client;

        // Act
        root.Dispose();

        // Assert
        client.ShouldBe(external);

        // External client remains usable after container dispose.
        Should.NotThrow(() => external.ListDatabaseNames());
    }

    [TestCase(true)]
    [TestCase(false)]
    public void BulkAndProjection_OptionalRegistration(bool enabled)
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity1>("e1", d =>
                {
                    d.WithCollectionName("testEntity1");
                    if (enabled)
                    {
                        d.WithBulkRepository().WithProjectionRepository();
                    }
                });
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();
        var sp = scope.ServiceProvider;

        // Act & Assert
        if (enabled)
        {
            Should.NotThrow(() => sp.GetRequiredService<IGenericBulkRepository<TestEntity1>>());
            Should.NotThrow(() => sp.GetRequiredService<IGenericProjectionRepository<TestEntity1>>());
        }
        else
        {
            Should.Throw<InvalidOperationException>(() => sp.GetRequiredService<IGenericBulkRepository<TestEntity1>>());
            Should.Throw<InvalidOperationException>(() => sp.GetRequiredService<IGenericProjectionRepository<TestEntity1>>());
        }
    }

    [Test]
    public async Task BsonPath_UsesNamespacePipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("TestDb", db =>
            {
                db.OnCluster("primary").WithNamespacePrefix("pfx");
                db.AddDocumentBinding<TestEntity>("keep-graph-valid", d => d.WithCollectionName("keep"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();

        // Act
        var result = await factory.GetCollectionAsync(
            new MongoDatabaseKey("TestDb"),
            "TestCollection",
            staticPrefix: "col");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.CollectionNamespace.CollectionName.ShouldBe("col_TestCollection");
        result.Value.CollectionNamespace.DatabaseNamespace.DatabaseName.ShouldBe("pfx_TestDb");
    }

    [Test]
    public async Task CustomPrefixContributor_FailsBeforeGetCollection_WhenRequiredPrefixMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();
        services.AddScoped<INamespaceSegmentContributor>(_ => new AsyncLocalPrefixContributor { RequirePrefix = true });

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity>("orders", d => d.WithCollectionName("orders"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();

        // Act
        var result = await factory.GetCollectionAsync<TestEntity>(new MongoDocumentBindingKey("orders"));

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("required namespace prefix");
    }

    [Test]
    public async Task TwoClusters_UseDistinctSingletonClients()
    {
        // Arrange
        var connectionString = MongoDbContainer.GetConnectionString();
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddCluster("analytics", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity>("orders", d => d.WithCollectionName("orders"));
            })
            .AddDatabase("metrics", db =>
            {
                db.OnCluster("analytics");
                db.AddDocumentBinding<TestEntity>("metrics-orders", d => d.WithCollectionName("orders"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);

        using var scope = root.CreateScope();
        var repositories = scope.ServiceProvider.GetRequiredService<IRepositoryResolver>();
        var orders = repositories.GetRepository<TestEntity>("orders");
        var metrics = repositories.GetRepository<TestEntity>("metrics-orders");

        // Act
        var primary = root.GetRequiredKeyedService<IMongoClient>("primary");
        var analytics = root.GetRequiredKeyedService<IMongoClient>("analytics");
        await orders.StoreAsync(new TestEntity { Value = 1 });
        await metrics.StoreAsync(new TestEntity { Value = 2 });

        // Assert
        primary.ShouldNotBe(analytics);

        var primaryDoc = await primary.GetDatabase("app").GetCollection<TestEntity>("orders")
            .Find(x => x.Value == 1).FirstOrDefaultAsync();
        var analyticsDoc = await analytics.GetDatabase("metrics").GetCollection<TestEntity>("orders")
            .Find(x => x.Value == 2).FirstOrDefaultAsync();
        primaryDoc.ShouldNotBeNull();
        analyticsDoc.ShouldNotBeNull();
    }

    [Test]
    public async Task SameType_TwoBindings_ResolvesKeyedWithoutUnkeyed()
    {
        // Arrange
        var connectionString = MongoDbContainer.GetConnectionString();
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity>("orders-main", d => d.WithCollectionName("orders"));
            })
            .AddDatabase("archive", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity>("orders-archive", d => d.WithCollectionName("orders"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();
        var sp = scope.ServiceProvider;
        var repositories = sp.GetRequiredService<IRepositoryResolver>();

        // Act
        var main = repositories.GetRepository<TestEntity>("orders-main");
        var archive = repositories.GetRepository<TestEntity>("orders-archive");
        await main.StoreAsync(new TestEntity { Value = 10 });
        await archive.StoreAsync(new TestEntity { Value = 20 });

        // Assert
        Should.Throw<InvalidOperationException>(() => repositories.GetRepository<TestEntity>());

        var client = sp.GetRequiredKeyedService<IMongoClient>("primary");
        (await client.GetDatabase("app").GetCollection<TestEntity>("orders").Find(x => x.Value == 10).FirstOrDefaultAsync())
            .ShouldNotBeNull();
        (await client.GetDatabase("archive").GetCollection<TestEntity>("orders").Find(x => x.Value == 20).FirstOrDefaultAsync())
            .ShouldNotBeNull();
    }

    [Test]
    public async Task SameType_TwoCollections_SameDatabase()
    {
        // Arrange
        var connectionString = MongoDbContainer.GetConnectionString();
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity>("products-live", d => d.WithCollectionName("products_live"));
                db.AddDocumentBinding<TestEntity>("products-staging", d => d.WithCollectionName("products_staging"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();
        var sp = scope.ServiceProvider;
        var repositories = sp.GetRequiredService<IRepositoryResolver>();

        // Act
        await repositories.GetRepository<TestEntity>("products-live")
            .StoreAsync(new TestEntity { Value = 1 });
        await repositories.GetRepository<TestEntity>("products-staging")
            .StoreAsync(new TestEntity { Value = 2 });

        // Assert
        var db = sp.GetRequiredKeyedService<IMongoDatabase>("app");
        (await db.GetCollection<TestEntity>("products_live").Find(x => x.Value == 1).FirstOrDefaultAsync()).ShouldNotBeNull();
        (await db.GetCollection<TestEntity>("products_staging").Find(x => x.Value == 2).FirstOrDefaultAsync()).ShouldNotBeNull();
        (await db.GetCollection<TestEntity>("products_live").Find(x => x.Value == 2).FirstOrDefaultAsync()).ShouldBeNull();
    }

    [Test]
    public async Task ParallelCustomPrefixIsolation_WritesLandInPrefixedNamespaces()
    {
        // Arrange
        var connectionString = MongoDbContainer.GetConnectionString();
        var services = new ServiceCollection();
        services.AddScoped<INamespaceSegmentContributor>(_ => new AsyncLocalPrefixContributor { RequirePrefix = true });

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity>("orders", d => d.WithCollectionName("orders"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);

        // Act
        await Parallel.ForEachAsync(new[] { "tenantA", "tenantB" }, async (prefix, _) =>
        {
            using var scope = root.CreateScope();
            using (AsyncLocalPrefixContributor.Use(prefix))
            {
                var repo = scope.ServiceProvider.GetRequiredService<IGenericRepository<TestEntity>>();
                await repo.StoreAsync(new TestEntity { Value = prefix == "tenantA" ? 1 : 2 });
            }
        });

        // Assert
        var client = root.GetRequiredKeyedService<IMongoClient>("primary");
        (await client.GetDatabase("tenantA_app").GetCollection<TestEntity>("tenantA_orders")
            .CountDocumentsAsync(FilterDefinition<TestEntity>.Empty)).ShouldBe(1);
        (await client.GetDatabase("tenantB_app").GetCollection<TestEntity>("tenantB_orders")
            .CountDocumentsAsync(FilterDefinition<TestEntity>.Empty)).ShouldBe(1);
        var unscopedNames = await (await client.GetDatabase("app").ListCollectionNamesAsync()).ToListAsync();
        unscopedNames.ShouldNotContain("orders");
    }

    [Test]
    public async Task MissingRequiredCustomPrefix_StoreAsync_DoesNotWrite()
    {
        // Arrange
        var connectionString = MongoDbContainer.GetConnectionString();
        var services = new ServiceCollection();
        var collectionName = $"missing_prefix_{Guid.NewGuid():N}";
        services.AddScoped<INamespaceSegmentContributor>(_ => new AsyncLocalPrefixContributor { RequirePrefix = true });

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity>("orders", d => d.WithCollectionName(collectionName));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGenericRepository<TestEntity>>();

        // Act
        var result = await repo.StoreAsync(new TestEntity { Value = 99 });

        // Assert
        result.IsFailed.ShouldBeTrue();

        var client = root.GetRequiredKeyedService<IMongoClient>("primary");
        var count = await client.GetDatabase("app")
            .GetCollection<TestEntity>(collectionName)
            .CountDocumentsAsync(FilterDefinition<TestEntity>.Empty);
        count.ShouldBe(0);
    }

    [Test]
    public async Task EscapeHatches_KeyedDriverTypesMatchRepositoryPath()
    {
        // Arrange
        var connectionString = MongoDbContainer.GetConnectionString();
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity>("orders", d => d.WithCollectionName("orders_escape"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();
        var sp = scope.ServiceProvider;

        var client = sp.GetRequiredKeyedService<IMongoClient>("primary");
        var database = sp.GetRequiredKeyedService<IMongoDatabase>("app");
        var collection = sp.GetRequiredKeyedService<IMongoCollection<TestEntity>>("orders");

        // Act
        var sameClient = ReferenceEquals(client, root.GetRequiredKeyedService<IMongoClient>("primary"));
        await sp.GetRequiredService<IGenericRepository<TestEntity>>().StoreAsync(new TestEntity { Value = 7 });

        // Assert
        database.DatabaseNamespace.DatabaseName.ShouldBe("app");
        collection.CollectionNamespace.CollectionName.ShouldBe("orders_escape");
        sameClient.ShouldBeTrue();
        (await collection.Find(x => x.Value == 7).FirstOrDefaultAsync()).ShouldNotBeNull();
    }

    [Test]
    public async Task RepositoryResolver_KeylessKeyedAndAmbiguous()
    {
        // Arrange
        var connectionString = MongoDbContainer.GetConnectionString();
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity1>("e1", d => d
                    .WithCollectionName("resolver_e1")
                    .WithBulkRepository()
                    .WithProjectionRepository());
                db.AddDocumentBinding<TestEntity>("orders-main", d => d.WithCollectionName("orders_main"));
                db.AddDocumentBinding<TestEntity>("orders-archive", d => d.WithCollectionName("orders_archive"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();
        var repositories = scope.ServiceProvider.GetRequiredService<IRepositoryResolver>();

        // Act
        var keyless = repositories.GetRepository<TestEntity1>();
        await keyless.StoreAsync(new TestEntity1 { Value = 42 });
        var main = repositories.GetRepository<TestEntity>("orders-main");
        var archive = repositories.GetRepository<TestEntity>("orders-archive");
        await main.StoreAsync(new TestEntity { Value = 1 });
        await archive.StoreAsync(new TestEntity { Value = 2 });

        // Assert
        // Single binding: keyless works for plain / bulk / projection.
        keyless.ShouldNotBeNull();
        Should.NotThrow(() => repositories.GetBulkRepository<TestEntity1>());
        Should.NotThrow(() => repositories.GetProjectionRepository<TestEntity1>());
        (await keyless.GetListAsync()).ValueOrDefault!.ShouldContain(x => x.Value == 42);

        // Multiple bindings: keyless fails; keyed disambiguates.
        Should.Throw<InvalidOperationException>(() => repositories.GetRepository<TestEntity>());

        var client = scope.ServiceProvider.GetRequiredKeyedService<IMongoClient>("primary");
        (await client.GetDatabase("app").GetCollection<TestEntity>("orders_main")
            .Find(x => x.Value == 1).FirstOrDefaultAsync()).ShouldNotBeNull();
        (await client.GetDatabase("app").GetCollection<TestEntity>("orders_archive")
            .Find(x => x.Value == 2).FirstOrDefaultAsync()).ShouldNotBeNull();
    }

    [Test]
    public async Task AsyncPrefixResolver_OnDatabase_AppliesToPhysicalDatabaseName()
    {
        // Arrange
        var connectionString = MongoDbContainer.GetConnectionString();
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("UserDB", db =>
            {
                db.OnCluster("primary");
                db.WithNamespacePrefixResolver<AsyncLocalNamespacePrefixResolver>();
                db.AddDocumentBinding<TestEntity>("users", d => d.WithCollectionName("users"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();

        // Act
        using (AsyncLocalNamespacePrefixResolver.Use("tenantA"))
        {
            await scope.ServiceProvider.GetRequiredService<IGenericRepository<TestEntity>>()
                .StoreAsync(new TestEntity { Value = 1 });
        }

        // Assert
        var client = root.GetRequiredKeyedService<IMongoClient>("primary");
        var names = await (await client.ListDatabaseNamesAsync()).ToListAsync();
        names.ShouldContain("tenantA_UserDB");

        (await client.GetDatabase("tenantA_UserDB").GetCollection<TestEntity>("users")
            .Find(x => x.Value == 1).FirstOrDefaultAsync()).ShouldNotBeNull();
    }

    [Test]
    public async Task AsyncPrefixResolver_OnBinding_AppliesToPhysicalCollectionName()
    {
        // Arrange
        var connectionString = MongoDbContainer.GetConnectionString();
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity>("orders", d => d
                    .WithCollectionName("orders")
                    .WithNamespacePrefixResolver<AsyncLocalNamespacePrefixResolver>());
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();

        // Act
        using (AsyncLocalNamespacePrefixResolver.Use("feat"))
        {
            await scope.ServiceProvider.GetRequiredService<IGenericRepository<TestEntity>>()
                .StoreAsync(new TestEntity { Value = 9 });
        }

        // Assert
        var client = root.GetRequiredKeyedService<IMongoClient>("primary");
        (await client.GetDatabase("app").GetCollection<TestEntity>("feat_orders")
            .Find(x => x.Value == 9).FirstOrDefaultAsync()).ShouldNotBeNull();
    }

    [Test]
    public async Task AsyncPrefixResolver_FailClosed_DoesNotWrite()
    {
        // Arrange
        var connectionString = MongoDbContainer.GetConnectionString();
        var services = new ServiceCollection();
        var collectionName = $"async_fail_{Guid.NewGuid():N}";

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db =>
            {
                db.OnCluster("primary");
                db.WithNamespacePrefixResolver<AsyncLocalNamespacePrefixResolver>();
                db.AddDocumentBinding<TestEntity>("orders", d => d.WithCollectionName(collectionName));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();

        // Act
        Result<TestEntity> result;
        using (AsyncLocalNamespacePrefixResolver.UseFailure())
        {
            result = await scope.ServiceProvider.GetRequiredService<IGenericRepository<TestEntity>>()
                .StoreAsync(new TestEntity { Value = 99 });
        }

        // Assert
        result.IsFailed.ShouldBeTrue();

        var client = root.GetRequiredKeyedService<IMongoClient>("primary");
        var count = await client.GetDatabase("app")
            .GetCollection<TestEntity>(collectionName)
            .CountDocumentsAsync(FilterDefinition<TestEntity>.Empty);
        count.ShouldBe(0);
    }

    [Test]
    public async Task AsyncPrefixResolver_CombinesWithStaticPrefix()
    {
        // Arrange
        var connectionString = MongoDbContainer.GetConnectionString();
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("UserDB", db =>
            {
                db.OnCluster("primary");
                db.WithNamespacePrefix("catalog");
                db.WithNamespacePrefixResolver<AsyncLocalNamespacePrefixResolver>();
                db.AddDocumentBinding<TestEntity>("users", d => d.WithCollectionName("users"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();

        // Act
        using (AsyncLocalNamespacePrefixResolver.Use("tenantA"))
        {
            await scope.ServiceProvider.GetRequiredService<IGenericRepository<TestEntity>>()
                .StoreAsync(new TestEntity { Value = 3 });
        }

        // Assert
        var client = root.GetRequiredKeyedService<IMongoClient>("primary");
        var names = await (await client.ListDatabaseNamesAsync()).ToListAsync();
        names.ShouldContain("tenantA_catalog_UserDB");
    }

    [Test]
    public async Task AsyncPrefixResolver_SkipsCache_WhenPrefixChangesInSameScope()
    {
        // Arrange
        var connectionString = MongoDbContainer.GetConnectionString();
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("CacheDb", db =>
            {
                db.OnCluster("primary");
                db.WithNamespacePrefixResolver<AsyncLocalNamespacePrefixResolver>();
                db.AddDocumentBinding<TestEntity>("orders", d => d.WithCollectionName("orders"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGenericRepository<TestEntity>>();

        // Act
        using (AsyncLocalNamespacePrefixResolver.Use("t1"))
        {
            await repo.StoreAsync(new TestEntity { Value = 1 });
        }

        using (AsyncLocalNamespacePrefixResolver.Use("t2"))
        {
            await repo.StoreAsync(new TestEntity { Value = 2 });
        }

        // Assert
        var client = root.GetRequiredKeyedService<IMongoClient>("primary");
        (await client.GetDatabase("t1_CacheDb").GetCollection<TestEntity>("orders")
            .CountDocumentsAsync(FilterDefinition<TestEntity>.Empty)).ShouldBe(1);
        (await client.GetDatabase("t2_CacheDb").GetCollection<TestEntity>("orders")
            .CountDocumentsAsync(FilterDefinition<TestEntity>.Empty)).ShouldBe(1);
    }

    [Test]
    public void ResolverChainOrder_AppliesStaticPrefix()
    {
        // Arrange
        var config = new V1ParityHarness.V1Config(
            DatabaseKey: "app",
            LogicalDatabaseName: "app",
            LogicalCollectionName: "orders",
            DatabasePrefix: "env",
            CollectionPrefix: "svc");

        // Act
        var expected = V1ParityHarness.Project(config);

        // Assert
        expected.Database.ShouldBe("env_app");
        expected.Collection.ShouldBe("svc_orders");
    }

    [Test]
    public void V1ParityHarness_RejectsDivergentDatabaseNames()
    {
        // Arrange
        var config = new V1ParityHarness.V1Config(
            DatabaseKey: "reg",
            LogicalDatabaseName: "logical",
            LogicalCollectionName: "orders",
            DatabasePrefix: null,
            CollectionPrefix: null);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => V1ParityHarness.Project(config));
    }

    public class TestEntity : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int Value { get; set; }
    }

    public class TestEntity1 : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int Value { get; set; }
    }

    public class TestEntity2 : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Value { get; set; }
    }

    public class TestEntity3 : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal Value { get; set; }
    }
}