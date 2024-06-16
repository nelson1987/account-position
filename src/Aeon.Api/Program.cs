using Aeon.Api.Extensions;
using Aeon.Api.Handlers;
using Aeon.Application;
using Serilog;
try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.AddSerilogLogBuilder("API.Observability");
    Log.Information("Starting API");
    //Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

    builder.AddServiceDefaults();
    builder.AddRedisOutputCache();
    builder.AddPollyRetryPolicy();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    //services.AddHostedService<Worker>();
    builder.Services.AddCore();
    //================================
    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

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

