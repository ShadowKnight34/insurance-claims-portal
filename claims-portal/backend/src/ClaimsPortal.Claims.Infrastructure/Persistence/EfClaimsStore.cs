using ClaimsPortal.Claims.Application.Abstractions;
using ClaimsPortal.Claims.Domain.Entities;
using ClaimsPortal.Claims.Domain.Enums;
using ClaimsPortal.Claims.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ClaimsPortal.Claims.Infrastructure.Persistence;

public sealed class EfClaimsStore : IClaimsStore
{
    private readonly ClaimsDbContext _db;

    public EfClaimsStore(ClaimsDbContext db)
    {
        _db = db;
    }

    public Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalised = email.Trim().ToLowerInvariant();
        return _db.Users.FirstOrDefaultAsync(user => user.Email.ToLower() == normalised, cancellationToken);
    }

    public Task<User?> FindUserByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<bool> IsActiveOfficerAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _db.Users.AnyAsync(
            user => user.Id == userId && user.IsActive && user.Role == UserRole.ClaimsOfficer,
            cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListActiveOfficersAsync(CancellationToken cancellationToken)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(user => user.IsActive && user.Role == UserRole.ClaimsOfficer)
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Claim> CreateClaimAsync(Func<string, Claim> factory, DateTime utcNow, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var reference = await NextReferenceAsync(utcNow, cancellationToken);
            var claim = factory(reference);
            _db.Claims.Add(claim);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return claim;
            }
            catch (DbUpdateException exception) when (attempt < 5 && IsReferenceConflict(exception))
            {
                _db.Entry(claim).State = EntityState.Detached;
                foreach (var history in claim.StatusHistory)
                {
                    _db.Entry(history).State = EntityState.Detached;
                }
            }
        }

        throw new DomainValidationException("A unique claim reference could not be allocated. Submit the claim again.");
    }

    public Task<Claim?> FindClaimAsync(Guid id, CancellationToken cancellationToken)
    {
        return QueryWithDetails().FirstOrDefaultAsync(claim => claim.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Claim>> SearchClaimsAsync(ClaimSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = _db.Claims
            .AsNoTracking()
            .Include(claim => claim.Claimant)
            .Include(claim => claim.AssignedOfficer)
            .AsQueryable();

        if (criteria.ClaimantId is Guid claimantId)
        {
            query = query.Where(claim => claim.ClaimantId == claimantId);
        }

        if (criteria.UnassignedOnly)
        {
            query = query.Where(claim => claim.AssignedOfficerId == null);
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
            var term = criteria.Search.Trim().ToLower();
            query = query.Where(claim =>
                claim.ReferenceNumber.ToLower().Contains(term) ||
                claim.IncidentLocation.ToLower().Contains(term) ||
                claim.IncidentDescription.ToLower().Contains(term) ||
                claim.Claimant.FullName.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(claim => claim.CreatedAt)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Claim>(items, criteria.Page, criteria.PageSize, total);
    }

    public async Task<IReadOnlyList<DashboardClaimRow>> ListDashboardRowsAsync(CancellationToken cancellationToken)
    {
        return await _db.Claims
            .AsNoTracking()
            .Select(claim => new DashboardClaimRow(
                claim.Status,
                claim.ClaimType,
                claim.EstimatedLossAmount,
                claim.ApprovedSettlementAmount))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OfficerWorkloadRow>> ListOfficerWorkloadAsync(CancellationToken cancellationToken)
    {
        var officers = await _db.Users
            .AsNoTracking()
            .Where(user => user.IsActive && user.Role == UserRole.ClaimsOfficer)
            .OrderBy(user => user.FullName)
            .Select(user => new { user.Id, user.FullName })
            .ToListAsync(cancellationToken);

        var counts = await _db.Claims
            .AsNoTracking()
            .Where(claim => claim.AssignedOfficerId != null)
            .GroupBy(claim => claim.AssignedOfficerId!.Value)
            .Select(group => new
            {
                OfficerId = group.Key,
                Total = group.Count(),
                Open = group.Count(claim =>
                    claim.Status != ClaimStatus.Rejected && claim.Status != ClaimStatus.Settled)
            })
            .ToListAsync(cancellationToken);

        return officers
            .Select(officer =>
            {
                var count = counts.FirstOrDefault(item => item.OfficerId == officer.Id);
                return new OfficerWorkloadRow(
                    officer.Id,
                    officer.FullName,
                    count?.Open ?? 0,
                    count?.Total ?? 0);
            })
            .ToList();
    }

    public Task AddDocumentAsync(ClaimDocument document, CancellationToken cancellationToken)
    {
        _db.ClaimDocuments.Add(document);
        return Task.CompletedTask;
    }

    public Task<ClaimDocument?> FindDocumentAsync(Guid claimId, Guid documentId, CancellationToken cancellationToken)
    {
        return _db.ClaimDocuments
            .Include(document => document.UploadedBy)
            .FirstOrDefaultAsync(
                document => document.ClaimId == claimId && document.Id == documentId,
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Claim> QueryWithDetails()
    {
        return _db.Claims
            .AsSplitQuery()
            .Include(claim => claim.Claimant)
            .Include(claim => claim.AssignedOfficer)
            .Include(claim => claim.Documents).ThenInclude(document => document.UploadedBy)
            .Include(claim => claim.Assessments).ThenInclude(assessment => assessment.Officer)
            .Include(claim => claim.StatusHistory).ThenInclude(history => history.ChangedBy)
            .Include(claim => claim.Assignments).ThenInclude(assignment => assignment.Officer)
            .Include(claim => claim.Assignments).ThenInclude(assignment => assignment.AssignedBy)
            .Include(claim => claim.InformationRequests).ThenInclude(request => request.RequestedBy);
    }

    private async Task<string> NextReferenceAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var prefix = $"CLM-{utcNow.Year}-";
        var last = await _db.Claims
            .Where(claim => claim.ReferenceNumber.StartsWith(prefix))
            .OrderByDescending(claim => claim.ReferenceNumber)
            .Select(claim => claim.ReferenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;
        if (last is not null && int.TryParse(last.AsSpan(prefix.Length), out var parsed))
        {
            next = parsed + 1;
        }

        return $"{prefix}{next:D6}";
    }

    private static bool IsReferenceConflict(DbUpdateException exception)
    {
        var text = exception.ToString();
        return text.Contains("ReferenceNumber", StringComparison.OrdinalIgnoreCase)
            || text.Contains("claims_ReferenceNumber", StringComparison.OrdinalIgnoreCase)
            || text.Contains("23505", StringComparison.Ordinal);
    }
}
