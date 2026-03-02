using System.Security.Claims;
using ExulofraApi.Common.Abstractions;
using ExulofraApi.Common.Extensions;
using ExulofraApi.Common.Models;
using ExulofraApi.Domain.Entities;
using ExulofraApi.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExulofraApi.Features.Sessions.Create;

public record CreateSessionResult(Guid SessionId);

public record CreateSessionCommand(SessionType Type) : IRequest<Result<CreateSessionResult>>;

public class CreateSessionHandler(
    AppDbContext context,
    IHttpContextAccessor httpContextAccessor,
    ILogger<CreateSessionHandler> logger
) : IRequestHandler<CreateSessionCommand, Result<CreateSessionResult>>
{
    public async Task<Result<CreateSessionResult>> Handle(
        CreateSessionCommand request,
        CancellationToken cancellationToken
    )
    {
        var userIdString = httpContextAccessor.HttpContext?.User?.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Result<CreateSessionResult>.Failure(
                Error.Unauthorized("Kullanıcı doğrulanamadı.")
            );
        }

        var session = new Session
        {
            Type = request.Type,
            IsActive = true,
            CreatorUserId = userId,
        };

        context.Sessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Session created: {SessionId}", session.Id);

        return Result<CreateSessionResult>.Success(new CreateSessionResult(session.Id));
    }
}

public class CreateSessionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "sessions",
                async (CreateSessionCommand command, ISender sender) =>
                {
                    var result = await sender.Send(command);
                    return result.ToActionResult();
                }
            )
            .WithTags("Sessions")
            .WithSummary("Creates a new session for translation/dubbing")
            .RequireAuthorization();
    }
}
