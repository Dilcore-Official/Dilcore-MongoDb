# Dilcore MongoDB

An opinionated .NET MongoDB application toolkit: validated multi-cluster / multi-database DI, scoped tenant-aware namespace resolution, and repository helpers over `MongoDB.Driver`.

> **v2 roadmap:** See [ROADMAP.md](ROADMAP.md) and [roadmap issues](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues?q=is%3Aissue+label%3Aroadmap). Package selection: [docs/product/package-selection.md](docs/product/package-selection.md). Naming: [ADR 0001](docs/adr/0001-package-naming.md).

## Packages

| Package | Role |
|---------|------|
| `Dilcore.MongoDB.Abstractions` | Contracts, keys, namespace abstractions, repository interfaces |
| `Dilcore.MongoDB` | DI, namespace pipeline, repositories, driver integration |

## Community and trust

| Document | Purpose |
|----------|---------|
| [CONTRIBUTING.md](CONTRIBUTING.md) | How to contribute, develop, and open pull requests |
| [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) | Community standards |
| [SECURITY.md](SECURITY.md) | Private vulnerability reporting |
| [SUPPORT.md](SUPPORT.md) | Where to get help (Issues vs Discussions) |
| [GOVERNANCE.md](GOVERNANCE.md) | Maintainers and decision making |
| [CHANGELOG.md](CHANGELOG.md) | Notable changes |
| [LICENSE](LICENSE) | MIT |

## 🏗️ Architecture Overview

The library follows Clean Architecture principles with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Generic Repos   │  │ Bulk Repos      │  │ Projection  │ │
│  │                 │  │                 │  │ Repos       │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ MongoDB         │  │ Collection      │  │ Database    │ │
│  │ Implementation  │  │ Providers       │  │ Providers   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                   Abstractions Layer                        │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ IDocumentEntity │  │ Repository      │  │ Prefix      │ │
│  │                 │  │ Interfaces      │  │ Providers   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## 🚀 Key Features

- **Multi-Database Support**: Configure and manage multiple MongoDB databases within a single application
- **Repository Pattern**: Generic, Bulk, and Projection repositories with FluentResults for error handling
- **Prefix Resolvers**: Dynamic database and collection naming with custom prefix providers
- **Type Safety**: Strongly-typed entities with automatic serialization/deserialization
- **Soft Delete Support**: Built-in soft delete functionality for entities
- **Index Management**: Automatic index creation and management
- **Dependency Injection**: Full integration with Microsoft.Extensions.DependencyInjection
- **Thread Safety**: Thread-safe operations with proper async/await patterns

## 📦 Core Components

### Document Entity Interface

All entities must implement `IDocumentEntity`:

```csharp
public interface IDocumentEntity
{
    Guid Id { get; set; }
    long ETag { get; set; }
    bool IsDeleted { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
```

### Repository Types

#### 1. Generic Repository (`IGenericRepository<T>`)
Standard CRUD operations with filtering and querying capabilities:

```csharp
public interface IGenericRepository<TDocument> where TDocument : IDocumentEntity
{
    Task<Result<TDocument>> StoreAsync(TDocument entity, CancellationToken cancellationToken = default);
    Task<Result<TDocument>> GetAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TDocument>>> GetListAsync(CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default);
    Task<Result<bool>> HasAnyAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default);
    Task<Result<long>> CountAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default);
}
```

#### 2. Bulk Repository (`IGenericBulkRepository<T>`)
Optimized for bulk operations:

```csharp
public interface IGenericBulkRepository<TDocument> where TDocument : IDocumentEntity
{
    Task<Result<IReadOnlyList<TDocument>>> BulkStoreAsync(TDocument[] entities, CancellationToken cancellationToken = default);
    Task<Result> BulkDeleteAsync(Expression<Func<TDocument, bool>> expression, CancellationToken cancellationToken = default);
}
```

#### 3. Projection Repository (`IGenericProjectionRepository<T>`)
Optimized for data projections and transformations:

```csharp
public interface IGenericProjectionRepository<TDocument> where TDocument : IDocumentEntity
{
    Task<Result<TProjection>> GetAsync<TProjection>(FilterDefinition<TDocument> filter, Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TProjection>>> GetListAsync<TProjection>(FilterDefinition<TDocument> filter, Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default);
}
```

## 🗄️ Multi-Database Approach

The library supports multiple MongoDB databases within a single application, each with its own configuration, collections, and prefix resolvers.

### Database Configuration

```csharp
services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("UserDB", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<User>("users", d => d.WithCollectionName("users"));
        db.AddDocumentBinding<Role>("roles", d => d
            .WithCollectionName("roles")
            .WithBulkRepository());
    })
    .AddDatabase("ProductDB", db =>
    {
        db.OnCluster("primary");
        db.WithNamespacePrefix("catalog");
        db.AddDocumentBinding<Product>("products", d => d
            .WithCollectionName("products")
            .WithBulkRepository()
            .WithProjectionRepository());
    }));
```

### Benefits of Multi-Database Approach

