using ExulofraApi.Common.Abstractions;
using ExulofraApi.Common.Extensions;
using ExulofraApi.Common.Models;
using ExulofraApi.Domain.Entities;
using ExulofraApi.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExulofraApi.Features.Translations.Start;

public record StartTranslationResult(Guid TranslationId);

public record StartTranslationCommand(
    Guid SessionId,
    string SourceLang,
    string TargetLang,
    string SourceVoice,
    string TargetVoice,
    bool IsMuted,
    string? InputAudioUrl = null,
    string? OutputAudioUrl = null
) : IRequest<Result<StartTranslationResult>>;

public class StartTranslationHandler(AppDbContext context, ILogger<StartTranslationHandler> logger)
    : IRequestHandler<StartTranslationCommand, Result<StartTranslationResult>>
{
    public async Task<Result<StartTranslationResult>> Handle(
        StartTranslationCommand request,
        CancellationToken cancellationToken
    )
    {
        var session = await context.Sessions.FirstOrDefaultAsync(
            s => s.Id == request.SessionId,
            cancellationToken
        );

        if (session is null)
        {
            return Result<StartTranslationResult>.Failure(Error.NotFound("Session not found"));
        }

        var translation = new Translation
        {
            SessionId = request.SessionId,
            SourceLang = request.SourceLang,
            SourceVoice = request.SourceVoice,
            TargetLang = request.TargetLang,
            TargetVoice = request.TargetVoice,
            IsMuted = request.IsMuted,
            InputAudioUrl = request.InputAudioUrl,
            OutputAudioUrl = request.OutputAudioUrl,
        };

        context.Translations.Add(translation);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Translation created: {TranslationId} for Session: {SessionId}",
            translation.Id,
            session.Id
        );

        return Result<StartTranslationResult>.Success(new StartTranslationResult(translation.Id));
    }
}

public class StartTranslationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "translations",
                async (StartTranslationCommand command, ISender sender) =>
                {
                    var result = await sender.Send(command);
                    return result.ToActionResult();
                }
            )
            .WithTags("Translations")
            .WithSummary("Starts a new translation context")
            .RequireAuthorization();
    }
}
