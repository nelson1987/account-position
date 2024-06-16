using Aeon.Domain.Entities;
using Aeon.Domain.Repositories;
using Aeon.Infrastructure.ExternalServices;
using Aeon.Infrastructure.Repositories;
using FluentResults;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Aeon.Application.Features.Positions.AddPositions;

public interface IAddPositionsHandler
{
    Task<Result<AddPositionsResponse>> Handler(AddPositionsCommand request, CancellationToken cancellationToken = default);
}

public class AddPositionsHandler : IAddPositionsHandler
{
    private readonly ILogger<Repository> _logger;
    private readonly IValidator<AddPositionsCommand> _validator;
    private readonly IRepository _repository;
    private readonly IHttpGitHubClientFactory _httpClientFactory;

    public AddPositionsHandler(ILogger<Repository> logger, IValidator<AddPositionsCommand> validator, IRepository repository, IHttpGitHubClientFactory httpClientFactory)
    {
        _logger = logger;
        _validator = validator;
        _repository = repository;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<AddPositionsResponse>> Handler(AddPositionsCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"{nameof(Handler)}: {request.ToString()}");
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            //return Result.Fail(validationResult.Errors);
            return Result.Fail("erro ao validar");
        }

        //var response = await _pipeline.ExecuteAsync(
        //    async token =>
        //    {
        //        //await Task.Delay(5000, token);
        //        //return
        await _httpClientFactory.BuscarApi();
        //        // This causes the action fail, thus using the fallback strategy above
        //        //return new HttpResponseMessage(HttpStatusCode.OK);
        //        return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        //    },
        //    CancellationToken.None);
        //_logger.LogInformation($"Response: {response.StatusCode}");

        var product = request.MapTo<Produto>();
        product.isActive = true;
        await _repository.Inserir(product);

        return Result.Ok(product.MapTo<AddPositionsResponse>());
    }
}
