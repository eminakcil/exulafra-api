namespace ExulofraApi.Extensions;

public static class DatabaseExtension
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        return services;
    }
}
