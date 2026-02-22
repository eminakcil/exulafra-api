using ExulofraApi.Domain.Entities;
using ExulofraApi.Features.Translations.ProcessAudio.Events;
using ExulofraApi.Infrastructure.Persistence;
using MediatR;

namespace ExulofraApi.Features.Translations.ProcessAudio.Handlers;

public class SegmentTranslatedDatabaseHandler(
    AppDbContext context,
    ILogger<SegmentTranslatedDatabaseHandler> logger
) : INotificationHandler<SegmentTranslatedEvent>
{
    public async Task Handle(
        SegmentTranslatedEvent notification,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var segment = new Segment
            {
                Id = notification.SegmentId,
                TranslationId = notification.TranslationId,
                SourceText = notification.SourceText,
                TargetText = notification.TargetText,
                SpeakerTag = notification.SpeakerTag,
                Timestamp = notification.Timestamp,
            };

            context.Segments.Add(segment);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Segment successfully saved. TranslationId: {TranslationId}, SegmentId: {SegmentId}",
                notification.TranslationId,
                notification.SegmentId
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save segment to database. TranslationId: {TranslationId}, SegmentId: {SegmentId}",
                notification.TranslationId,
                notification.SegmentId
            );
            throw;
        }
    }
}
