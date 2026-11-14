using NUnit.Framework;

namespace Dilcore.MongoDB.TestSupport;

/// <summary>
/// Base for fixtures that run a documentation example against a clean MongoDB.
/// Each test starts with every application database dropped, so examples do not share data.
/// </summary>
public abstract class ExampleIntegrationTests : BaseIntegrationTests
{
    private const string DropApplicationDatabases =
        "db.getMongo().getDBNames().filter(n => !['admin','config','local'].includes(n))" +
        ".forEach(n => db.getSiblingDB(n).dropDatabase())";

    /// <summary>The connection string the documentation examples call <c>connectionString</c>.</summary>
    protected string connectionString => MongoDbContainer.GetConnectionString();

    /// <summary>Drops every application database before each test.</summary>
    [SetUp]
    public async Task ResetDatabasesAsync()
    {
        await MongoDbContainer.ExecScriptAsync(DropApplicationDatabases);
    }
}