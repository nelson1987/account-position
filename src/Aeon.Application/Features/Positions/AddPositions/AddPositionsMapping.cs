using Aeon.Domain.Entities;
using AutoMapper;

namespace Aeon.Application.Features.Positions.AddPositions;

public class AddPositionsMapping : Profile
{
    public AddPositionsMapping()
    {
        CreateMap<AddPositionsCommand, Produto>();
        CreateMap<Produto, AddPositionsResponse>();
    }
}
