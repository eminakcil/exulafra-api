using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ExulofraApi.Common.Abstractions;
using ExulofraApi.Common.Models;
using ExulofraApi.Domain.Entities;
using ExulofraApi.Extensions;
using ExulofraApi.Infrastructure.Persistence;
using BC = BCrypt.Net.BCrypt;

namespace ExulofraApi.Features.Auth.Register;

public record RegisterCommand(string Email, string Password) : IRequest<Result<Guid>>;

public class RegisterHandler(AppDbContext context) : IRequestHandler<RegisterCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken
    )
    {
        var exists = await context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);
        if (exists)
        {
            return Result<Guid>.Failure(Error.Conflict("Bu e-posta adresi zaten kullanımda."));
        }

        var passwordHash = BC.HashPassword(request.Password);
        var user = new User(request.Email, passwordHash);

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id);
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
                    var result = await sender.Send(command);
                    return result.ToActionResult();
                }
            )
            .WithTags("Authentication");
    }
}
