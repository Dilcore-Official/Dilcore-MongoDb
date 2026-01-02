using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Extensions;
using Dilcore.DocumentDb.MongoDb.Services;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace Dilcore.DocumentDb.MongoDb.IntegrationTests;

public class ServiceCollectionExtensionsTests
{
    [Test]
    public void VerifyLifetimes_ShouldBeCorrect()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "mongodb://localhost:27017";

        services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
        {
            builder.AddDatabase("TestDB1",
                db =>
                {
                    db.AddBsonDocumentCollectionFactory();
                });
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        using (var scope = serviceProvider.CreateScope())
        {
            var provider = scope.ServiceProvider;

            // MongoClientProvider should be Singleton
            var clientProvider1 = serviceProvider.GetRequiredService<MongoClientProvider>();
            var clientProvider2 = serviceProvider.GetRequiredService<MongoClientProvider>();
            clientProvider1.ShouldBe(clientProvider2);

            // IMongoDbCollectionFactory should be Scoped
            VerifyScoped<IMongoDbCollectionFactory>(serviceProvider);

            // IBsonDocumentCollectionFactory should be Scoped
            VerifyScoped<IBsonDocumentCollectionFactory>(serviceProvider);

            // Keyed services
            VerifyKeyedScoped<IMongoDatabaseProvider>(serviceProvider, "TestDB1");
            VerifyKeyedScoped<IMongoDbCollectionProvider>(serviceProvider, "TestDB1");
            VerifyKeyedScoped<IDocumentDatabasePrefixProvider>(serviceProvider, "TestDB1");
            VerifyKeyedScoped<IDocumentCollectionPrefixProvider>(serviceProvider, "TestDB1");
        }
    }

    private void VerifyScoped<T>(ServiceProvider rootProvider) where T : class
    {
        using var scope1 = rootProvider.CreateScope();
        using var scope2 = rootProvider.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredService<T>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<T>();

        instance1.ShouldNotBe(instance2);

        var instance1_2 = scope1.ServiceProvider.GetRequiredService<T>();
        instance1.ShouldBe(instance1_2);
    }

    private void VerifyKeyedScoped<T>(ServiceProvider rootProvider, object serviceKey) where T : class
    {
        using var scope1 = rootProvider.CreateScope();
        using var scope2 = rootProvider.CreateScope();

        var instance1 = scope1.ServiceProvider.GetRequiredKeyedService<T>(serviceKey);
        var instance2 = scope2.ServiceProvider.GetRequiredKeyedService<T>(serviceKey);

        instance1.ShouldNotBe(instance2);

        var instance1_2 = scope1.ServiceProvider.GetRequiredKeyedService<T>(serviceKey);
        instance1.ShouldBe(instance1_2);
    }
}
