using Aeon.Domain.Entities;
using Aeon.Domain.Repositories;
using FluentResults;
using FluentValidation;

namespace Aeon.Application.Features.Positions.AddPositions;

public interface IAddPositionsHandler
{
    Task<Result<AddPositionsResponse>> Handler(AddPositionsCommand request, CancellationToken cancellationToken = default);
}

public class AddPositionsHandler : IAddPositionsHandler
{
    private readonly IValidator<AddPositionsCommand> validator;
    private readonly IRepository repository;

    public AddPositionsHandler(IValidator<AddPositionsCommand> validator, IRepository repository)
    {
        this.validator = validator;
        this.repository = repository;
    }

    public async Task<Result<AddPositionsResponse>> Handler(AddPositionsCommand request, CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            //return Result.Fail(validationResult.Errors);
            return Result.Fail("erro ao validar");
        }

        var product = request.MapTo<Produto>();
        product.isActive = true;
        await repository.Inserir(product);

        return Result.Ok(product.MapTo<AddPositionsResponse>());
    }
}
