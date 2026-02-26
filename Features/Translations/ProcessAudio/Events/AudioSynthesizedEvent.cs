using MediatR;

namespace ExulofraApi.Features.Translations.ProcessAudio.Events;

public record AudioSynthesizedEvent(Guid TranslationId, Guid SegmentId, string AudioBase64)
    : INotification;
