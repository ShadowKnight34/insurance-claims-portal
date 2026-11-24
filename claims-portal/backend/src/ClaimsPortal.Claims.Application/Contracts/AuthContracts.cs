using System.ComponentModel.DataAnnotations;

namespace ClaimsPortal.Claims.Application.Contracts;

public sealed record LoginRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MinLength(8), MaxLength(100)] string Password);

public sealed record UserProfileDto(Guid Id, string Email, string FullName, string Role);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAt, UserProfileDto User);

public sealed record OfficerOptionDto(Guid Id, string FullName, string Email);
