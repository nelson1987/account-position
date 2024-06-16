using Aeon.Application.Features.Positions.AddPositions;
using Aeon.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Aeon.Api.Controllers
{
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
        public PositionsController(ILogger<PositionsController> logger, IAddPositionsHandler addPositionsHandler, IRepository repository)
        {
            _logger = logger;
            _addPositionsHandler = addPositionsHandler;
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"{nameof(Get)}");
            using (Activity activity = Telemetry.LoginActivitySource.StartActivity(nameof(Get)))
            {
                var listagem = await _repository.Buscar(cancellationToken);
                return Ok(listagem);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"{nameof(Post)}");
            var handle = await _addPositionsHandler.Handler(new AddPositionsCommand("First", "Last", 10.00M), cancellationToken);
            return Created();
        }
    }
}
