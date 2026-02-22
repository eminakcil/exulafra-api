using MediatR;

namespace ExulofraApi.Features.Translations.ProcessAudio.Events;

public record AudioSynthesizedEvent(Guid TranslationId, Guid SegmentId, string AudioUrl)
    : INotification;
