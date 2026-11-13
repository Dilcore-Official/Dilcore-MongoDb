using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Provisioning;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.MongoDB.UnitTests;

/// <summary>
/// DI registration for <see cref="IMongoDbProvisioner"/> — no live MongoDB.
/// </summary>
public class ProvisioningDiRegistrationTests
{
    private const string PlaceholderConnectionString = "mongodb://127.0.0.1:27017";

    [Test]
    public void AddMongoDb_RegistersIMongoDbProvisioner_AsScoped_AndValidateOnBuild()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(PlaceholderConnectionString))
            .AddDatabase("ProvisionDiDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<ProvisionDiDoc>("docs", d => d.WithCollectionName("docs"));
            }));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var first = scope1.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
        var again = scope1.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
        var second = scope2.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        first.ShouldBeSameAs(again);
        first.ShouldNotBeSameAs(second);
    }

    [Test]
    public void AddMongoDb_CustomProvisioningStep_IsInjectedIntoEnumerable()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(PlaceholderConnectionString))
            .AddDatabase("ProvisionDiStepDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<ProvisionDiDoc>("docs", d => d.WithCollectionName("docs"));
            }));
        services.AddSingleton<IProvisioningStep, MarkerProvisioningStep>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>().ShouldNotBeNull();
        var steps = scope.ServiceProvider.GetServices<IProvisioningStep>().ToList();
        steps.ShouldContain(step => step is MarkerProvisioningStep);
        steps.ShouldContain(step => step.Name == MarkerProvisioningStep.StepName);
    }

    private sealed class ProvisionDiDoc : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    private sealed class MarkerProvisioningStep : IProvisioningStep
    {
        public const string StepName = "unit-marker-step";
        public string Name => StepName;

        public Task<Result<ProvisioningStepResult>> ExecuteAsync(
            IMongoDatabaseResolver databaseResolver,
            bool apply,
            CancellationToken cancellationToken)
            => Task.FromResult(Result.Ok(new ProvisioningStepResult
            {
                Name = Name,
                Action = apply ? "applied" : "would-apply"
            }));
    }
}
