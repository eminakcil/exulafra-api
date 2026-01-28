using ExulofraApi.Common.Abstractions;

namespace ExulofraApi.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string Role { get; private set; }

    public string? RefreshToken { get; private set; }
    public DateTimeOffset? RefreshTokenExpiryTime { get; private set; }

    private User()
    {
        Email = null!;
        PasswordHash = null!;
        Role = null!;
    }

    public User(string email, string passwordHash, string role = "User")
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentNullException(nameof(email));

        Id = Guid.NewGuid();
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    public void UpdateRefreshToken(string refreshToken, DateTimeOffset expiryTime)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token boş olamaz.", nameof(refreshToken));

        RefreshToken = refreshToken;
        RefreshTokenExpiryTime = expiryTime;
    }
}
