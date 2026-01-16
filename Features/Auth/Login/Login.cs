using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ExulofraApi.Common.Abstractions;
using ExulofraApi.Infrastructure.Persistence;
using BC = BCrypt.Net.BCrypt;

namespace ExulofraApi.Features.Auth.Login;

public record LoginResponse(string Token);

public record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;

public class LoginHandler(AppDbContext context, IJwtProvider jwtProvider)
    : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(
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
            throw new UnauthorizedAccessException("Geçersiz e-posta veya şifre.");
        }

        var token = jwtProvider.Generate(user);
        return new LoginResponse(token);
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
                    var response = await sender.Send(command);
                    return Results.Ok(response);
                }
            )
            .WithTags("Authentication");
    }
}
