using ClaimsPortal.Claims.Application.Abstractions;
using ClaimsPortal.Claims.Application.Contracts;
using ClaimsPortal.Claims.Application.Exceptions;
using ClaimsPortal.Claims.Domain.Entities;
using ClaimsPortal.Claims.Domain.Enums;
using ClaimsPortal.Claims.Domain.Workflow;

namespace ClaimsPortal.Claims.Application.Services;

public sealed class ClaimService
{
    private readonly IClaimsStore _store;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IClaimEventPublisher _events;

    public ClaimService(
        IClaimsStore store,
        ICurrentUser currentUser,
        IClock clock,
        IClaimEventPublisher events)
    {
        _store = store;
        _currentUser = currentUser;
        _clock = clock;
        _events = events;
    }

    public async Task<ClaimDetailDto> CreateAsync(CreateClaimRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsClaimant)
        {
            throw new ForbiddenAppException("Only claimants can submit claims.");
        }

        var now = _clock.UtcNow;
        var claim = await _store.CreateClaimAsync(
            reference => Claim.Submit(
                _currentUser.UserId,
                reference,
                request.ClaimType,
                request.IncidentDate,
                request.IncidentLocation,
                request.IncidentDescription,
                request.EstimatedLossAmount,
                now),
            now,
            cancellationToken);
        await PublishStatusAsync(claim, null, cancellationToken);
        return ToDetail(await ReloadAsync(claim.Id, cancellationToken));
    }

    public async Task<PagedResult<ClaimSummaryDto>> SearchAsync(ClaimListQuery query, CancellationToken cancellationToken)
    {
        var criteria = new ClaimSearchCriteria(
            Page: query.Page < 1 ? 1 : query.Page,
            PageSize: query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100),
            Status: query.Status,
            ClaimType: query.ClaimType,
            Search: string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            ClaimantId: _currentUser.IsClaimant ? _currentUser.UserId : null,
            AssignedOfficerId: _currentUser.IsClaimsOfficer && query.AssignedToMe
                ? _currentUser.UserId
                : _currentUser.IsClaimsOfficer ? query.AssignedOfficerId : null,
            UnassignedOnly: _currentUser.IsClaimsOfficer && query.UnassignedOnly);

        var page = await _store.SearchClaimsAsync(criteria, cancellationToken);
        return new PagedResult<ClaimSummaryDto>(
            page.Items.Select(ToSummary).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }

    public async Task<ClaimDetailDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return ToDetail(await RequireViewableAsync(id, cancellationToken));
    }

    public async Task<ClaimDetailDto> UpdateAsync(Guid id, UpdateClaimRequest request, CancellationToken cancellationToken)
    {
        var claim = await RequireViewableAsync(id, cancellationToken);
        if (!_currentUser.IsClaimant || claim.ClaimantId != _currentUser.UserId)
        {
            throw new ForbiddenAppException("Only the claimant can edit a submitted claim.");
        }

        claim.UpdateIncidentDetails(
            request.ClaimType,
            request.IncidentDate,
            request.IncidentLocation,
            request.IncidentDescription,
            request.EstimatedLossAmount,
            _clock.UtcNow);
        await _store.SaveChangesAsync(cancellationToken);
        return ToDetail(await ReloadAsync(id, cancellationToken));
    }

    public async Task<ClaimDetailDto> AssignAsync(Guid id, AssignClaimRequest request, CancellationToken cancellationToken)
    {
        EnsureOfficer();
        if (!await _store.IsActiveOfficerAsync(request.OfficerId, cancellationToken))
        {
            throw new NotFoundAppException("The selected claims officer was not found.");
        }

        var claim = await RequireViewableAsync(id, cancellationToken);
        var before = claim.Status;
        claim.AssignTo(request.OfficerId, _currentUser.UserId, request.Notes, _clock.UtcNow);
        await _store.SaveChangesAsync(cancellationToken);
        await PublishIfChangedAsync(claim, before, cancellationToken);
        return ToDetail(await ReloadAsync(id, cancellationToken));
    }

    public async Task<ClaimDetailDto> RequestInformationAsync(
        Guid id,
        InformationRequestCreate request,
        CancellationToken cancellationToken)
    {
        EnsureOfficer();
        var claim = await RequireViewableAsync(id, cancellationToken);
        var before = claim.Status;
        claim.RequestInformation(_currentUser.UserId, request.Message, _clock.UtcNow);
        await _store.SaveChangesAsync(cancellationToken);
        await PublishIfChangedAsync(claim, before, cancellationToken);
        return ToDetail(await ReloadAsync(id, cancellationToken));
    }

    public async Task<ClaimDetailDto> RespondToInformationAsync(
        Guid id,
        Guid requestId,
        InformationResponseRequest request,
        CancellationToken cancellationToken)
    {
        var claim = await RequireViewableAsync(id, cancellationToken);
        if (!_currentUser.IsClaimant || claim.ClaimantId != _currentUser.UserId)
        {
            throw new ForbiddenAppException("Only the claimant can respond to an information request.");
        }

        var before = claim.Status;
        claim.RespondToInformation(requestId, request.Response, _currentUser.UserId, _clock.UtcNow);
        await _store.SaveChangesAsync(cancellationToken);
        await PublishIfChangedAsync(claim, before, cancellationToken);
        return ToDetail(await ReloadAsync(id, cancellationToken));
    }

    public async Task<ClaimDetailDto> AddAssessmentAsync(
        Guid id,
        AssessmentRequest request,
        CancellationToken cancellationToken)
    {
        EnsureOfficer();
        var claim = await RequireViewableAsync(id, cancellationToken);
        claim.AddAssessment(_currentUser.UserId, request.Notes, _clock.UtcNow);
        await _store.SaveChangesAsync(cancellationToken);
        return ToDetail(await ReloadAsync(id, cancellationToken));
    }

    public async Task<ClaimDetailDto> ApproveAsync(Guid id, ApproveClaimRequest request, CancellationToken cancellationToken)
    {
        EnsureOfficer();
        var claim = await RequireViewableAsync(id, cancellationToken);
        var before = claim.Status;
        claim.Approve(request.SettlementAmount, _currentUser.UserId, request.Notes, _clock.UtcNow);
        await _store.SaveChangesAsync(cancellationToken);
        await PublishIfChangedAsync(claim, before, cancellationToken);
        return ToDetail(await ReloadAsync(id, cancellationToken));
    }

    public async Task<ClaimDetailDto> RejectAsync(Guid id, RejectClaimRequest request, CancellationToken cancellationToken)
    {
        EnsureOfficer();
        var claim = await RequireViewableAsync(id, cancellationToken);
        var before = claim.Status;
        claim.Reject(request.Reason, _currentUser.UserId, _clock.UtcNow);
        await _store.SaveChangesAsync(cancellationToken);
        await PublishIfChangedAsync(claim, before, cancellationToken);
        return ToDetail(await ReloadAsync(id, cancellationToken));
    }

    public async Task<ClaimDetailDto> SettleAsync(Guid id, CancellationToken cancellationToken)
    {
        EnsureOfficer();
        var claim = await RequireViewableAsync(id, cancellationToken);
        var before = claim.Status;
        claim.Settle(_currentUser.UserId, _clock.UtcNow);
        await _store.SaveChangesAsync(cancellationToken);
        await PublishIfChangedAsync(claim, before, cancellationToken);
        return ToDetail(await ReloadAsync(id, cancellationToken));
    }

    private async Task<Claim> RequireViewableAsync(Guid id, CancellationToken cancellationToken)
    {
        var claim = await _store.FindClaimAsync(id, cancellationToken)
            ?? throw new NotFoundAppException("Claim not found.");

        if (_currentUser.IsClaimsOfficer)
        {
            return claim;
        }

        if (_currentUser.IsClaimant && claim.ClaimantId == _currentUser.UserId)
        {
            return claim;
        }

        throw new ForbiddenAppException("You can only access your own claims.");
    }

    private async Task<Claim> ReloadAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _store.FindClaimAsync(id, cancellationToken)
            ?? throw new NotFoundAppException("Claim not found.");
    }

    private void EnsureOfficer()
    {
        if (!_currentUser.IsClaimsOfficer)
        {
            throw new ForbiddenAppException("Only claims officers can perform this action.");
        }
    }

    private async Task PublishIfChangedAsync(Claim claim, ClaimStatus before, CancellationToken cancellationToken)
    {
        if (claim.Status != before)
        {
            await PublishStatusAsync(claim, before, cancellationToken);
        }
    }

    private Task PublishStatusAsync(Claim claim, ClaimStatus? before, CancellationToken cancellationToken)
    {
        return _events.PublishAsync(
            new ClaimStatusChanged(
                claim.Id,
                claim.ReferenceNumber,
                before?.ToString(),
                claim.Status.ToString(),
                _currentUser.UserId,
                _clock.UtcNow),
            cancellationToken);
    }

    private static ClaimSummaryDto ToSummary(Claim claim)
    {
        return new ClaimSummaryDto(
            claim.Id,
            claim.ReferenceNumber,
            claim.ClaimType.ToString(),
            claim.Status.ToString(),
            claim.IncidentDate,
            claim.IncidentLocation,
            claim.EstimatedLossAmount,
            claim.ApprovedSettlementAmount,
            claim.Claimant?.FullName ?? "Unknown",
            claim.AssignedOfficer?.FullName,
            claim.CreatedAt,
            claim.UpdatedAt);
    }

    private static ClaimDetailDto ToDetail(Claim claim)
    {
        return new ClaimDetailDto(
            claim.Id,
            claim.ReferenceNumber,
            claim.ClaimType.ToString(),
            claim.Status.ToString(),
            claim.IncidentDate,
            claim.IncidentLocation,
            claim.IncidentDescription,
            claim.EstimatedLossAmount,
            claim.ApprovedSettlementAmount,
            claim.RejectionReason,
            claim.ClaimantId,
            claim.Claimant?.FullName ?? "Unknown",
            claim.AssignedOfficerId,
            claim.AssignedOfficer?.FullName,
            claim.CreatedAt,
            claim.UpdatedAt,
            ClaimWorkflow.NextStatuses(claim.Status).Select(status => status.ToString()).ToList(),
            claim.StatusHistory
                .OrderBy(item => item.CreatedAt)
                .Select(item => new StatusHistoryDto(
                    item.Id,
                    item.FromStatus?.ToString(),
                    item.ToStatus.ToString(),
                    item.ChangedBy?.FullName ?? "Unknown",
                    item.Notes,
                    item.CreatedAt))
                .ToList(),
            claim.Assignments
                .OrderBy(item => item.CreatedAt)
                .Select(item => new AssignmentDto(
                    item.Id,
                    item.Officer?.FullName ?? "Unknown",
                    item.AssignedBy?.FullName ?? "Unknown",
                    item.Notes,
                    item.CreatedAt))
                .ToList(),
            claim.Assessments
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new AssessmentDto(
                    item.Id,
                    item.Officer?.FullName ?? "Unknown",
                    item.Notes,
                    item.CreatedAt))
                .ToList(),
            claim.InformationRequests
                .OrderBy(item => item.CreatedAt)
                .Select(item => new InformationRequestDto(
                    item.Id,
                    item.Message,
                    item.RequestedBy?.FullName ?? "Unknown",
                    item.Response,
                    item.RespondedAt,
                    item.CreatedAt))
                .ToList(),
            claim.Documents
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new DocumentDto(
                    item.Id,
                    item.FileName,
                    item.ContentType,
                    item.SizeBytes,
                    item.UploadedBy?.FullName ?? "Unknown",
                    item.CreatedAt))
                .ToList());
    }
}
