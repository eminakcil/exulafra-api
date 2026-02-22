using ExulofraApi.Features.Translations.ProcessAudio.Events;
using ExulofraApi.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExulofraApi.Features.Translations.ProcessAudio.Handlers;

public class AudioSynthesizedDatabaseHandler(AppDbContext context)
    : INotificationHandler<AudioSynthesizedEvent>
{
    public async Task Handle(
        AudioSynthesizedEvent notification,
        CancellationToken cancellationToken
    )
    {
        var segment = await context.Segments.FirstOrDefaultAsync(
            s => s.Id == notification.SegmentId,
            cancellationToken
        );

        if (segment != null)
        {
            segment.AudioUrl = notification.AudioUrl;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
