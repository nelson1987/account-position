using Aeon.Application;
using Elastic.Channels;
using Elastic.CommonSchema.Serilog;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Ingest.Elasticsearch;
using Elastic.Serilog.Sinks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Polly;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Filters;
using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Http.Features;
try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddSerilog(builder.Configuration, "API Observability");
    Log.Information("Starting API");

    Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
    //builder.Services.AddOpenTelemetry().WithTracing(builder => builder.AddOtlpExporter()
    //    .AddSource("Login")
    //    .AddAspNetCoreInstrumentation()
    //    .AddOtlpExporter()
    //    .ConfigureResource(resource =>
    //        resource.AddService(
    //            serviceName: "Login"))
    //);
    //builder.AddServiceDefaults();
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
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.Information("Server Shutting down...");
    Log.CloseAndFlush();
}
public partial class Program
{
    //static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    //{
    //    return HttpPolicyExtensions
    //        .HandleTransientHttpError()
    //        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
    //        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
    //                                                                    retryAttempt)));
    //}
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

//public static class PollyContextExtensions
//{
//    private static readonly string LoggerKey = "ILogger";

//    public static Context WithLogger<T>(this Context context, ILogger logger)
//    {
//        context[LoggerKey] = logger;
//        return context;
//    }

//    public static ILogger GetLogger(this Context context)
//    {
//        if (context.TryGetValue(LoggerKey, out object logger))
//        {
//            return logger as ILogger;
//        }

//        return null;
//    }
//}
public static class SerilogExtension
{
    public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder, IConfiguration configuration, string applicationName)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", $"{applicationName} - {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}")
            .Enrich.WithCorrelationId()
            .Enrich.WithExceptionDetails()
            .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.StaticFiles"))
            .WriteTo.Async(writeTo => writeTo.Elasticsearch(new[] { new Uri(configuration["ElasticsearchSettings:uri"]) }, opts => 
            {
                opts.DataStream = new DataStreamName("logs", configuration["ElasticsearchSettings:defaultIndex"], "demo");
                //opts.BootstrapMethod = BootstrapMethod.Failure;
            }))
            //{
            //    opts.TypeName = null,
            //    opts.AutoRegisterTemplate = true,
            //    opts.IndexFormat = configuration["ElasticsearchSettings:defaultIndex"],
            //    opts.BatchAction = ElasticOpType.Create,
            //    opts.CustomFormatter = new EcsTextFormatter(),
            //    opts.ModifyConnectionSettings = x => x.BasicAuthentication(configuration["ElasticsearchSettings:username"], configuration["ElasticsearchSettings:password"])
            //}))
            .WriteTo.Async(writeTo => writeTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"))
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Host.UseSerilog(Log.Logger, true);

        return builder;
    }

    public static WebApplication UseSerilog(this WebApplication app)
    {
        app.UseMiddleware<ErrorHandlingMiddleware>();
        app.UseSerilogRequestLogging(opts =>
        {
            opts.EnrichDiagnosticContext = LogEnricherExtensions.EnrichFromRequest;
        });

        return app;
    }
}
public static class LogEnricherExtensions
{
    public static void EnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        diagnosticContext.Set("UserName", httpContext?.User?.Identity?.Name);
        diagnosticContext.Set("ClientIP", httpContext?.Connection?.RemoteIpAddress?.ToString());
        diagnosticContext.Set("UserAgent", httpContext?.Request?.Headers?["User-Agent"].FirstOrDefault());
        diagnosticContext.Set("Resource", httpContext?.GetMetricsCurrentResourceName());
    }

    public static string? GetMetricsCurrentResourceName(this HttpContext httpContext)
    {
        if (httpContext == null)
            throw new ArgumentNullException(nameof(httpContext));

        var endpoint = httpContext?.Features?.Get<IEndpointFeature>()?.Endpoint;

        return endpoint?.Metadata?.GetMetadata<EndpointNameMetadata>()?.EndpointName;
    }
}
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        Log.Error(exception, "Error");

        var code = HttpStatusCode.InternalServerError;

        var result = System.Text.Json.JsonSerializer.Serialize(new { error = exception?.Message });

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        return context.Response.WriteAsync(result);
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