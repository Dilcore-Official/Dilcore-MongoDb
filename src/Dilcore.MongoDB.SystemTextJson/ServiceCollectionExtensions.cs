using Dilcore.MongoDB.Abstractions.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dilcore.MongoDB.SystemTextJson;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers System.Text.Json document store facades. Requires <c>AddMongoDb</c> first.
    /// </summary>
    public static IServiceCollection AddSystemTextJsonDocuments(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(IJsonDocumentStoreFactory)))
        {
            throw new InvalidOperationException(
                "AddSystemTextJsonDocuments requires AddMongoDb to be called first.");
        }

        services.TryAddScoped<ISystemTextJsonDocumentStoreFactory, SystemTextJsonDocumentStoreFactory>();
        return services;
    }
}
