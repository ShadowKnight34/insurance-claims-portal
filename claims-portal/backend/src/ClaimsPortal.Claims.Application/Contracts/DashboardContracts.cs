namespace ClaimsPortal.Claims.Application.Contracts;

public sealed record CountByLabel(string Label, int Count);

public sealed record OfficerWorkloadDto(
    Guid OfficerId,
    string OfficerName,
    int OpenClaims,
    int TotalAssigned);

public sealed record DashboardDto(
    int TotalClaims,
    int PendingClaims,
    int UnderReview,
    int AwaitingInformation,
    int Approved,
    int Rejected,
    int Settled,
    decimal OutstandingLiability,
    decimal TotalApprovedSettlement,
    IReadOnlyList<CountByLabel> ByType,
    IReadOnlyList<CountByLabel> ByStatus,
    IReadOnlyList<OfficerWorkloadDto> OfficerWorkload);
