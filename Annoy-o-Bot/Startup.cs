using Annoy_o_Bot;
using Annoy_o_Bot.CosmosDB;
using Annoy_o_Bot.GitHub;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Annoy_o_Bot;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddScoped<IGitHubAppInstallation, GitHubAppInstallation>();
        builder.Services.AddScoped<ICosmosClientWrapper, CosmosClientWrapper>();
    }
}