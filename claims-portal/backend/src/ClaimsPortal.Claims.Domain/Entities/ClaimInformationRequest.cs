namespace ClaimsPortal.Claims.Domain.Entities;

public sealed class ClaimInformationRequest
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public Claim Claim { get; set; } = null!;
    public Guid RequestedById { get; set; }
    public User RequestedBy { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public string? Response { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
