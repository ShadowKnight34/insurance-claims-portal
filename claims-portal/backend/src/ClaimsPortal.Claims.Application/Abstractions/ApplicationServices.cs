using ClaimsPortal.Claims.Domain.Entities;

namespace ClaimsPortal.Claims.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) Create(User user);
}

public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsClaimant { get; }
    bool IsClaimsOfficer { get; }
}

public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public interface IFileStorage
{
    Task<string> SaveAsync(Guid claimId, string originalFileName, Stream content, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken);
}

public sealed record ClaimStatusChanged(
    Guid ClaimId,
    string ReferenceNumber,
    string? FromStatus,
    string ToStatus,
    Guid ChangedById,
    DateTime OccurredAt);

public interface IClaimEventPublisher
{
    Task PublishAsync(ClaimStatusChanged change, CancellationToken cancellationToken);
}
