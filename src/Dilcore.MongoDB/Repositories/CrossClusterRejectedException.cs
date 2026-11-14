namespace Dilcore.MongoDB.Repositories;

internal sealed class CrossClusterRejectedException(string clusterName)
    : InvalidOperationException($"Binding targets cluster '{clusterName}', which is outside the current transaction.");