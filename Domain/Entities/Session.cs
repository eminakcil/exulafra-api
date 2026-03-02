using ExulofraApi.Common.Abstractions;

namespace ExulofraApi.Domain.Entities;

public enum SessionType
{
    Dubbing = 1,
    Reporting = 2,
    Dialogue = 3,
    Broadcast = 4,
}

public class Session : BaseEntity
{
    public SessionType Type { get; set; }
    public string InitSocketConnectionId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid CreatorUserId { get; set; }

    // Navigation Properties
    public ICollection<Translation> Translations { get; set; } = new List<Translation>();
    public User Creator { get; set; } = null!;
}
