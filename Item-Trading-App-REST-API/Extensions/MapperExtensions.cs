using MapsterMapper;

namespace Item_Trading_App_REST_API.Extensions;

public static class MapperExtensions
{
    public static R AdaptToType<T, R>(this IMapper mapper, T request, params (string, object)[] parameters)
    {
        var builder = mapper.From(request);

        if (parameters is not null)
            foreach (var parameter in parameters)
            {
                builder = builder.AddParameters(parameter.Item1, parameter.Item2);
            }

        return builder.AdaptToType<R>();
    }
}
