namespace ClaimsPortal.Claims.Domain.Entities;

public sealed class ClaimDocument : Entity
{
    public Guid ClaimId { get; set; }
    public Claim Claim { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public Guid UploadedById { get; set; }
    public User UploadedBy { get; set; } = null!;
}
