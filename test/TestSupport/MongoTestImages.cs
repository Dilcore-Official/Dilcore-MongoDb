using Testcontainers.MongoDb;

namespace Dilcore.MongoDB.TestSupport;

/// <summary>
/// Single pin for MongoDB Testcontainers images used by integration tests and benchmarks.
/// Set <see cref="ImageTagEnvironmentVariable"/> to a major.minor tag such as <c>7.0</c> to
/// run the same tests against another official <c>mongo</c> image. The default is <see cref="DefaultTag"/>.
/// </summary>
public static class MongoTestImages
{
    /// <summary>Major.minor tag used when <see cref="ImageTagEnvironmentVariable"/> is unset.</summary>
    public const string DefaultTag = "9.0";

    /// <summary>Environment variable that overrides <see cref="DefaultTag"/>.</summary>
    public const string ImageTagEnvironmentVariable = "MONGODB_IMAGE_TAG";

    /// <summary>Official image reference, <c>mongo:{tag}</c>, for the resolved tag.</summary>
    public static string Mongo => BuildImage(ReadTag());

    /// <summary>
    /// Starts one <c>mongod</c> with no root user.
    /// The official image otherwise bootstraps a temporary process and then starts again with authorization.
    /// That second bind can fail with address already in use, which fails the container before the test runs.
    /// </summary>
    /// <returns>A standalone MongoDB container.</returns>
    public static MongoDbContainer CreateStandalone()
        => new MongoDbBuilder(Mongo).WithUsername(string.Empty).WithPassword(string.Empty).Build();

    /// <summary>
    /// Starts one <c>mongod</c> as a single-node replica set so multi-document transactions can run.
    /// </summary>
    /// <param name="replicaSetName">Replica set name passed to the image entrypoint.</param>
    /// <returns>A replica-set MongoDB container.</returns>
    public static MongoDbContainer CreateReplicaSet(string replicaSetName = "rs0")
        => new MongoDbBuilder(Mongo).WithReplicaSet(replicaSetName).Build();

    private static string? ReadTag()
        => Environment.GetEnvironmentVariable(ImageTagEnvironmentVariable);

    private static string BuildImage(string? tag)
    {
        var resolved = string.IsNullOrWhiteSpace(tag) ? DefaultTag : tag.Trim();

        return $"mongo:{resolved}";
    }
}