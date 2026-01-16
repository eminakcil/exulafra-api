using FluentValidation;
using ExulofraApi.Common.Behaviors;

namespace ExulofraApi.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(Program).Assembly;
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);

            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        return services;
    }
}
