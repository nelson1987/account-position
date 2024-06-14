using Aeon.Application.Features.Positions.AddPositions;
using Aeon.Infrastructure;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Aeon.Application;

public static class Dependencies 
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services
            .AddApplication()
            .AddInfrastructure();
        return services;
    }

    private static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAddPositionsHandler, AddPositionsHandler>();
        services.AddScoped<IValidator<AddPositionsCommand>, AddPositionsValidator>();
        return services; 
    }
}
