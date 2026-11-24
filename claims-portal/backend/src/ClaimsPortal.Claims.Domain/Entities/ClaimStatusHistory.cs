using ClaimsPortal.Claims.Domain.Enums;

namespace ClaimsPortal.Claims.Domain.Entities;

public sealed class ClaimStatusHistory
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public Claim Claim { get; set; } = null!;
    public ClaimStatus? FromStatus { get; set; }
    public ClaimStatus ToStatus { get; set; }
    public Guid ChangedById { get; set; }
    public User ChangedBy { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
