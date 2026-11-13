using Dilcore.MongoDB.Abstractions.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dilcore.MongoDB.NewtonsoftJson;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Newtonsoft.Json document store facades. Requires <c>AddMongoDb</c> first.
    /// </summary>
    public static IServiceCollection AddNewtonsoftJsonDocuments(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(IJsonDocumentStoreFactory)))
        {
            throw new InvalidOperationException(
                "AddNewtonsoftJsonDocuments requires AddMongoDb to be called first.");
        }

        services.TryAddScoped<INewtonsoftJsonDocumentStoreFactory, NewtonsoftJsonDocumentStoreFactory>();
        return services;
    }
}
