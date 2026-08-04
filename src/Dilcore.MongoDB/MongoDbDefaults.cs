namespace Dilcore.MongoDB;

internal static class MongoDbDefaults
{
    internal const int MaxConnectionPoolSize = 25;
    internal const int MaxConnectionIdleTimeInMinutes = 5;
    internal const char DefaultNamespaceSeparator = '_';
    internal const int NamespaceCacheCapacity = 256;
}
