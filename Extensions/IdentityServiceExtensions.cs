using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ExulofraApi.Common.Abstractions;
using ExulofraApi.Infrastructure.Authentication;

namespace ExulofraApi.Extensions;

public static class IdentityServicesExtension
{
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        var jwtOptions = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
        if (jwtOptions is null || string.IsNullOrEmpty(jwtOptions.Key))
        {
            throw new InvalidOperationException(
                "JWT Key is not configured properly in appsettings.json"
            );
        }

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));

        services.AddScoped<IJwtProvider, JwtProvider>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Key)
                    ),
                }
            );
        services.AddAuthorization();

        return services;
    }
}
