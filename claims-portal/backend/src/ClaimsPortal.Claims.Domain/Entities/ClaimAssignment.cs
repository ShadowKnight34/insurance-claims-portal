namespace ClaimsPortal.Claims.Domain.Entities;

public sealed class ClaimAssignment
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public Claim Claim { get; set; } = null!;
    public Guid OfficerId { get; set; }
    public User Officer { get; set; } = null!;
    public Guid AssignedById { get; set; }
    public User AssignedBy { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
