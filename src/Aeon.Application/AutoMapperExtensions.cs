using AutoMapper;

namespace Aeon.Application;

public static class AutoMapperExtensions
{
    private static readonly Lazy<IMapper> Lazy = new(() =>
    {
        var config = new MapperConfiguration(cfg => cfg.AddMaps(typeof(AutoMapperExtensions).Assembly));
        return config.CreateMapper();
    });

    public static IMapper Mapper => Lazy.Value;

    public static T MapTo<T>(this object source) => Mapper.Map<T>(source);
}