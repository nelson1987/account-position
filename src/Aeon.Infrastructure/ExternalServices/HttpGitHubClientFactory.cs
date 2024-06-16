using Microsoft.Extensions.Logging;
using Polly;
using System.Net;

namespace Aeon.Infrastructure.ExternalServices;
public interface IHttpGitHubClientFactory
{
    Task<HttpResponseMessage> BuscarApi();
}
public class HttpGitHubClientFactory : IHttpGitHubClientFactory
{
    private readonly ILogger<HttpGitHubClientFactory> _logger;
    private readonly HttpClient _client;
    private readonly ResiliencePipeline<HttpResponseMessage> _policy;
    public HttpGitHubClientFactory(ILogger<HttpGitHubClientFactory> logger, IHttpClientFactory factory, ResiliencePipeline<HttpResponseMessage> policy)
    {
        _client = factory.CreateClient("Github");
        _policy = policy;
        _logger = logger;
    }
    public async Task<HttpResponseMessage> BuscarApi()
    {
        var response = await _policy.ExecuteAsync(
            async token =>
            {
                //await await _client.GetAsync("/someapi")
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            },
            CancellationToken.None);
        _logger.LogInformation($"Response: {response.StatusCode}");
        return response;
    }
}