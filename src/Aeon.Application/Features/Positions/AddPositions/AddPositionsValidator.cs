using FluentValidation;

namespace Aeon.Application.Features.Positions.AddPositions;

public class AddPositionsValidator : AbstractValidator<AddPositionsCommand>
{
    public AddPositionsValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
