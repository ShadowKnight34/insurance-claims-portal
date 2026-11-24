using ClaimsPortal.Claims.Domain.Enums;

namespace ClaimsPortal.Claims.Domain.Liability;

public readonly record struct ClaimExposure(
    ClaimStatus Status,
    decimal EstimatedLossAmount,
    decimal? ApprovedSettlementAmount);

/// <summary>
/// Outstanding exposure is money the insurer may still have to pay.
/// Rejected claims are not an exposure. Settled claims have already been paid.
/// Approved claims use the agreed settlement amount. Every earlier open status
/// uses the claimant's estimated loss, because no settlement has been agreed.
/// </summary>
public static class LiabilityCalculator
{
    public static decimal OutstandingExposure(ClaimExposure claim)
    {
        return claim.Status switch
        {
            ClaimStatus.Rejected => 0m,
            ClaimStatus.Settled => 0m,
            ClaimStatus.Approved => decimal.Round(claim.ApprovedSettlementAmount ?? 0m, 2, MidpointRounding.AwayFromZero),
            _ => decimal.Round(claim.EstimatedLossAmount, 2, MidpointRounding.AwayFromZero)
        };
    }

    public static decimal TotalOutstanding(IEnumerable<ClaimExposure> claims)
    {
        return decimal.Round(claims.Sum(OutstandingExposure), 2, MidpointRounding.AwayFromZero);
    }

    public static decimal TotalApprovedSettlement(IEnumerable<ClaimExposure> claims)
    {
        var total = claims
            .Where(claim => claim.Status is ClaimStatus.Approved or ClaimStatus.Settled)
            .Sum(claim => claim.ApprovedSettlementAmount ?? 0m);

        return decimal.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    public static bool CountsAsOutstanding(ClaimStatus status)
    {
        return status is not ClaimStatus.Rejected and not ClaimStatus.Settled;
    }
}
