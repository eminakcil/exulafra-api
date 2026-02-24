using MediatR;

namespace ExulofraApi.Features.Translations.ProcessAudio.Events;

public record PartialTextRecognizedEvent(
    Guid TranslationId,
    string SourceText,
    string TargetText,
    string SpeakerTag
) : INotification;
