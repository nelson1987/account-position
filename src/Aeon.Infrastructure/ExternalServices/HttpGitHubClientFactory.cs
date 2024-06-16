using Polly;

namespace Aeon.Infrastructure.ExternalServices;
public interface IHttpGitHubClientFactory
{
    Task<HttpResponseMessage> BuscarApi();
}
public class HttpGitHubClientFactory : IHttpGitHubClientFactory
{
    private readonly HttpClient _client;
    private readonly AsyncPolicy _policy;
    public HttpGitHubClientFactory(IHttpClientFactory factory, AsyncPolicy policy)
    {
        _client = factory.CreateClient("Github");
        _policy = policy;
    }
    public async Task<HttpResponseMessage> BuscarApi()
    {
        //var url = $"{_client.BaseAddress}/anuncios/getAll?skip={skip}&take={take}";
        //return await _policy.ExecuteAsync(() => _client.GetFromJsonAsync<List<AnunciosResponse>>(url));
        //return await _client.GetAsync("/someapi");
        return await _policy.ExecuteAsync(() => _client.GetAsync("/someapi"));
    }
}