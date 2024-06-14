using Aeon.Application;
using FluentResults;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;
using System.Net;
using System.Net.Mime;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
//builder.Services.AddRedisOutputCache(builder.Configuration);
//builder.AddRedisOutputCache("cache");

var fallbackStrategyOptions = new FallbackStrategyOptions<HttpResponseMessage>
{
    FallbackAction = _ =>
    {
        return Outcome.FromResultAsValueTask(new HttpResponseMessage(HttpStatusCode.OK));
    },

    ShouldHandle = arguments => arguments.Outcome switch
    {
        { Exception: HttpRequestException } => PredicateResult.True(),
        { Exception: TimeoutRejectedException } => PredicateResult.True(),
        { Result: HttpResponseMessage response } when response.StatusCode == HttpStatusCode.InternalServerError =>
            PredicateResult.True(),
        { Result: HttpResponseMessage response } when response.StatusCode == HttpStatusCode.BadRequest =>
            PredicateResult.True(),
        _ => PredicateResult.False(),
    },
    OnFallback = _ =>
    {
        Console.WriteLine("Fallback!");
        return default;
    }
};

var retryStrategyOptions = new RetryStrategyOptions<HttpResponseMessage>
{
    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(r => r.StatusCode == HttpStatusCode.InternalServerError)
                .Handle<HttpRequestException>(),

    OnRetry = arguments =>
    {
        Console.WriteLine($"Retrying '{arguments.Outcome.Result?.StatusCode}'...");
        return default;
    },
    Delay = TimeSpan.FromMilliseconds(400),
    BackoffType = DelayBackoffType.Constant,
    MaxRetryAttempts = 3,
};
builder.Services.AddSingleton(new ResiliencePipelineBuilder<HttpResponseMessage>()
            //.AddFallback(fallbackStrategyOptions)
            .AddRetry(retryStrategyOptions)
            .AddTimeout(TimeSpan.FromSeconds(20))
            .Build());
//services.AddHostedService<Worker>();
builder.Services.AddCore();
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//app.MapDefaultEndpoints();

app.Run();

public partial class Program
{
    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                                                                        retryAttempt)));
    }
}
public static class WaitAndRetryExtensions
{
    public static AsyncRetryPolicy CreateWaitAndRetryPolicy(IEnumerable<TimeSpan> sleepsBeetweenRetries)
    {
        return Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                sleepDurations: sleepsBeetweenRetries,
                onRetry: (_, span, retryCount, _) =>
                {
                    var previousBackgroundColor = Console.BackgroundColor;
                    var previousForegroundColor = Console.ForegroundColor;

                    Console.BackgroundColor = ConsoleColor.Yellow;
                    Console.ForegroundColor = ConsoleColor.Black;

                    Console.Out.WriteLineAsync($" ***** {DateTime.Now:HH:mm:ss} | " +
                        $"Retentativa: {retryCount} | " +
                        $"Tempo de Espera em segundos: {span.TotalSeconds} **** ");

                    Console.BackgroundColor = previousBackgroundColor;
                    Console.ForegroundColor = previousForegroundColor;
                });
    }
}

public static class PollyContextExtensions
{
    private static readonly string LoggerKey = "ILogger";

    public static Context WithLogger<T>(this Context context, ILogger logger)
    {
        context[LoggerKey] = logger;
        return context;
    }

    public static ILogger GetLogger(this Context context)
    {
        if (context.TryGetValue(LoggerKey, out object logger))
        {
            return logger as ILogger;
        }

        return null;
    }
}

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception, "Exception occurred: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Server error"
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks("/health");

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
    public static IServiceCollection AddRedisOutputCache(this IServiceCollection services, IConfiguration configuration)
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