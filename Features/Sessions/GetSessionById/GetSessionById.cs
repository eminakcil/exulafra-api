using System.Security.Claims;
using ExulofraApi.Common.Abstractions;
using ExulofraApi.Common.Extensions;
using ExulofraApi.Common.Models;
using ExulofraApi.Domain.Entities;
using ExulofraApi.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExulofraApi.Features.Sessions.GetSessionById;

public record SegmentDto(
    Guid Id,
    string SourceText,
    string TargetText,
    string SpeakerTag,
    TimeSpan Timestamp
);

public record TranslationDto(
    Guid Id,
    string SourceLang,
    string TargetLang,
    string TargetVoice,
    bool IsMuted,
    List<SegmentDto> Segments,
    DateTimeOffset CreatedAt
);

public record SessionDetailResponse(
    Guid Id,
    SessionType Type,
    DateTimeOffset CreatedAt,
    List<TranslationDto> Translations
);

public record GetSessionByIdQuery(Guid Id) : IRequest<Result<SessionDetailResponse>>;

public class GetSessionByIdHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetSessionByIdQuery, Result<SessionDetailResponse>>
{
    public async Task<Result<SessionDetailResponse>> Handle(
        GetSessionByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var userIdString = httpContextAccessor.HttpContext?.User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Result<SessionDetailResponse>.Failure(
                Error.Unauthorized("Kullanıcı doğrulanamadı.")
            );
        }

        var session = await context
            .Sessions.AsNoTracking()
            .Include(s => s.Translations)
                .ThenInclude(t => t.Segments)
            .FirstOrDefaultAsync(
                s => s.Id == request.Id && s.CreatorUserId == userId,
                cancellationToken
            );

        if (session is null)
        {
            return Result<SessionDetailResponse>.Failure(Error.NotFound("Oturum bulunamadı."));
        }

        var response = new SessionDetailResponse(
            Id: session.Id,
            Type: session.Type,
            CreatedAt: session.CreatedAt,
            Translations: session
                .Translations.OrderBy(t => t.CreatedAt)
                .Select(t => new TranslationDto(
                    Id: t.Id,
                    SourceLang: t.SourceLang,
                    TargetLang: t.TargetLang,
                    TargetVoice: t.TargetVoice,
                    IsMuted: t.IsMuted,
                    Segments: t.Segments.OrderBy(s => s.Timestamp)
                        .Select(s => new SegmentDto(
                            s.Id,
                            s.SourceText,
                            s.TargetText,
                            s.SpeakerTag,
                            s.Timestamp
                        ))
                        .ToList(),
                    CreatedAt: t.CreatedAt
                ))
                .ToList()
        );

        return Result<SessionDetailResponse>.Success(response);
    }
}

public class GetSessionByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "sessions/{id:guid}",
                async (Guid id, ISender sender) =>
                {
                    var query = new GetSessionByIdQuery(id);
                    var result = await sender.Send(query);
                    return result.ToActionResult();
                }
            )
            .WithTags("Sessions")
            .RequireAuthorization()
            .WithName("GetSessionById");
    }
}
