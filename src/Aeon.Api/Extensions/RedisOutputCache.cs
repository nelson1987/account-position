using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Mime;

namespace Aeon.Api.Extensions;
public static class RedisOutputCache
{
    public static IHostApplicationBuilder AddRedisOutputCache(this IHostApplicationBuilder builder)
    {
        builder.Services.AddRedisOutputCache(builder.Configuration);
        return builder;
    }
    private static IServiceCollection AddRedisOutputCache(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("ConexaoRedis")!;
        services.AddStackExchangeRedisCache(options =>
        {
            options.InstanceName = "Redis-Dev";
            options.Configuration = connectionString;
        });
        services.AddHealthChecks()
                .AddRedis(connectionString);
        return services;
    }

    public static IApplicationBuilder AddRedisHealthCheck(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/redisState",
            new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    var jsonresult = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        statusApplication = report.Status.ToString(),
                        healthChecks = report.Entries.Select(x => new
                        {
                            checkName = x.Key,
                            state = Enum.GetName(typeof(HealthStatus), x.Value.Status)
                        })
                    });
                    context.Response.ContentType = MediaTypeNames.Application.Json;
                    await context.Response.WriteAsync(jsonresult);
                }
            });
        return app;
    }
}
