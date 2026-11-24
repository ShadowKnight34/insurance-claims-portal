using ClaimsPortal.Claims.Domain.Enums;

namespace ClaimsPortal.Claims.Domain.Entities;

public sealed class User : Entity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}
