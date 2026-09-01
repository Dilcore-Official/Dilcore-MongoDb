using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Provisioning;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Transactions;
using Dilcore.MongoDB.DependencyInjection;
using Dilcore.MongoDB.Descriptors;
using Dilcore.MongoDB.Internal;
using Dilcore.MongoDB.Json;
using Dilcore.MongoDB.Namespace;
using Dilcore.MongoDB.Repositories;
using Dilcore.MongoDB.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMongoDb(
        this IServiceCollection services,
        Action<IMongoDbBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new MongoDbBuilder();
        configure(builder);
        var graph = builder.Build();
        MongoConventionRegistrar.EnsureRegistered(graph.Conventions);

        services.AddSingleton(graph);
        // Default static-prefix contributor. Apps may register additional
        // INamespaceSegmentContributor implementations for dynamic prefixes (e.g. multi-tenancy).
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<INamespaceSegmentContributor, PrefixNamespaceSegmentContributor>());
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<INamespaceSegmentContributor, DescriptorNamespacePrefixResolverContributor>());
        services.AddScoped<INamespaceResolver>(sp =>
            new DefaultNamespaceResolver(
                sp.GetServices<INamespaceSegmentContributor>(),
                sp.GetRequiredService<MongoRegistrationGraph>()));
        services.AddScoped<IMongoDatabaseResolver, MongoDatabaseResolver>();
        services.AddScoped<IMongoDbCollectionFactory, MongoDbCollectionFactory>();
        services.AddScoped<IMongoDbProvisioner, MongoDbProvisioner>();
        services.AddSingleton<IBsonJsonConverter, BsonJsonConverter>();
        services.AddScoped<IMongoDbTransactionRunner, MongoDbTransactionRunner>();
        services.AddScoped<IRepositoryResolver, RepositoryResolver>();

        foreach (var resolverType in graph.Databases
                     .Select(d => d.NamespacePrefixResolverType)
                     .Concat(graph.Bindings.Select(b => b.NamespacePrefixResolverType))
                     .Where(t => t is not null)
                     .Distinct())
        {
            services.TryAddScoped(resolverType!);
        }

        foreach (var cluster in graph.Clusters)
        {
            var clusterKey = cluster.Key.Name;
            var descriptor = cluster;

            services.AddKeyedSingleton(clusterKey, (_, _) => new MongoClientHolder(descriptor));
            services.AddKeyedSingleton<IMongoClient>(clusterKey, (sp, _) =>
                sp.GetRequiredKeyedService<MongoClientHolder>(clusterKey).Client);
        }

        foreach (var databaseKey in graph.Databases.Select(d => d.Key.Name))
        {
            services.AddKeyedScoped<IMongoDatabase>(databaseKey, (sp, _) =>
            {
                var resolver = sp.GetRequiredService<IMongoDatabaseResolver>();
                var result = resolver.GetDatabaseAsync(new MongoDatabaseKey(databaseKey))
                    .GetAwaiter()
                    .GetResult();

                if (result.IsFailed)
                {
                    throw new InvalidOperationException(
                        string.Join("; ", result.Errors.Select(e => e.Message)));
                }

                return result.Value;
            });
        }

        foreach (var binding in graph.Bindings)
        {
            RegisterBinding(services, binding);
        }

        RegisterUnkeyedRepositories(services, graph);

        return services;
    }

    private static void RegisterBinding(IServiceCollection services, DocumentBindingDescriptor binding)
    {
        var bindingKey = binding.Key.Name;
        var documentType = binding.DocumentType;

        var registerCollection = typeof(ServiceCollectionExtensions)
            .GetMethod(nameof(RegisterTypedBinding), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(documentType);

        registerCollection.Invoke(null, [services, binding, bindingKey]);
    }

    private static void RegisterTypedBinding<TDocument>(
        IServiceCollection services,
        DocumentBindingDescriptor binding,
        string bindingKey)
        where TDocument : class, IDocumentEntity
    {
        services.AddKeyedScoped<IMongoCollection<TDocument>>(bindingKey, (sp, _) =>
        {
            var factory = sp.GetRequiredService<IMongoDbCollectionFactory>();
            var result = factory.GetCollectionAsync<TDocument>(binding.Key)
                .GetAwaiter()
                .GetResult();

            if (result.IsFailed)
            {
                throw new InvalidOperationException(
                    string.Join("; ", result.Errors.Select(e => e.Message)));
            }

            return result.Value;
        });

        Action<GetCollectionOptions<TDocument>> optionsAction = options =>
        {
            options.WithCollectionName(binding.CollectionName);
            if (binding.SoftDeleteEnabled)
            {
                options.WithSoftDelete();
            }

            options.WithGuidIdGeneration(binding.GuidIdGenerationStrategy);

            if (binding.Indices is { Count: > 0 })
            {
                options.WithIndexes(binding.Indices.Cast<CreateIndexModel<TDocument>>().ToArray());
            }

            if (binding is { CollectionItemsTimeToLive: { } ttl, TimeToLeavePropertySelector: not null })
            {
                options.WithCollectionItemsTimeToLive(
                    ttl,
                    (Expression<Func<TDocument, object>>)binding.TimeToLeavePropertySelector);
            }
        };

        services.AddKeyedScoped<IGenericRepository<TDocument>>(bindingKey, (sp, _) =>
            CreateRepository(sp, binding.Key, optionsAction));

        if (binding.RegisterBulkRepository)
        {
            services.AddKeyedScoped<IGenericBulkRepository<TDocument>>(bindingKey, (sp, _) =>
                CreateBulkRepository(sp, binding.Key, optionsAction));
        }

        if (binding.RegisterProjectionRepository)
        {
            services.AddKeyedScoped<IGenericProjectionRepository<TDocument>>(bindingKey, (sp, _) =>
                CreateProjectionRepository(sp, binding.Key, optionsAction));
        }
    }

    private static void RegisterUnkeyedRepositories(IServiceCollection services, MongoRegistrationGraph graph)
    {
        foreach (var group in graph.Bindings.GroupBy(b => b.DocumentType))
        {
            if (group.Count() != 1)
            {
                continue;
            }

            var binding = group.Single();
            var method = typeof(ServiceCollectionExtensions)
                .GetMethod(nameof(RegisterUnkeyedTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(binding.DocumentType);

            method.Invoke(null, [services, binding]);
        }
    }

    private static void RegisterUnkeyedTyped<TDocument>(
        IServiceCollection services,
        DocumentBindingDescriptor binding)
        where TDocument : class, IDocumentEntity
    {
        Action<GetCollectionOptions<TDocument>> optionsAction = options =>
        {
            options.WithCollectionName(binding.CollectionName);
            if (binding.SoftDeleteEnabled)
            {
                options.WithSoftDelete();
            }

            options.WithGuidIdGeneration(binding.GuidIdGenerationStrategy);
        };

        services.AddScoped<IGenericRepository<TDocument>>(sp =>
            CreateRepository(sp, binding.Key, optionsAction));

        if (binding.RegisterBulkRepository)
        {
            services.AddScoped<IGenericBulkRepository<TDocument>>(sp =>
                CreateBulkRepository(sp, binding.Key, optionsAction));
        }

        if (binding.RegisterProjectionRepository)
        {
            services.AddScoped<IGenericProjectionRepository<TDocument>>(sp =>
                CreateProjectionRepository(sp, binding.Key, optionsAction));
        }
    }

    private static GenericMongoDbRepository<TDocument> CreateRepository<TDocument>(
        IServiceProvider sp,
        MongoDocumentBindingKey bindingKey,
        Action<GetCollectionOptions<TDocument>> optionsAction)
        where TDocument : class, IDocumentEntity
    {
        var factory = sp.GetRequiredService<IMongoDbCollectionFactory>();
        return new GenericMongoDbRepository<TDocument>(
            optionsAction,
            ct => factory.GetCollectionAsync<TDocument>(bindingKey, ct));
    }

    private static GenericMongoDbBulkRepository<TDocument> CreateBulkRepository<TDocument>(
        IServiceProvider sp,
        MongoDocumentBindingKey bindingKey,
        Action<GetCollectionOptions<TDocument>> optionsAction)
        where TDocument : class, IDocumentEntity
    {
        var factory = sp.GetRequiredService<IMongoDbCollectionFactory>();
        return new GenericMongoDbBulkRepository<TDocument>(
            optionsAction,
            ct => factory.GetCollectionAsync<TDocument>(bindingKey, ct));
    }

    private static GenericMongoDbProjectionRepository<TDocument> CreateProjectionRepository<TDocument>(
        IServiceProvider sp,
        MongoDocumentBindingKey bindingKey,
        Action<GetCollectionOptions<TDocument>> optionsAction)
        where TDocument : class, IDocumentEntity
    {
        var factory = sp.GetRequiredService<IMongoDbCollectionFactory>();
        return new GenericMongoDbProjectionRepository<TDocument>(
            optionsAction,
            ct => factory.GetCollectionAsync<TDocument>(bindingKey, ct));
    }
}
