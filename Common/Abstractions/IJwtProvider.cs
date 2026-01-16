using ExulofraApi.Common.Entities;

namespace ExulofraApi.Common.Abstractions;

public interface IJwtProvider
{
    string Generate(User user);
}
