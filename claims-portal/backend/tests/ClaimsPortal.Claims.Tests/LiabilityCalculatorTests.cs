using ClaimsPortal.Claims.Domain.Enums;
using ClaimsPortal.Claims.Domain.Liability;

namespace ClaimsPortal.Claims.Tests;

public sealed class LiabilityCalculatorTests
{
    [Theory]
    [InlineData(ClaimStatus.Rejected, 900, 0, 0)]
    [InlineData(ClaimStatus.Settled, 6400, 5800, 0)]
    [InlineData(ClaimStatus.Approved, 15000, 12600, 12600)]
    [InlineData(ClaimStatus.Submitted, 4800, 0, 4800)]
    [InlineData(ClaimStatus.UnderReview, 9200, 0, 9200)]
    [InlineData(ClaimStatus.AdditionalInformationRequired, 3100, 0, 3100)]
    public void OutstandingExposure_FollowsStatusRules(
        ClaimStatus status,
        int estimated,
        int approved,
        int expected)
    {
        decimal? settlement = approved == 0 ? null : approved;
        var exposure = LiabilityCalculator.OutstandingExposure(
            new ClaimExposure(status, estimated, settlement));

        Assert.Equal(expected, exposure);
    }

    [Fact]
    public void Totals_MatchTheSeededPortfolioRules()
    {
        ClaimExposure[] portfolio =
        [
            new(ClaimStatus.Submitted, 4800m, null),
            new(ClaimStatus.UnderReview, 9200m, null),
            new(ClaimStatus.AdditionalInformationRequired, 3100m, null),
            new(ClaimStatus.Approved, 15000m, 12600m),
            new(ClaimStatus.Rejected, 900m, null),
            new(ClaimStatus.Settled, 6400m, 5800m)
        ];

        Assert.Equal(29700m, LiabilityCalculator.TotalOutstanding(portfolio));
        Assert.Equal(18400m, LiabilityCalculator.TotalApprovedSettlement(portfolio));
        Assert.False(LiabilityCalculator.CountsAsOutstanding(ClaimStatus.Rejected));
        Assert.False(LiabilityCalculator.CountsAsOutstanding(ClaimStatus.Settled));
        Assert.True(LiabilityCalculator.CountsAsOutstanding(ClaimStatus.Approved));
    }
}
