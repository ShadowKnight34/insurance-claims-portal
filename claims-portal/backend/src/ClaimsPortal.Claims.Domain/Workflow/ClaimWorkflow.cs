using ClaimsPortal.Claims.Domain.Enums;
using ClaimsPortal.Claims.Domain.Exceptions;

namespace ClaimsPortal.Claims.Domain.Workflow;

/// <summary>
/// Allowed claim lifecycle:
/// Submitted → Under Review → Additional Information Required → Under Review → Approved or Rejected.
/// Approved may proceed to Settled. Rejected and Settled are terminal.
/// </summary>
public static class ClaimWorkflow
{
    private static readonly IReadOnlyDictionary<ClaimStatus, ClaimStatus[]> Allowed =
        new Dictionary<ClaimStatus, ClaimStatus[]>
        {
            [ClaimStatus.Submitted] = [ClaimStatus.UnderReview],
            [ClaimStatus.UnderReview] =
            [
                ClaimStatus.AdditionalInformationRequired,
                ClaimStatus.Approved,
                ClaimStatus.Rejected
            ],
            [ClaimStatus.AdditionalInformationRequired] = [ClaimStatus.UnderReview],
            [ClaimStatus.Approved] = [ClaimStatus.Settled],
            [ClaimStatus.Rejected] = [],
            [ClaimStatus.Settled] = []
        };

    public static bool CanTransition(ClaimStatus from, ClaimStatus to)
    {
        return Allowed.TryGetValue(from, out var next) && next.Contains(to);
    }

    public static void EnsureTransition(ClaimStatus from, ClaimStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidClaimTransitionException(from, to);
        }
    }

    public static IReadOnlyList<ClaimStatus> NextStatuses(ClaimStatus from)
    {
        return Allowed.TryGetValue(from, out var next) ? next : [];
    }
}
