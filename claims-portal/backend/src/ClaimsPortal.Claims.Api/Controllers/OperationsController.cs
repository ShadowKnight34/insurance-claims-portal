using ClaimsPortal.Claims.Application.Contracts;
using ClaimsPortal.Claims.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsPortal.Claims.Api.Controllers;

[ApiController]
public sealed class OperationsController : ControllerBase
{
    private readonly AuthService _auth;

    public OperationsController(AuthService auth)
    {
        _auth = auth;
    }

    [AllowAnonymous]
    [HttpGet("api/health")]
    public IActionResult Health()
    {
        return Ok(new { status = "ok" });
    }

    [Authorize(Roles = "ClaimsOfficer")]
    [HttpGet("api/officers")]
    public async Task<ActionResult<IReadOnlyList<OfficerOptionDto>>> Officers(CancellationToken cancellationToken)
    {
        return Ok(await _auth.ListOfficersAsync(cancellationToken));
    }
}
