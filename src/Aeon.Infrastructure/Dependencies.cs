using Aeon.Domain.Repositories;
using Aeon.Infrastructure.Contexts;
using Aeon.Infrastructure.ExternalServices;
using Aeon.Infrastructure.Repositories;
using Aeon.Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Aeon.Infrastructure;

public static class Dependencies
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("EntFra"));
        services.AddScoped<IRepository, Repository>();

        services.AddPollyResilience();
        services.AddScoped<IHttpGitHubClientFactory, HttpGitHubClientFactory>();
        services.AddHttpClient("Github", client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            client.DefaultRequestHeaders.Add(HttpRequestHeader.Accept.ToString(), "application/json");
            client.DefaultRequestHeaders.Add("Timeout", "1000000000");
        });

        return services;
    }
}