1. **Logical Separation**: Different business domains can have separate databases
2. **Scalability**: Each database can be scaled independently
3. **Security**: Different access controls per database
4. **Performance**: Optimized indexing and sharding strategies per database
5. **Maintenance**: Independent backup and maintenance schedules

## 🏷️ Namespace resolution

Physical database and collection names are resolved by a scoped ordered pipeline of `INamespaceSegmentContributor` implementations. The library has **no first-class multi-tenancy / Tenant APIs**.

### Static prefix (registration-time)

```csharp
.AddDatabase("UserDB", db =>
{
    db.OnCluster("primary");
    db.WithNamespacePrefix("catalog"); // → catalog_UserDB
})
```

### Async prefix resolver (primary multi-tenant path)

For prefixes that must be loaded from storage, an HTTP API, or other async work, register an `INamespacePrefixResolver` on a **database** or **document binding**. Dilcore registers the type as scoped automatically.

```csharp
public sealed class TenantDatabasePrefixResolver : INamespacePrefixResolver
{
    private readonly ITenantStore _store; // your app service — not a Dilcore type

    public TenantDatabasePrefixResolver(ITenantStore store) => _store = store;

    public async Task<Result<string?>> ResolveAsync(
        NamespaceResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _store.GetCurrentAsync(cancellationToken);
        if (tenant is null)
            return Result.Fail<string?>("Tenant context is required.");

        return Result.Ok<string?>(tenant.Id);
    }
}

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("UserDB", db =>
    {
        db.OnCluster("primary");
        db.WithNamespacePrefix("catalog"); // optional static segment
        db.WithNamespacePrefixResolver<TenantDatabasePrefixResolver>(); // async segment
        db.AddDocumentBinding<User>("users", d => d
            .WithCollectionName("users")
            .WithNamespacePrefixResolver<FeatureCollectionPrefixResolver>()); // optional per-collection
    }));
```

Combined example: async `tenantA` + static `catalog` + logical `UserDB` → `tenantA_catalog_UserDB`.

### Cross-cutting contributor (escape hatch)

For prefixes that apply across many databases/bindings without per-builder registration, implement `INamespaceSegmentContributor` and register it with DI (`AddScoped` / `TryAddEnumerable`). Prefer `WithNamespacePrefixResolver<T>` when the prefix is scoped to one database or binding.

### How resolution works

1. Contributors run in `Order` ascending (descriptor async resolvers at 90, static `WithNamespacePrefix` at 100) and may each emit a segment (or `null` to skip).
2. Segments are joined with `_` and validated as a physical MongoDB name.
3. When a database/binding has an async prefix resolver, that resolution is not cached within the scope (so a changed tenant context cannot reuse a stale physical name).

Fail-closed behavior (require a prefix when missing) is an app policy inside your resolver — return `Result.Fail`.

## 📋 Usage Examples

### Basic Setup (from samples/MongoDb.WebApi.Sample)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB with Testcontainers
var mongoDbContainer = new MongoDbBuilder().Build();
await mongoDbContainer.StartAsync();
var connectionString = mongoDbContainer.GetConnectionString();

builder.Services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("SampleDB", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<WeatherForecast>("weather", d => d
            .WithCollectionName("weatherForecasts")
            .WithBulkRepository());
    }));

var app = builder.Build();

// API Endpoints
app.MapGet("/weather-forecasts", async (IGenericRepository<WeatherForecast> repository) =>
{
    var result = await repository.GetListAsync();
    return result.IsSuccess ? Results.Ok(result.ValueOrDefault) : Results.BadRequest(result.Errors);
});

