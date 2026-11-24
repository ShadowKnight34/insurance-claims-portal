using ClaimsPortal.Claims.Domain.Entities;
using ClaimsPortal.Claims.Domain.Enums;

namespace ClaimsPortal.Claims.Application.Abstractions;

public sealed record ClaimSearchCriteria(
    int Page,
    int PageSize,
    ClaimStatus? Status,
    ClaimType? ClaimType,
    string? Search,
    Guid? ClaimantId,
    Guid? AssignedOfficerId,
    bool UnassignedOnly);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record DashboardClaimRow(
    ClaimStatus Status,
    ClaimType ClaimType,
    decimal EstimatedLossAmount,
    decimal? ApprovedSettlementAmount);

public sealed record OfficerWorkloadRow(
    Guid OfficerId,
    string OfficerName,
    int OpenClaims,
    int TotalAssigned);

public interface IClaimsStore
{
    Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> FindUserByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsActiveOfficerAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> ListActiveOfficersAsync(CancellationToken cancellationToken);

    Task<Claim> CreateClaimAsync(Func<string, Claim> factory, DateTime utcNow, CancellationToken cancellationToken);
    Task<Claim?> FindClaimAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<Claim>> SearchClaimsAsync(ClaimSearchCriteria criteria, CancellationToken cancellationToken);

    Task<IReadOnlyList<DashboardClaimRow>> ListDashboardRowsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OfficerWorkloadRow>> ListOfficerWorkloadAsync(CancellationToken cancellationToken);

    Task AddDocumentAsync(ClaimDocument document, CancellationToken cancellationToken);
    Task<ClaimDocument?> FindDocumentAsync(Guid claimId, Guid documentId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
