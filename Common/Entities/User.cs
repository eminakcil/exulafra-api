using ExulofraApi.Common.Abstractions;

namespace ExulofraApi.Common.Entities;

public sealed class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}
