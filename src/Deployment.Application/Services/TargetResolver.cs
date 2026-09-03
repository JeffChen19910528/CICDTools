using App = Deployment.Domain.Entities.Application;
using Deployment.Application.Interfaces;
using Deployment.Domain.Entities;

namespace Deployment.Application.Services;

public class TargetResolver(IApplicationRepository appRepo) : ITargetResolver
{
    public async Task<App> ResolveApplicationAsync(string applicationName, CancellationToken ct = default)
        => await appRepo.GetByNameAsync(applicationName, ct)
            ?? throw new InvalidOperationException($"Application '{applicationName}' not found.");

    public async Task<(App Application, AppEnvironment Environment, DeploymentTarget Target)> ResolveTargetAsync(
        string applicationName, string environmentName, string targetName, CancellationToken ct = default)
    {
        var app = await ResolveApplicationAsync(applicationName, ct);
        var env = await appRepo.GetEnvironmentAsync(app.Id, environmentName, ct)
            ?? throw new InvalidOperationException($"Environment '{environmentName}' not found.");
        var target = await appRepo.GetTargetAsync(env.Id, targetName, ct)
            ?? throw new InvalidOperationException($"Target '{targetName}' not found.");
        return (app, env, target);
    }
}