app.MapPost("/weather-forecasts", async (IGenericRepository<WeatherForecast> repository, WeatherForecast forecast) =>
{
    var result = await repository.StoreAsync(forecast);
    return result.IsSuccess ? Results.Ok(result.ValueOrDefault) : Results.BadRequest(result.Errors);
});
```

### Entity Definition

```csharp
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary) : IDocumentEntity
{
    public Guid Id { get; set; }
    public long ETag { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

### Advanced configuration with custom namespace prefixes

```csharp
services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("TestDB1", db =>
    {
        db.OnCluster("primary");
        db.WithNamespacePrefix("env"); // static segment
        db.WithNamespacePrefixResolver<TenantDatabasePrefixResolver>(); // async multi-tenant segment
        db.AddDocumentBinding<TestEntity>("testEntities", d => d
            .WithCollectionName("testEntities")
            .WithSoftDelete()
            .WithBulkRepository()
            .WithProjectionRepository());
    }));
```

### Repository Usage Patterns

#### Basic CRUD Operations
```csharp
public class UserService
{
    private readonly IGenericRepository<User> _userRepository;

    public UserService(IGenericRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<User>> CreateUserAsync(User user)
    {
        return await _userRepository.StoreAsync(user);
    }

    public async Task<Result<User>> GetUserByIdAsync(Guid userId)
    {
        return await _userRepository.GetAsync(userId);
    }

    public async Task<Result<IReadOnlyList<User>>> GetActiveUsersAsync()
    {
        return await _userRepository.GetListAsync(x => !x.IsDeleted);
    }

    public async Task<Result<bool>> DeleteUserAsync(Guid userId, long eTag)
    {
        return await _userRepository.DeleteAsync(userId, eTag);
    }
}
```

#### Bulk Operations
```csharp
public class DataMigrationService
{
    private readonly IGenericBulkRepository<User> _bulkRepository;

    public DataMigrationService(IGenericBulkRepository<User> bulkRepository)
    {
        _bulkRepository = bulkRepository;
    }

    public async Task<Result<IReadOnlyList<User>>> MigrateUsersAsync(User[] users)
    {
        return await _bulkRepository.BulkStoreAsync(users);
    }

    public async Task<Result> CleanupInactiveUsersAsync()
    {
        return await _bulkRepository.BulkDeleteAsync(x => x.IsDeleted && x.UpdatedAt < DateTime.UtcNow.AddYears(-1));
    }
}
```

#### Projection Operations
```csharp
public class ReportingService
{
    private readonly IGenericProjectionRepository<User> _projectionRepository;

    public ReportingService(IGenericProjectionRepository<User> projectionRepository)
    {
        _projectionRepository = projectionRepository;
    }

    public async Task<Result<IReadOnlyList<UserSummary>>> GetUserSummariesAsync()
    {
        return await _projectionRepository.GetListAsync(
            filter: Builders<User>.Filter.Eq(x => x.IsDeleted, false),
            projection: x => new UserSummary
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                LastLoginDate = x.UpdatedAt
            });
    }
}

public class UserSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime LastLoginDate { get; set; }
}
```

## 🧪 Testing

The library includes comprehensive test suites demonstrating various usage patterns:

### Integration Tests
- **GenericRepositoryTests**: CRUD operations, filtering, soft delete
- **GenericBulkRepositoryTests**: Bulk insert and delete operations
- **GenericProjectionRepositoryTests**: Data projection and transformation
- **ServiceCollectionExtensionsTests**: DI container configuration and multi-database scenarios
- **MongoCollectionProviderTests**: Collection management and prefix resolution

### Unit Tests
- **DocumentEntityExtensionsTests**: Entity extension methods and utilities

### Test Infrastructure
The tests use Testcontainers for MongoDB to provide isolated, reproducible test environments:

```csharp
public abstract class BaseIntegrationTests
{
    protected static readonly MongoDbContainer MongoDbContainer = new MongoDbBuilder().Build();

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await MongoDbContainer.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await MongoDbContainer.DisposeAsync();
    }
}
```

## 🔧 Configuration Options

### Collection Options
```csharp
options.WithCollectionName("customName")           // Custom collection name
       .WithDatabaseName("customDb")               // Target database
       .WithSoftDelete()                           // Enable soft delete
       .WithIndexes(                               // Define indexes
           Builders<T>.IndexKeys.Ascending(x => x.Field1),
           Builders<T>.IndexKeys.Descending(x => x.Field2)
       );
```

### Repository Options
```csharp
repositoryOptions.WithBulkRepository()             // Enable bulk operations
                 .WithProjectionRepository();      // Enable projection operations
```

## 🚀 Getting Started

1. **Install the packages** (configure in Directory.Packages.props):
   ```xml
   <PackageVersion Include="MongoDB.Driver" Version="3.5.0" />
   <PackageVersion Include="FluentResults" Version="4.0.0" />
   <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.9" />
   ```

2. **Define your entities**:
   ```csharp
   public class MyEntity : IDocumentEntity
   {
       public Guid Id { get; set; }
       public long ETag { get; set; }
       public bool IsDeleted { get; set; }
       public DateTime CreatedAt { get; set; }
       public DateTime UpdatedAt { get; set; }
       
       // Your custom properties
       public string Name { get; set; }
       public string Description { get; set; }
   }
   ```

3. **Configure services**:
   ```csharp
   services.AddMongoDb(mongo => mongo
       .AddCluster("primary", c => c.UseConnectionString(connectionString))
       .AddDatabase("MyDatabase", db =>
       {
           db.OnCluster("primary");
           db.AddDocumentBinding<MyEntity>("myEntities", d => d
               .WithCollectionName("myEntities"));
       }));
   ```

4. **Use in your services**:
   ```csharp
   public class MyService
   {
       private readonly IGenericRepository<MyEntity> _repository;
       
       public MyService(IGenericRepository<MyEntity> repository)
       {
           _repository = repository;
       }
       
       public async Task<Result<MyEntity>> CreateAsync(MyEntity entity)
       {
           return await _repository.StoreAsync(entity);
       }
   }
   ```

## 📚 Additional Resources

- **Sample Application**: See `samples/MongoDb.WebApi.Sample` for a complete working example
- **Integration Tests**: Explore `test/Repositories.IntegrationTests` for comprehensive usage patterns
- **Unit Tests**: Check `test/UnitTests` for entity and extension testing examples

The Dilcore DocumentDB library provides a robust, scalable foundation for MongoDB-based applications with clean architecture, comprehensive testing, and flexible configuration options.