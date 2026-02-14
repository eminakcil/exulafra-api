using ExulofraApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExulofraApi.DependencyInjection;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        using AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            logger.LogInformation("Database migration process started.");

            context.Database.Migrate();

            logger.LogInformation("Database migration completed successfully.");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled exception occurred during migration: {Message}",
                exception.Message
            );

            throw;
        }
    }
}
