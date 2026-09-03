using App = Deployment.Domain.Entities.Application;
using Deployment.Domain.Entities;

namespace Deployment.Application.Interfaces;

/// <summary>
/// Centralizes application/environment/target lookup so every service
/// throws the same not-found errors instead of re-implementing the chain.
/// </summary>
public interface ITargetResolver
{
    Task<App> ResolveApplicationAsync(string applicationName, CancellationToken ct = default);

    Task<(App Application, AppEnvironment Environment, DeploymentTarget Target)> ResolveTargetAsync(
        string applicationName, string environmentName, string targetName, CancellationToken ct = default);
}
