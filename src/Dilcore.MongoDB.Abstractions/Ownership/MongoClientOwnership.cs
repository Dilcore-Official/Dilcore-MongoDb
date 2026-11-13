namespace Dilcore.MongoDB.Abstractions.Ownership;

/// <summary>
/// Indicates whether Dilcore created the <c>MongoDB.Driver.IMongoClient</c> or adopted an app-supplied instance.
/// </summary>
public enum MongoClientOwnership
{
    /// <summary>
    /// The library constructed the client and disposes it on shutdown.
    /// </summary>
    LibraryOwned = 0,

    /// <summary>
    /// The application supplied the client; Dilcore does not dispose it.
    /// </summary>
    ExternalOwned = 1
}