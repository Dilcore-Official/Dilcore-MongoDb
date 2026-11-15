using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Provisioning;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/guides/custom-provisioning-steps.md.</summary>
public class CustomProvisioningStepsExampleTests : ExampleIntegrationTests
{
    public sealed class MetricsCollectionStep : IProvisioningStep
    {
        private const string TimeSeriesName = "metrics_ts";

        public string Name => "metrics-timeseries";

        public async Task<Result<ProvisioningStepResult>> ExecuteAsync(
            IMongoDatabaseResolver databaseResolver,
            bool apply,
            CancellationToken cancellationToken = default)
        {
            var databaseKey = new MongoDatabaseKey("telemetry");
            var database = await databaseResolver.GetDatabaseAsync(databaseKey, cancellationToken);
            if (database.IsFailed)
            {
                return database.ToResult();
            }

            var names = await database.Value.ListCollectionNamesAsync(cancellationToken: cancellationToken);
            var existing = await names.ToListAsync(cancellationToken);
            if (existing.Contains(TimeSeriesName))
            {
                return Result.Ok(new ProvisioningStepResult
                {
                    Name = Name,
                    Action = "skip",
                    Details = database.Value.DatabaseNamespace.DatabaseName
                });
            }

            if (apply)
            {
                var options = new CreateCollectionOptions
                {
                    TimeSeriesOptions = new TimeSeriesOptions("timestamp")
                };
                await database.Value.CreateCollectionAsync(TimeSeriesName, options, cancellationToken);
            }

            return Result.Ok(new ProvisioningStepResult
            {
                Name = Name,
                Action = apply ? "create" : "would-create",
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
        var databases = scope.ServiceProvider.GetRequiredService<IMongoDatabaseResolver>();
        var telemetry = await databases.GetDatabaseAsync(new MongoDatabaseKey("telemetry"));
        var listed = await telemetry.Value.ListCollectionNamesAsync();
        var collectionNames = await listed.ToListAsync();

        // Assert
        dryRun.Value.Steps.Single(s => s.Name == "metrics-timeseries").Action.ShouldBe("would-create");
        custom.Action.ShouldBe("create");
        custom.Details.ShouldBe("telemetry");
        collectionNames.ShouldContain("metrics_ts");
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