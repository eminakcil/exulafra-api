using MediatR;

namespace ExulofraApi.Features.Translations.ProcessAudio.Events;

public record SegmentTranslatedEvent(
    Guid TranslationId,
    Guid SegmentId,
    string SourceText,
    string TargetText,
    string SpeakerTag,
    TimeSpan Timestamp
) : INotification;
