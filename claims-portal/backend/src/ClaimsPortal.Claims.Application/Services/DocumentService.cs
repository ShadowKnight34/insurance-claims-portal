using ClaimsPortal.Claims.Application.Abstractions;
using ClaimsPortal.Claims.Application.Contracts;
using ClaimsPortal.Claims.Application.Exceptions;
using ClaimsPortal.Claims.Domain.Documents;
using ClaimsPortal.Claims.Domain.Entities;
using ClaimsPortal.Claims.Domain.Enums;

namespace ClaimsPortal.Claims.Application.Services;

public sealed class DocumentService
{
    private readonly IClaimsStore _store;
    private readonly ICurrentUser _currentUser;
    private readonly IFileStorage _files;
    private readonly IClock _clock;

    public DocumentService(IClaimsStore store, ICurrentUser currentUser, IFileStorage files, IClock clock)
    {
        _store = store;
        _currentUser = currentUser;
        _files = files;
        _clock = clock;
    }

    public async Task<DocumentDto> UploadAsync(
        Guid claimId,
        string fileName,
        string contentType,
        long sizeBytes,
        Stream content,
        CancellationToken cancellationToken)
    {
        var claim = await RequireViewableClaimAsync(claimId, cancellationToken);
        if (claim.Status is ClaimStatus.Rejected or ClaimStatus.Settled)
        {
            throw new ForbiddenAppException("Documents cannot be added to a rejected or settled claim.");
        }

        if (_currentUser.IsClaimant && claim.ClaimantId != _currentUser.UserId)
        {
            throw new ForbiddenAppException("You can only upload documents to your own claims.");
        }

        DocumentPolicy.EnsureValid(fileName, sizeBytes);
        var storedName = await _files.SaveAsync(claim.Id, fileName, content, cancellationToken);
        var now = _clock.UtcNow;
        var document = new ClaimDocument
        {
            Id = Guid.NewGuid(),
            ClaimId = claim.Id,
            FileName = Path.GetFileName(fileName),
            StoredFileName = storedName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            SizeBytes = sizeBytes,
            UploadedById = _currentUser.UserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _store.AddDocumentAsync(document, cancellationToken);
        claim.UpdatedAt = now;
        await _store.SaveChangesAsync(cancellationToken);

        var saved = await _store.FindDocumentAsync(claim.Id, document.Id, cancellationToken)
            ?? throw new NotFoundAppException("The document was saved but could not be reloaded.");
        return ToDto(saved);
    }

    public async Task<IReadOnlyList<DocumentDto>> ListAsync(Guid claimId, CancellationToken cancellationToken)
    {
        var claim = await RequireViewableClaimAsync(claimId, cancellationToken);
        return claim.Documents
            .OrderByDescending(document => document.CreatedAt)
            .Select(ToDto)
            .ToList();
    }

    public async Task<(DocumentDto Metadata, Stream Content)> OpenAsync(
        Guid claimId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        await RequireViewableClaimAsync(claimId, cancellationToken);
        var document = await _store.FindDocumentAsync(claimId, documentId, cancellationToken)
            ?? throw new NotFoundAppException("Document not found.");
        var stream = await _files.OpenReadAsync(document.StoredFileName, cancellationToken);
        return (ToDto(document), stream);
    }

    private async Task<Claim> RequireViewableClaimAsync(Guid claimId, CancellationToken cancellationToken)
    {
        var claim = await _store.FindClaimAsync(claimId, cancellationToken)
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

    private static DocumentDto ToDto(ClaimDocument document)
    {
        return new DocumentDto(
            document.Id,
            document.FileName,
            document.ContentType,
            document.SizeBytes,
            document.UploadedBy?.FullName ?? "Unknown",
            document.CreatedAt);
    }
}
