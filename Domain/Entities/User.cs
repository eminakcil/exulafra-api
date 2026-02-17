using ExulofraApi.Common.Abstractions;
using ExulofraApi.Domain.Constants;

namespace ExulofraApi.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string Role { get; private set; }

    public ICollection<UserRefreshToken> RefreshTokens { get; private set; } =
        new List<UserRefreshToken>();

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

        if (role != Roles.Admin && role != Roles.User)
            throw new ArgumentException("Geçersiz rol.", nameof(role));

        Id = Guid.NewGuid();
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }
}
