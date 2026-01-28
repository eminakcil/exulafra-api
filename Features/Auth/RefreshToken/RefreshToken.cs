using MediatR;
using Microsoft.EntityFrameworkCore;
using ExulofraApi.Common.Abstractions;
using ExulofraApi.Common.Extensions;
using ExulofraApi.Common.Models;
using ExulofraApi.Infrastructure.Persistence;

namespace ExulofraApi.Features.Auth;

public record RefreshTokenCommand(string AccessToken, string RefreshToken)
    : IRequest<Result<TokenResponse>>;

public class RefreshTokenHandler(AppDbContext context, IJwtProvider jwtProvider)
    : IRequestHandler<RefreshTokenCommand, Result<TokenResponse>>
{
    public async Task<Result<TokenResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken
    )
    {
        var user = await context.Users.FirstOrDefaultAsync(
            u => u.RefreshToken == request.RefreshToken,
            cancellationToken
        );

        if (user is null || user.RefreshTokenExpiryTime <= DateTimeOffset.UtcNow)
        {
            return Result<TokenResponse>.Failure(
                Error.Unauthorized("Oturum süresi dolmuş, lütfen tekrar giriş yapın.")
            );
        }

        var tokenResponse = jwtProvider.Generate(user);

        user.UpdateRefreshToken(tokenResponse.RefreshToken, DateTimeOffset.UtcNow.AddDays(7));

        await context.SaveChangesAsync(cancellationToken);

        return Result<TokenResponse>.Success(tokenResponse);
    }
}

public class RefreshTokenEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "auth/refresh-token",
                async (RefreshTokenCommand command, ISender sender) =>
                {
                    var result = await sender.Send(command);
                    return result.ToActionResult();
                }
            )
            .WithTags("Authentication");
    }
}
