using System.Security.Claims;
using ExulofraApi.Common.Abstractions;
using ExulofraApi.Common.Extensions;
using ExulofraApi.Common.Models;
using ExulofraApi.Domain.Entities;
using ExulofraApi.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExulofraApi.Features.Sessions.GetSessions;

public record SessionSummaryResponse(Guid Id, SessionType Type, DateTimeOffset CreatedAt);

public record GetSessionsQuery : IRequest<Result<List<SessionSummaryResponse>>>;

public class GetSessionsHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetSessionsQuery, Result<List<SessionSummaryResponse>>>
{
    public async Task<Result<List<SessionSummaryResponse>>> Handle(
        GetSessionsQuery request,
        CancellationToken cancellationToken
    )
    {
        var userIdString = httpContextAccessor.HttpContext?.User?.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Result<List<SessionSummaryResponse>>.Failure(
                Error.Unauthorized("Kullanıcı doğrulanamadı.")
            );
        }

        var sessions = await context
            .Sessions.AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SessionSummaryResponse(s.Id, s.Type, s.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<List<SessionSummaryResponse>>.Success(sessions);
    }
}

public class GetSessionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "sessions",
                async (ISender sender) =>
                {
                    var result = await sender.Send(new GetSessionsQuery());
                    return result.ToActionResult();
                }
            )
            .WithTags("Sessions")
            .RequireAuthorization()
            .WithName("GetSessions");
    }
}
