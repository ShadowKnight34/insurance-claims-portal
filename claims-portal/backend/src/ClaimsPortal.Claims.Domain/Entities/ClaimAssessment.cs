namespace ClaimsPortal.Claims.Domain.Entities;

public sealed class ClaimAssessment : Entity
{
    public Guid ClaimId { get; set; }
    public Claim Claim { get; set; } = null!;
    public Guid OfficerId { get; set; }
    public User Officer { get; set; } = null!;
    public string Notes { get; set; } = string.Empty;
}
