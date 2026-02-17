using ExulofraApi.Common.Abstractions;

namespace ExulofraApi.Domain.Entities;

public class Translation : BaseEntity
{
    public Guid SessionId { get; set; }
    public string SourceLang { get; set; } = string.Empty;
    public string TargetLang { get; set; } = string.Empty;
    public string TargetVoice { get; set; } = string.Empty;
    public string? InputAudioUrl { get; set; }
    public string? OutputAudioUrl { get; set; }
    public bool IsMuted { get; set; }

    // Navigation Properties
    public Session Session { get; set; } = null!;
    public ICollection<Segment> Segments { get; set; } = new List<Segment>();
}
