using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Provisioning;
using Dilcore.MongoDB.Extensions;
using FluentResults;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/guides/custom-provisioning-steps.md.</summary>
public class CustomProvisioningStepsExampleTests : ExampleIntegrationTests
{
    public sealed class MetricsCollectionStep : IProvisioningStep
    {
        public string Name => "metrics-timeseries";

        public async Task<Result<ProvisioningStepResult>> ExecuteAsync(
            IMongoDatabaseResolver databaseResolver,
            bool apply,
            CancellationToken cancellationToken = default)
        {
            var database = await databaseResolver.GetDatabaseAsync(new MongoDatabaseKey("telemetry"), cancellationToken);
            if (database.IsFailed)
                return database.ToResult();

            return Result.Ok(new ProvisioningStepResult
            {
                Name = Name,
                Action = apply ? "applied" : "would-apply",
                Details = database.Value.DatabaseNamespace.DatabaseName
            });
        }
    }

    public sealed class Metric : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("telemetry", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<Metric>("metrics", d => d.WithCollectionName("metrics"));
            }));

        services.AddSingleton<IProvisioningStep, MetricsCollectionStep>();

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task CustomStep_RunsInDryRunAndApply()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        // Act
        var dryRun = await provisioner.DryRunAsync();
        var report = await provisioner.ApplyAsync(CancellationToken.None);
        var custom = report.Value.Steps.Single(s => s.Name == "metrics-timeseries");

        // Assert
        dryRun.Value.Steps.Single(s => s.Name == "metrics-timeseries").Action.ShouldBe("would-apply");
        custom.Action.ShouldBe("applied");
        custom.Details.ShouldBe("telemetry");
    }

    [Test]
    public async Task FailingStep_MakesApplyFail()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("other", db => db.OnCluster("primary")));
        services.AddSingleton<IProvisioningStep, MetricsCollectionStep>();
        await using var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        // Act
        var report = await provisioner.ApplyAsync();

        // Assert
        report.ShouldBeFailure();
    }
}