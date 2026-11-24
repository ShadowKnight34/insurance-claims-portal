using ClaimsPortal.Claims.Application.Abstractions;
using ClaimsPortal.Claims.Application.Contracts;
using ClaimsPortal.Claims.Application.Exceptions;
using ClaimsPortal.Claims.Domain.Entities;

namespace ClaimsPortal.Claims.Application.Services;

public sealed class AuthService
{
    private readonly IClaimsStore _store;
    private readonly IPasswordHasher _passwords;
    private readonly ITokenService _tokens;
    private readonly ICurrentUser _currentUser;

    public AuthService(
        IClaimsStore store,
        IPasswordHasher passwords,
        ITokenService tokens,
        ICurrentUser currentUser)
    {
        _store = store;
        _passwords = passwords;
        _tokens = tokens;
        _currentUser = currentUser;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _store.FindUserByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null || !user.IsActive || !_passwords.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAppException("Email or password is incorrect.");
        }

        var (token, expiresAt) = _tokens.Create(user);
        return new LoginResponse(token, expiresAt, ToProfile(user));
    }

    public async Task<UserProfileDto> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var user = await _store.FindUserByIdAsync(_currentUser.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAppException("Your session is no longer valid.");
        }

        return ToProfile(user);
    }

    public async Task<IReadOnlyList<OfficerOptionDto>> ListOfficersAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsClaimsOfficer)
        {
            throw new ForbiddenAppException("Only claims officers can view the officer directory.");
        }

        var officers = await _store.ListActiveOfficersAsync(cancellationToken);
        return officers
            .Select(officer => new OfficerOptionDto(officer.Id, officer.FullName, officer.Email))
            .ToList();
    }

    public static UserProfileDto ToProfile(User user)
    {
        return new UserProfileDto(user.Id, user.Email, user.FullName, user.Role.ToString());
    }
}
