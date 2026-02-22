using ExulofraApi.Features.Translations.ProcessAudio.Events;
using ExulofraApi.Infrastructure.SignalR;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ExulofraApi.Features.Translations.ProcessAudio.Handlers;

public class ProcessAudioSignalRHandlers(IHubContext<TranslationHub> hubContext)
    : INotificationHandler<PartialTextRecognizedEvent>,
        INotificationHandler<SegmentTranslatedEvent>,
        INotificationHandler<AudioSynthesizedEvent>
{
    public async Task Handle(
        PartialTextRecognizedEvent notification,
        CancellationToken cancellationToken
    )
    {
        await hubContext
            .Clients.Group(notification.TranslationId.ToString())
            .SendAsync("ReceivePartial", notification.Text, cancellationToken);
    }

    public async Task Handle(
        SegmentTranslatedEvent notification,
        CancellationToken cancellationToken
    )
    {
        await hubContext
            .Clients.Group(notification.TranslationId.ToString())
            .SendAsync(
                "ReceiveTranslation",
                notification.SegmentId,
                notification.SourceText,
                cancellationToken
            );
    }

    public async Task Handle(
        AudioSynthesizedEvent notification,
        CancellationToken cancellationToken
    )
    {
        await hubContext
            .Clients.Group(notification.TranslationId.ToString())
            .SendAsync(
                "ReceiveAudio",
                notification.SegmentId,
                notification.AudioUrl,
                cancellationToken
            );
    }
}
