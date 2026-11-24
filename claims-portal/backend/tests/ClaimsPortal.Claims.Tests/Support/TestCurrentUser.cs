using ClaimsPortal.Claims.Application.Abstractions;

namespace ClaimsPortal.Claims.Tests.Support;

public sealed class TestCurrentUser : ICurrentUser
{
    public Guid UserId { get; init; }
    public bool IsClaimant { get; init; }
    public bool IsClaimsOfficer { get; init; }
}
