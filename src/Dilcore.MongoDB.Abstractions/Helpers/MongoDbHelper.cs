using System.Security.Cryptography;

namespace Dilcore.MongoDB.Abstractions.Helpers;

public static class MongoDbHelper
{
    /// <summary>
    /// Generates a collision-resistant 64-bit concurrency token. The empty token remains 0.
    /// </summary>
    public static long GenerateEtag()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToInt64(bytes);
        return value == 0 ? 1 : value;
    }
}
