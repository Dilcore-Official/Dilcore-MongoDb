using System.Reflection;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.MongoDB.ArchitectureTests;

public class DiArchitectureTests
{
    [Test]
    public void AddMongoDb_IsTheOnlyPublicDiEntryPoint()
    {
        var extensions = typeof(ServiceCollectionExtensions);
        var methods = extensions.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.DeclaringType == extensions)
            .Select(m => m.Name)
            .Distinct()
            .ToList();

        methods.ShouldBe(["AddMongoDb"]);
    }

    [Test]
    public void AddMongoDb_AcceptsImmutableBuilderCallback()
    {
        var method = typeof(ServiceCollectionExtensions).GetMethod(nameof(ServiceCollectionExtensions.AddMongoDb));
        method.ShouldNotBeNull();

        var parameters = method!.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(IServiceCollection));
        parameters[1].ParameterType.ShouldBe(typeof(Action<DependencyInjection.IMongoDbBuilder>));
    }
}
