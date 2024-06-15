using Aeon.Application.Features.Positions.AddPositions;
using Aeon.Domain.Repositories;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;
using Serilog;
using StackExchange.Redis;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Aeon.Api.Controllers
{

    public class ResultadoContador
    {
        public int ValorAtual { get; set; }
        public string Local { get; set; }
        public string Kernel { get; set; }
        public string TargetFramework { get; set; }
        public string MensagemFixa { get; set; }
        public object MensagemVariavel { get; set; }
    }
    public static class Telemetry
    {
        //...

        // Name it after the service name for your app.
        // It can come from a config file, constants file, etc.
        public static readonly ActivitySource LoginActivitySource = new("Login");

        //...
    }
    [ApiController]
    [Route("[controller]")]
    public class PositionsController : ControllerBase
    {
        private readonly ILogger<PositionsController> _logger;
        private readonly IAddPositionsHandler _addPositionsHandler;
        private readonly IRepository _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;
        public PositionsController(ILogger<PositionsController> logger, IAddPositionsHandler addPositionsHandler, IRepository repository, IHttpClientFactory httpClientFactory, ResiliencePipeline<HttpResponseMessage> pipeline)
        {
            _logger = logger;
            _addPositionsHandler = addPositionsHandler;
            _repository = repository;
            _httpClientFactory = httpClientFactory;
            _pipeline = pipeline;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
        {
            using (Activity activity = Telemetry.LoginActivitySource.StartActivity(nameof(Get)))
            {
                var listagem = await _repository.Buscar(cancellationToken);

                var client = _httpClientFactory.CreateClient("GitHub");

                var response = await _pipeline.ExecuteAsync(
                    async token =>
                    {
                        //await Task.Delay(5000, token);
                        //return await client.GetAsync("/someapi");
                        // This causes the action fail, thus using the fallback strategy above
                        //return new HttpResponseMessage(HttpStatusCode.OK);
                        return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                    },
                    CancellationToken.None);

                Log.Information($"Response: {response.StatusCode}");

                return Ok(listagem);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(CancellationToken cancellationToken = default)
        {
            var handle = await _addPositionsHandler.Handler(new AddPositionsCommand("First", "Last", 10.00M), cancellationToken);
            return Created();
        }
    }
}
