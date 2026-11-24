using System.Security.Claims;
using ClaimsPortal.Claims.Application.Abstractions;
using ClaimsPortal.Claims.Application.Exceptions;
using ClaimsPortal.Claims.Domain.Enums;

namespace ClaimsPortal.Claims.Api.Security;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(value, out var userId))
            {
                throw new UnauthorizedAppException("Authentication is required.");
            }

            return userId;
        }
    }

    public bool IsClaimant => HasRole(UserRole.Claimant);

    public bool IsClaimsOfficer => HasRole(UserRole.ClaimsOfficer);

    private bool HasRole(UserRole role)
    {
        return _httpContextAccessor.HttpContext?.User.IsInRole(role.ToString()) == true;
    }
}
