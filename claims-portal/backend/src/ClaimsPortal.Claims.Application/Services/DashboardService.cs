using ClaimsPortal.Claims.Application.Abstractions;
using ClaimsPortal.Claims.Application.Contracts;
using ClaimsPortal.Claims.Application.Exceptions;
using ClaimsPortal.Claims.Domain.Entities;
using ClaimsPortal.Claims.Domain.Enums;
using ClaimsPortal.Claims.Domain.Liability;

namespace ClaimsPortal.Claims.Application.Services;

public sealed class DashboardService
{
    private readonly IClaimsStore _store;
    private readonly ICurrentUser _currentUser;

    public DashboardService(IClaimsStore store, ICurrentUser currentUser)
    {
        _store = store;
        _currentUser = currentUser;
    }

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsClaimsOfficer)
        {
            throw new ForbiddenAppException("Only claims officers can view management statistics.");
        }

        var rows = await _store.ListDashboardRowsAsync(cancellationToken);
        var exposures = rows
            .Select(row => new ClaimExposure(row.Status, row.EstimatedLossAmount, row.ApprovedSettlementAmount))
            .ToList();
        var workload = await _store.ListOfficerWorkloadAsync(cancellationToken);

        return new DashboardDto(
            TotalClaims: rows.Count,
            PendingClaims: rows.Count(row => row.Status == ClaimStatus.Submitted),
            UnderReview: rows.Count(row => row.Status == ClaimStatus.UnderReview),
            AwaitingInformation: rows.Count(row => row.Status == ClaimStatus.AdditionalInformationRequired),
            Approved: rows.Count(row => row.Status == ClaimStatus.Approved),
            Rejected: rows.Count(row => row.Status == ClaimStatus.Rejected),
            Settled: rows.Count(row => row.Status == ClaimStatus.Settled),
            OutstandingLiability: LiabilityCalculator.TotalOutstanding(exposures),
            TotalApprovedSettlement: LiabilityCalculator.TotalApprovedSettlement(exposures),
            ByType: Enum.GetValues<ClaimType>()
                .Select(type => new CountByLabel(type.ToString(), rows.Count(row => row.ClaimType == type)))
                .ToList(),
            ByStatus: Enum.GetValues<ClaimStatus>()
                .Select(status => new CountByLabel(status.ToString(), rows.Count(row => row.Status == status)))
                .ToList(),
            OfficerWorkload: workload
                .Select(row => new OfficerWorkloadDto(row.OfficerId, row.OfficerName, row.OpenClaims, row.TotalAssigned))
                .ToList());
    }
}
