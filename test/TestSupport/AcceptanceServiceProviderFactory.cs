using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.MongoDB.TestSupport;

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
