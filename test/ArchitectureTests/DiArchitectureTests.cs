using System.Reflection;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.MongoDB.ArchitectureTests;

public class DiArchitectureTests
{
    [Test]
    public void AddMongoDb_IsTheOnlyPublicDiEntryPoint()
    {
        // Arrange
        var extensions = typeof(ServiceCollectionExtensions);

        // Act
        var methods = extensions.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.DeclaringType == extensions)
            .Select(m => m.Name)
            .Distinct()
            .ToList();

        // Assert
        methods.ShouldBe(["AddMongoDb"]);
    }

    [Test]
    public void AddMongoDb_AcceptsImmutableBuilderCallback()
    {
        // Arrange
        var method = typeof(ServiceCollectionExtensions).GetMethod(nameof(ServiceCollectionExtensions.AddMongoDb));

        // Act
        var parameters = method?.GetParameters();

        // Assert
        method.ShouldNotBeNull();
        parameters!.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(IServiceCollection));
        parameters[1].ParameterType.ShouldBe(typeof(Action<DependencyInjection.IMongoDbBuilder>));
    }
}