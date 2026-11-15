# Read only the fields you need

Project documents into smaller types on the server with `IGenericProjectionRepository<TDocument>`. Package: `Dilcore.MongoDB`.

## Setup

```csharp
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;

var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")!;
var services = new ServiceCollection();

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("hr", db =>
    {
        db.OnCluster("primary");

        // The projection repository is opt-in per binding.
        db.AddDocumentBinding<Employee>("employees", d => d
            .WithCollectionName("employees")
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7)
            .WithProjectionRepository());
    }));

public sealed class Employee : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public decimal Salary { get; set; }
}

public sealed record EmployeeSummary(string Name, string Department);
```

## Use

```csharp
using Dilcore.MongoDB.Abstractions.Repositories;
using FluentResults;
using MongoDB.Driver;

public sealed class DirectoryService(IGenericProjectionRepository<Employee> employees)
{
    // Salary is never read from the database.
    public Task<Result<IReadOnlyList<EmployeeSummary>>> ListAsync(
        string department,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Employee>.Filter.Eq(x => x.Department, department);

        return employees.GetListAsync(
            filter,
            x => new EmployeeSummary(x.Name, x.Department),
            cancellationToken);
    }

    public Task<Result<EmployeeSummary>> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Employee>.Filter.Eq(x => x.Id, id);

        return employees.GetAsync(
            filter,
            x => new EmployeeSummary(x.Name, x.Department),
            cancellationToken);
    }
}
```

Members: `GetAsync<TProjection>(filter, projection)`, `GetListAsync<TProjection>(filter, projection)` and `GetListAsync<TProjection>(projection)` (no filter). `TProjection` must be a class (a record works).

## Handle failures

```csharp
using Dilcore.MongoDB.Abstractions.Results;

var summary = await directory.FindAsync(id, cancellationToken);
if (summary.HasError<DocumentNotFoundError>())
{
    // No document matches the filter.
}
```

## Pitfalls

- Call `WithProjectionRepository()` on the binding before you inject `IGenericProjectionRepository<T>`.
- Projections are read-only. To update a document, load the full document through `IGenericRepository<T>`.
- Keyset paging has its own `Projection` property with extra rules, see [Keyset paging](keyset-paging.md).

## See also

- [Create, read and update documents](crud-with-repositories.md)
- [Handle typed errors](handle-errors.md)
- Runnable example: [ProjectionsExampleTests](../../test/Repositories.IntegrationTests/Examples/ProjectionsExampleTests.cs)
