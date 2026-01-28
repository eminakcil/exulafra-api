using Microsoft.AspNetCore.Authorization;

namespace ExulofraApi.Common.Authorization;

public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        : base(policy: permission) { }
}
