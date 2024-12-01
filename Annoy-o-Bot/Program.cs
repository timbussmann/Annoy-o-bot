using Annoy_o_Bot.GitHub.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddScoped<IGitHubApi, GitHubApi>();
    })
    .Build();

host.Run();
