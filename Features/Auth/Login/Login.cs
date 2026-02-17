using ExulofraApi.Common.Abstractions;
using ExulofraApi.Common.Extensions;
using ExulofraApi.Common.Models;
using ExulofraApi.Domain.Entities;
using ExulofraApi.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace ExulofraApi.Features.Auth.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<TokenResponse>>;

public class LoginHandler(AppDbContext context, IJwtProvider jwtProvider)
    : IRequestHandler<LoginCommand, Result<TokenResponse>>
{
    public async Task<Result<TokenResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken
    )
    {
        var user = await context.Users.FirstOrDefaultAsync(
            u => u.Email == request.Email,
            cancellationToken
        );

        if (user is null || !BC.Verify(request.Password, user.PasswordHash))
        {
            return Result<TokenResponse>.Failure(
                Error.Unauthorized("Geçersiz e-posta veya şifre.")
            );
        }

        var tokenResponse = jwtProvider.Generate(user);

        var refreshToken = new UserRefreshToken(
            tokenResponse.RefreshToken,
            DateTimeOffset.UtcNow.AddDays(7),
            user.Id
        );

        user.RefreshTokens.Add(refreshToken);

        await context.SaveChangesAsync(cancellationToken);

        return Result<TokenResponse>.Success(tokenResponse);
    }
}

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "auth/login",
                async (LoginCommand command, ISender sender) =>
                {
                    var result = await sender.Send(command);
                    return result.ToActionResult();
                }
            )
            .WithTags("Authentication")
            .RequireRateLimiting("auth-limit");
    }
}
