using ExulofraApi.Common.Abstractions;

namespace ExulofraApi.Domain.Entities;

public class Segment : BaseEntity
{
    public Guid TranslationId { get; set; }
    public string SourceText { get; set; } = string.Empty;
    public string TargetText { get; set; } = string.Empty;
    public string SpeakerTag { get; set; } = string.Empty; // e.g., "Guest-1", "Host"
    public TimeSpan Timestamp { get; set; }
    public string? AudioUrl { get; set; }

    // Navigation Properties
    public Translation Translation { get; set; } = null!;
}
