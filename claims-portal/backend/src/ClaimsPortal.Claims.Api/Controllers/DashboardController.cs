using ClaimsPortal.Claims.Application.Contracts;
using ClaimsPortal.Claims.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsPortal.Claims.Api.Controllers;

[ApiController]
[Authorize(Roles = "ClaimsOfficer")]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboard;

    public DashboardController(DashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<DashboardDto>> Statistics(CancellationToken cancellationToken)
    {
        return Ok(await _dashboard.GetAsync(cancellationToken));
    }

    [HttpGet("liability")]
    public async Task<ActionResult<LiabilityResponse>> Liability(CancellationToken cancellationToken)
    {
        var statistics = await _dashboard.GetAsync(cancellationToken);
        return Ok(new LiabilityResponse(statistics.OutstandingLiability, statistics.TotalApprovedSettlement));
    }

    [HttpGet("workload")]
    public async Task<ActionResult<IReadOnlyList<OfficerWorkloadDto>>> Workload(CancellationToken cancellationToken)
    {
        var statistics = await _dashboard.GetAsync(cancellationToken);
        return Ok(statistics.OfficerWorkload);
    }
}

public sealed record LiabilityResponse(decimal OutstandingLiability, decimal TotalApprovedSettlement);
