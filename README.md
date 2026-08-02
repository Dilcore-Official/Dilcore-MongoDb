# Dilcore DocumentDB Library

A comprehensive .NET library providing a clean, abstracted interface for working with MongoDB databases. The library implements the Repository pattern with support for multiple databases, custom prefixes, and various repository types including generic, bulk, and projection repositories.

> **v2 roadmap:** See [ROADMAP.md](ROADMAP.md) and [roadmap issues](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues?q=is%3Aissue+label%3Aroadmap) for the professional open-source redesign (milestones M0–M9). v2 renames the product to **Dilcore MongoDB** ([ADR 0001](docs/adr/0001-package-naming.md)).

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
services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
{
    // Database 1: User Management
    builder.AddDatabase("UserDB", db =>
    {
        db.AddGenericRepository<User>(options => 
            options.WithCollectionName("users")
                   .WithDatabaseName("UserDB"));
        
        db.AddGenericRepository<Role>(repositoryOptions => 
            repositoryOptions.WithBulkRepository(),
            collectionOptions => 
            collectionOptions.WithCollectionName("roles")
                             .WithDatabaseName("UserDB"));
    });

    // Database 2: Product Catalog
    builder.AddDatabase("ProductDB", db =>
    {
        db.AddCustomDatabasePrefixResolver<ProductDbPrefixProvider>();
        
        db.AddGenericRepository<Product>(repositoryOptions => 
            repositoryOptions.WithBulkRepository()
                            .WithProjectionRepository(),
            collectionOptions => 
            collectionOptions.WithCollectionName("products")
                             .WithDatabaseName("ProductDB"));
    });
});
```

### Benefits of Multi-Database Approach

1. **Logical Separation**: Different business domains can have separate databases
2. **Scalability**: Each database can be scaled independently
3. **Security**: Different access controls per database
4. **Performance**: Optimized indexing and sharding strategies per database
5. **Maintenance**: Independent backup and maintenance schedules

## 🏷️ Prefix Resolvers

Prefix resolvers provide dynamic naming capabilities for databases and collections, enabling multi-tenancy, environment-specific naming, and organizational patterns.

### Database Prefix Resolver (`IDocumentDatabasePrefixProvider`)

Controls the naming of MongoDB databases:

```csharp
public interface IDocumentDatabasePrefixProvider : IDocumentPrefixProvider
{
    Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default);
}
```

**Purpose and Use Cases:**
- **Multi-tenancy**: Different prefixes for different tenants (`tenant1_UserDB`, `tenant2_UserDB`)
- **Environment separation**: Environment-specific prefixes (`dev_UserDB`, `prod_UserDB`)
- **Regional deployment**: Geographic prefixes (`us_UserDB`, `eu_UserDB`)
- **Version management**: Version-specific databases (`v1_UserDB`, `v2_UserDB`)

**Example Implementation:**
```csharp
public class TenantDatabasePrefixProvider : IDocumentDatabasePrefixProvider
{
    private readonly ITenantContext _tenantContext;

    public TenantDatabasePrefixProvider(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.GetCurrentTenantId();
        return Task.FromResult(Result.Ok($"tenant_{tenantId}"));
    }
}
```

### Collection Prefix Resolver (`IDocumentCollectionPrefixProvider`)

Controls the naming of MongoDB collections within databases:

```csharp
public interface IDocumentCollectionPrefixProvider : IDocumentPrefixProvider
{
    Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default);
}
```

**Purpose and Use Cases:**
- **Feature flags**: Different collection versions (`beta_users`, `stable_users`)
- **A/B testing**: Separate collections for different test groups
- **Data migration**: Temporary prefixes during migrations (`temp_users`, `migrated_users`)
- **Organizational structure**: Department or team-specific prefixes (`hr_employees`, `it_employees`)

**Example Implementation:**
```csharp
public class FeatureFlagCollectionPrefixProvider : IDocumentCollectionPrefixProvider
{
    private readonly IFeatureFlagService _featureFlagService;

    public FeatureFlagCollectionPrefixProvider(IFeatureFlagService featureFlagService)
    {
        _featureFlagService = featureFlagService;
    }

    public Task<Result<string>> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var useNewSchema = _featureFlagService.IsEnabled("UseNewUserSchema");
        var prefix = useNewSchema ? "v2" : "v1";
        return Task.FromResult(Result.Ok(prefix));
    }
}
```

### How Prefix Resolution Works

1. **Database Resolution**: `{DatabasePrefix}_{DatabaseName}` → `tenant1_UserDB`
2. **Collection Resolution**: `{CollectionPrefix}_{CollectionName}` → `v2_users`
3. **Final MongoDB Path**: `tenant1_UserDB.v2_users`

### Default Behavior

If no custom prefix providers are registered, the library uses default implementations that return empty strings, resulting in the original database and collection names.

## 📋 Usage Examples

### Basic Setup (from samples/MongoDb.WebApi.Sample)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB with Testcontainers
var mongoDbContainer = new MongoDbBuilder().Build();
await mongoDbContainer.StartAsync();
var connectionString = mongoDbContainer.GetConnectionString();

builder.Services.AddMongoDb(configure => configure.UseConnectionString(connectionString), dbContainer =>
{
    dbContainer.AddDatabase("SampleDB", db =>
    {
        db.AddGenericRepository<WeatherForecast>(
            registerRepositoryAction: register => register.WithBulkRepository(),
            options =>
            {
                options.WithCollectionName("weatherForecasts")
                       .WithDatabaseName("SampleDB");
            });
    });
});

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

### Advanced Configuration with Custom Prefixes

```csharp
services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
{
    builder.AddDatabase("TestDB1", db =>
    {
        // Custom database prefix for multi-tenancy
        db.AddCustomDatabasePrefixResolver<TenantDatabasePrefixProvider>();
        
        // Custom collection prefix for feature flags
        db.AddCustomCollectionPrefixResolver<FeatureFlagCollectionPrefixProvider>();

        // Register repository with all capabilities
        db.AddGenericRepository<TestEntity>(
            repositoryOptions => repositoryOptions
                .WithBulkRepository()
                .WithProjectionRepository(),
            collectionOptions => collectionOptions
                .WithCollectionName("testEntities")
                .WithDatabaseName("TestDB1")
                .WithSoftDelete()
                .WithIndexes(
                    Builders<TestEntity>.IndexKeys.Ascending(x => x.Name),
                    Builders<TestEntity>.IndexKeys.Descending(x => x.CreatedAt)
                ));
    });
});
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
   services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
   {
       builder.AddDatabase("MyDatabase", db =>
       {
           db.AddGenericRepository<MyEntity>(options => 
               options.WithCollectionName("myEntities")
                      .WithDatabaseName("MyDatabase"));
       });
   });
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