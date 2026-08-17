using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Infrastructure;

public static class AcceptanceServiceProviderFactory
{
    public static ServiceProvider Create(IServiceCollection services)
    {
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }
}
