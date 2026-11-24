using ClaimsPortal.Claims.Application.Abstractions;
using ClaimsPortal.Claims.Domain.Entities;
using ClaimsPortal.Claims.Domain.Enums;

namespace ClaimsPortal.Claims.Tests.Support;

public sealed class InMemoryClaimsStore : IClaimsStore
{
    private readonly Dictionary<Guid, User> _users = [];
    private readonly Dictionary<Guid, Claim> _claims = [];
    private readonly List<ClaimDocument> _documents = [];
    private int _sequence;

    public User AddUser(string name, UserRole role)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{name.Replace(' ', '.').ToLowerInvariant()}@claims.demo",
            FullName = name,
            Role = role,
            IsActive = true,
            PasswordHash = "not-used",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _users[user.Id] = user;
        return user;
    }

    public Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var match = _users.Values.FirstOrDefault(user =>
            user.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(match);
    }

    public Task<User?> FindUserByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<bool> IsActiveOfficerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var isOfficer = _users.TryGetValue(userId, out var user)
            && user.IsActive
            && user.Role == UserRole.ClaimsOfficer;
        return Task.FromResult(isOfficer);
    }

    public Task<IReadOnlyList<User>> ListActiveOfficersAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<User> officers = _users.Values
            .Where(user => user.IsActive && user.Role == UserRole.ClaimsOfficer)
            .OrderBy(user => user.FullName)
            .ToList();
        return Task.FromResult(officers);
    }

    public Task<Claim> CreateClaimAsync(Func<string, Claim> factory, DateTime utcNow, CancellationToken cancellationToken)
    {
        _sequence++;
        var claim = factory($"CLM-{utcNow.Year}-{_sequence:D6}");
        if (_users.TryGetValue(claim.ClaimantId, out var claimant))
        {
            claim.Claimant = claimant;
        }

        _claims[claim.Id] = claim;
        return Task.FromResult(claim);
    }

    public Task<Claim?> FindClaimAsync(Guid id, CancellationToken cancellationToken)
    {
        _claims.TryGetValue(id, out var claim);
        AttachUsers(claim);
        return Task.FromResult(claim);
    }

    public Task<PagedResult<Claim>> SearchClaimsAsync(ClaimSearchCriteria criteria, CancellationToken cancellationToken)
    {
        IEnumerable<Claim> query = _claims.Values;
        if (criteria.ClaimantId is Guid claimantId)
        {
            query = query.Where(claim => claim.ClaimantId == claimantId);
        }

        if (criteria.UnassignedOnly)
        {
            query = query.Where(claim => claim.AssignedOfficerId is null);
        }
        else if (criteria.AssignedOfficerId is Guid officerId)
        {
            query = query.Where(claim => claim.AssignedOfficerId == officerId);
        }

        if (criteria.Status is ClaimStatus status)
        {
            query = query.Where(claim => claim.Status == status);
        }

        if (criteria.ClaimType is ClaimType claimType)
        {
            query = query.Where(claim => claim.ClaimType == claimType);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var term = criteria.Search.Trim();
            query = query.Where(claim =>
                claim.ReferenceNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                || claim.IncidentLocation.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var matches = query.OrderByDescending(claim => claim.CreatedAt).ToList();
        foreach (var claim in matches)
        {
            AttachUsers(claim);
        }

        var page = matches
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToList();
        return Task.FromResult(new PagedResult<Claim>(page, criteria.Page, criteria.PageSize, matches.Count));
    }

    public Task<IReadOnlyList<DashboardClaimRow>> ListDashboardRowsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<DashboardClaimRow> rows = _claims.Values
            .Select(claim => new DashboardClaimRow(
                claim.Status,
                claim.ClaimType,
                claim.EstimatedLossAmount,
                claim.ApprovedSettlementAmount))
            .ToList();
        return Task.FromResult(rows);
    }

    public Task<IReadOnlyList<OfficerWorkloadRow>> ListOfficerWorkloadAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<OfficerWorkloadRow> rows = _users.Values
            .Where(user => user.IsActive && user.Role == UserRole.ClaimsOfficer)
            .Select(officer =>
            {
                var assigned = _claims.Values.Where(claim => claim.AssignedOfficerId == officer.Id).ToList();
                var open = assigned.Count(claim =>
                    claim.Status is not ClaimStatus.Rejected and not ClaimStatus.Settled);
                return new OfficerWorkloadRow(officer.Id, officer.FullName, open, assigned.Count);
            })
            .ToList();
        return Task.FromResult(rows);
    }

    public Task AddDocumentAsync(ClaimDocument document, CancellationToken cancellationToken)
    {
        _documents.Add(document);
        if (_claims.TryGetValue(document.ClaimId, out var claim))
        {
            claim.Documents.Add(document);
        }

        return Task.CompletedTask;
    }

    public Task<ClaimDocument?> FindDocumentAsync(Guid claimId, Guid documentId, CancellationToken cancellationToken)
    {
        var document = _documents.FirstOrDefault(item => item.ClaimId == claimId && item.Id == documentId);
        return Task.FromResult(document);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void AttachUsers(Claim? claim)
    {
        if (claim is null)
        {
            return;
        }

        if (_users.TryGetValue(claim.ClaimantId, out var claimant))
        {
            claim.Claimant = claimant;
        }

        if (claim.AssignedOfficerId is Guid officerId && _users.TryGetValue(officerId, out var officer))
        {
            claim.AssignedOfficer = officer;
        }
    }
}
