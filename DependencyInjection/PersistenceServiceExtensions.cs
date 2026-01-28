using Microsoft.EntityFrameworkCore;
using ExulofraApi.Infrastructure.Persistence;
using ExulofraApi.Infrastructure.Persistence.Interceptors;

namespace ExulofraApi.DependencyInjection;

public static class DatabaseExtension
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        services.AddDbContext<AppDbContext>(
            (sp, options) =>
            {
                var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
                var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();

                options
                    .UseInMemoryDatabase("ExulofraDb")
                    .AddInterceptors(auditInterceptor, softDeleteInterceptor);
            }
        );

        return services;
    }
}
