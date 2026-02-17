using ExulofraApi.Common.Abstractions;

namespace ExulofraApi.Domain.Entities;

public sealed class UserRefreshToken : BaseEntity
{
    public string Token { get; set; } = null!;
    public DateTimeOffset Expires { get; set; }
    public Guid UserId { get; set; }
    public bool IsRevoked { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }

    public UserRefreshToken() { }

    public UserRefreshToken(string token, DateTimeOffset expires, Guid userId)
    {
        Token = token;
        Expires = expires;
        UserId = userId;
    }
}
