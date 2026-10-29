using Microsoft.IdentityModel.JsonWebTokens;

namespace WorkItemTracker.Api.Auth;

public class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string UserId =>
        httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? throw new InvalidOperationException("The current request has no 'sub' claim.");
}
