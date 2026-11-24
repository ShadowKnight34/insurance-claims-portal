using ClaimsPortal.Claims.Application.Abstractions;
using ClaimsPortal.Claims.Application.Contracts;
using ClaimsPortal.Claims.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsPortal.Claims.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/claims")]
public sealed class ClaimsController : ControllerBase
{
    private readonly ClaimService _claims;
    private readonly DocumentService _documents;

    public ClaimsController(ClaimService claims, DocumentService documents)
    {
        _claims = claims;
        _documents = documents;
    }

    [HttpPost]
    public async Task<ActionResult<ClaimDetailDto>> Create(CreateClaimRequest request, CancellationToken cancellationToken)
    {
        var claim = await _claims.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = claim.Id }, claim);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ClaimSummaryDto>>> List([FromQuery] ClaimListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _claims.SearchAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClaimDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _claims.GetAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClaimDetailDto>> Update(Guid id, UpdateClaimRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _claims.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult<ClaimDetailDto>> Assign(Guid id, AssignClaimRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _claims.AssignAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/information-requests")]
    public async Task<ActionResult<ClaimDetailDto>> RequestInformation(
        Guid id,
        InformationRequestCreate request,
        CancellationToken cancellationToken)
    {
        return Ok(await _claims.RequestInformationAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/information-requests/{requestId:guid}/respond")]
    public async Task<ActionResult<ClaimDetailDto>> Respond(
        Guid id,
        Guid requestId,
        InformationResponseRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _claims.RespondToInformationAsync(id, requestId, request, cancellationToken));
    }

    [HttpPost("{id:guid}/assessments")]
    public async Task<ActionResult<ClaimDetailDto>> Assess(Guid id, AssessmentRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _claims.AddAssessmentAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ClaimDetailDto>> Approve(Guid id, ApproveClaimRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _claims.ApproveAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ClaimDetailDto>> Reject(Guid id, RejectClaimRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _claims.RejectAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/settle")]
    public async Task<ActionResult<ClaimDetailDto>> Settle(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _claims.SettleAsync(id, cancellationToken));
    }

    [HttpGet("{id:guid}/documents")]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> Documents(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _documents.ListAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/documents")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<DocumentDto>> Upload(Guid id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { title = "Validation failed", detail = "Choose a document to upload." });
        }

        await using var stream = file.OpenReadStream();
        var document = await _documents.UploadAsync(id, file.FileName, file.ContentType, file.Length, stream, cancellationToken);
        return Ok(document);
    }

    [HttpGet("{id:guid}/documents/{documentId:guid}/content")]
    public async Task<IActionResult> Download(Guid id, Guid documentId, CancellationToken cancellationToken)
    {
        var (metadata, content) = await _documents.OpenAsync(id, documentId, cancellationToken);
        return File(content, metadata.ContentType, metadata.FileName);
    }
}
