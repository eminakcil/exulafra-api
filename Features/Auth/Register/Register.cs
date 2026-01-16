using FluentValidation;
using MediatR;
using ExulofraApi.Common.Abstractions;
using ExulofraApi.Common.Entities;
using ExulofraApi.Infrastructure.Persistence;
using BC = BCrypt.Net.BCrypt;

namespace ExulofraApi.Features.Auth.Register;

public record RegisterCommand(string Email, string Password) : IRequest<Guid>;

public class RegisterHandler(AppDbContext context) : IRequestHandler<RegisterCommand, Guid>
{
    public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BC.HashPassword(request.Password),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "auth/register",
                async (RegisterCommand command, ISender sender) =>
                {
                    var userId = await sender.Send(command);
                    return Results.Ok(userId);
                }
            )
            .WithTags("Authentication");
    }
}
