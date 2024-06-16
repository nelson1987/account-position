using Polly;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;
using Serilog;
using System.Net;

namespace Aeon.Api.Extensions;
public static class PollyRetryPolicy
{
    public static IHostApplicationBuilder AddPollyRetryPolicy(this IHostApplicationBuilder builder)
    {

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
                { Result: HttpResponseMessage response }
                    when response.StatusCode == HttpStatusCode.InternalServerError ||
                         response.StatusCode == HttpStatusCode.BadRequest =>
                    PredicateResult.True(),
                _ => PredicateResult.False(),
            },
            OnFallback = _ =>
            {
                Log.Information("Fallback!");
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
                Log.Information($"Retrying '{arguments.Outcome.Result?.StatusCode}'...");
                return default;
            },
            Delay = TimeSpan.FromMilliseconds(400),
            BackoffType = DelayBackoffType.Exponential,
            MaxRetryAttempts = 3,
        };
        builder!.Services.AddSingleton(new ResiliencePipelineBuilder<HttpResponseMessage>()
                    .AddFallback(fallbackStrategyOptions)
                    .AddRetry(retryStrategyOptions)
                    .AddTimeout(TimeSpan.FromSeconds(20))
                    .Build());
        return builder;
    }
}