using ExulofraApi.Common.Abstractions;
using ExulofraApi.Infrastructure.Options;
using ExulofraApi.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ExulofraApi.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<AzureOptions>(configuration.GetSection(AzureOptions.SectionName));

        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        }); // .AddMessagePackProtocol(); // access to messagepack?

        services.AddScoped<ISpeechService, AzureSpeechService>();

        return services;
    }
}
