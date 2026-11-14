using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/guides/projections.md.</summary>
public class ProjectionsExampleTests : ExampleIntegrationTests
{
    public sealed class Employee : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Department { get; set; } = "";
        public decimal Salary { get; set; }
    }

    public sealed record EmployeeSummary(string Name, string Department);

    public sealed class DirectoryService(IGenericProjectionRepository<Employee> employees)
    {
        public Task<Result<IReadOnlyList<EmployeeSummary>>> ListAsync(string department, CancellationToken cancellationToken)
            => employees.GetListAsync(
                Builders<Employee>.Filter.Eq(x => x.Department, department),
                x => new EmployeeSummary(x.Name, x.Department),
                cancellationToken);

        public Task<Result<EmployeeSummary>> FindAsync(Guid id, CancellationToken cancellationToken)
            => employees.GetAsync(
                Builders<Employee>.Filter.Eq(x => x.Id, id),
                x => new EmployeeSummary(x.Name, x.Department),
                cancellationToken);
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("hr", db =>
            {
                db.OnCluster("primary");

                db.AddDocumentBinding<Employee>("employees", d => d
                    .WithCollectionName("employees")
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7)
                    .WithProjectionRepository());
            }));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task ListAndFind_ReturnProjectedSummaries()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredKeyedService<IGenericRepository<Employee>>("employees");
        var service = ActivatorUtilities.CreateInstance<DirectoryService>(scope.ServiceProvider);
        var ann = await repository.StoreAsync(new Employee { Name = "Ann", Department = "eng", Salary = 10m });
        (await repository.StoreAsync(new Employee { Name = "Bob", Department = "ops", Salary = 20m })).ShouldBeSuccess();

        // Act
        var list = await service.ListAsync("eng", CancellationToken.None);
        var found = await service.FindAsync(ann.Value.Id, CancellationToken.None);

        // Assert
        list.Value.ShouldBe([new EmployeeSummary("Ann", "eng")]);
        found.Value.ShouldBe(new EmployeeSummary("Ann", "eng"));
    }

    [Test]
    public async Task Find_OfMissingDocument_ReturnsDocumentNotFoundError()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = ActivatorUtilities.CreateInstance<DirectoryService>(scope.ServiceProvider);

        // Act
        var found = await service.FindAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        found.HasError<DocumentNotFoundError>().ShouldBeTrue();
    }
}