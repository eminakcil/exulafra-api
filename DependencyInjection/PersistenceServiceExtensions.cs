using ExulofraApi.Infrastructure.Persistence;
using ExulofraApi.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace ExulofraApi.DependencyInjection;

public static class PersistenceServiceExtensions
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
                    .UseNpgsql(config.GetConnectionString("DefaultConnection"))
                    .AddInterceptors(auditInterceptor, softDeleteInterceptor);
            }
        );

        return services;
    }
}
