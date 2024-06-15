using Aeon.Api.Extensions;
using Aeon.Api.Handlers;
using Aeon.Application;
using Polly;
using Polly.Retry;
using Serilog;
try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddSerilogLogBuilder(builder.Configuration, "API.Observability");
    Log.Information("Starting API");

//    Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

    builder.AddServiceDefaults();
    builder.AddRedisOutputCache();
    builder.AddPollyRetryPolicy();
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

    app.AddRedisHealthCheck();
    app.MapDefaultEndpoints();

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

