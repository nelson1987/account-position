using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

namespace Aeon.Infrastructure.Resilience;

public static class PollyExtensions
{
    public static void AddPollyResilience(this IServiceCollection services)
    {
        services.AddSingleton<AsyncPolicy>(CreateWaitAndRetryPolicy(new[]
        {
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(10)
            }));
    }
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