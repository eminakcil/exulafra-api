using ExulofraApi.DependencyInjection;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    Log.Information("Starting web application...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog(
        (context, services, configuration) =>
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
    );

    builder
        .Services.AddPersistenceServices(builder.Configuration)
        .AddApplicationServices()
        .AddIdentityServices(builder.Configuration)
        .AddOpenApi()
        .AddPresentationServices(builder.Configuration)
        .AddHttpContextAccessor();

    var app = builder.Build();

    app.ApplyMigrations();

    app.ConfigurePipeline();

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var urls = string.Join(", ", app.Urls);
        Log.Information("Application is running on: {Urls}", urls);
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
