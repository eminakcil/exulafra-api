using System.Security.Claims;
using MediatR;
using ExulofraApi.Common.Abstractions;
using ExulofraApi.Common.Models;
using ExulofraApi.Extensions;

namespace ExulofraApi.Features.Auth.Me;

public record GetMeResponse(Guid Id, string Email, string Role);

public record GetMeQuery() : IRequest<Result<GetMeResponse>>;

public class GetMeHandler(IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<GetMeQuery, Result<GetMeResponse>>
{
    public async Task<Result<GetMeResponse>> Handle(
        GetMeQuery request,
        CancellationToken cancellationToken
    )
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            return Result<GetMeResponse>.Failure(Error.Unauthorized("Oturum bulunamadı."));
        }

        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var email = user.FindFirstValue(ClaimTypes.Email)!;
        var role = user.FindFirstValue(ClaimTypes.Role)!;

        var response = new GetMeResponse(userId, email, role);

        return await Task.FromResult(Result<GetMeResponse>.Success(response));
    }
}

public class GetMeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "auth/me",
                async (ISender sender) =>
                {
                    var result = await sender.Send(new GetMeQuery());
                    return result.ToActionResult();
                }
            )
            .WithName("GetMe")
            .WithTags("Authentication")
            .RequireAuthorization();
    }
}
