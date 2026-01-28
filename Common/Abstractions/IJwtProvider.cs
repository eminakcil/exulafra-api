using ExulofraApi.Domain.Entities;

namespace ExulofraApi.Common.Abstractions;

public record TokenResponse(string AccessToken, string RefreshToken);

public interface IJwtProvider
{
    TokenResponse Generate(User user);
    string GenerateRefreshToken();
}
