using ExulofraApi.Common.Abstractions;
using ExulofraApi.Common.Extensions;
using ExulofraApi.Common.Models;
using ExulofraApi.Domain.Entities;
using ExulofraApi.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExulofraApi.Features.Auth;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<TokenResponse>>;

public class RefreshTokenHandler(AppDbContext context, IJwtProvider jwtProvider)
    : IRequestHandler<RefreshTokenCommand, Result<TokenResponse>>
{
    public async Task<Result<TokenResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken
    )
    {
        var existingRefreshToken = await context
            .UserRefreshTokens.Include(rt => rt.User) // needed to get user for jwt generation? No, we need user for claims.
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (existingRefreshToken is null)
        {
            return Result<TokenResponse>.Failure(Error.Unauthorized("Geçersiz refresh token."));
        }

        // Reuse Detection
        if (existingRefreshToken.IsRevoked)
        {
            // Security: Attempted reuse of revoked token!
            // Option: Revoke all descendant tokens or user sessions.
            // For now, just return specific error or generic unauthorized.
            return Result<TokenResponse>.Failure(
                Error.Unauthorized("Bu oturum sonlandırılmış. Lütfen tekrar giriş yapın.")
            );
        }

        if (existingRefreshToken.Expires <= DateTimeOffset.UtcNow)
        {
            return Result<TokenResponse>.Failure(
                Error.Unauthorized("Oturum süresi dolmuş, lütfen tekrar giriş yapın.")
            );
        }

        var user = existingRefreshToken.User;
        if (user is null)
        {
            return Result<TokenResponse>.Failure(Error.Unauthorized("Kullanıcı bulunamadı."));
        }

        // Generate new tokens
        var tokenResponse = jwtProvider.Generate(user);

        // Rotate Token
        // 1. Revoke old token
        existingRefreshToken.IsRevoked = true;
        existingRefreshToken.RevokedAt = DateTimeOffset.UtcNow;
        existingRefreshToken.ReplacedByToken = tokenResponse.RefreshToken;

        // 2. Create new token
        var newRefreshToken = new UserRefreshToken(
            tokenResponse.RefreshToken,
            DateTimeOffset.UtcNow.AddDays(7),
            user.Id
        );

        context.UserRefreshTokens.Add(newRefreshToken);

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
