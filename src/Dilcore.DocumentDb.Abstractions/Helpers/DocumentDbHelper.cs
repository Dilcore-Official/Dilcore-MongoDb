namespace Dilcore.DocumentDb.Abstractions.Helpers;

public class DocumentDbHelper
{
    public static long GenerateEtag() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}