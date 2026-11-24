using ClaimsPortal.Claims.Domain.Enums;

namespace ClaimsPortal.Claims.Domain.Exceptions;

public sealed class InvalidClaimTransitionException : DomainException
{
    public InvalidClaimTransitionException(ClaimStatus from, ClaimStatus to)
        : base($"A claim cannot move from {from} to {to}.")
    {
        From = from;
        To = to;
    }

    public ClaimStatus From { get; }
    public ClaimStatus To { get; }
}
