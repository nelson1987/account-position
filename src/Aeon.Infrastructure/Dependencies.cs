using Aeon.Domain.Repositories;
using Aeon.Infrastructure.Contexts;
using Aeon.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Aeon.Infrastructure;

public static class Dependencies
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("EntFra"));
        services.AddScoped<IRepository, Repository>();
        return services;
    }
}
