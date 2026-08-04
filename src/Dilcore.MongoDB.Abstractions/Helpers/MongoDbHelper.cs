namespace Dilcore.MongoDB.Abstractions.Helpers;

public static class MongoDbHelper
{
    public static long GenerateEtag() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
