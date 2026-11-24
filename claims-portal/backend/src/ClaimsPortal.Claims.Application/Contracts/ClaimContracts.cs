using System.ComponentModel.DataAnnotations;
using ClaimsPortal.Claims.Domain.Enums;

namespace ClaimsPortal.Claims.Application.Contracts;

public sealed record CreateClaimRequest(
    [property: Required] ClaimType ClaimType,
    [property: Required] DateOnly IncidentDate,
    [property: Required, MaxLength(200)] string IncidentLocation,
    [property: Required, MinLength(20), MaxLength(4000)] string IncidentDescription,
    [property: Range(typeof(decimal), "0.01", "50000000")] decimal EstimatedLossAmount);

public sealed record UpdateClaimRequest(
    [property: Required] ClaimType ClaimType,
    [property: Required] DateOnly IncidentDate,
    [property: Required, MaxLength(200)] string IncidentLocation,
    [property: Required, MinLength(20), MaxLength(4000)] string IncidentDescription,
    [property: Range(typeof(decimal), "0.01", "50000000")] decimal EstimatedLossAmount);

public sealed record AssignClaimRequest(
    [property: Required] Guid OfficerId,
    [property: MaxLength(1000)] string? Notes);

public sealed record InformationRequestCreate(
    [property: Required, MinLength(10), MaxLength(2000)] string Message);

public sealed record InformationResponseRequest(
    [property: Required, MinLength(10), MaxLength(4000)] string Response);

public sealed record ApproveClaimRequest(
    [property: Range(typeof(decimal), "0.01", "50000000")] decimal SettlementAmount,
    [property: MaxLength(2000)] string? Notes);

public sealed record RejectClaimRequest(
    [property: Required, MinLength(10), MaxLength(2000)] string Reason);

public sealed record AssessmentRequest(
    [property: Required, MinLength(10), MaxLength(4000)] string Notes);

public sealed record ClaimSummaryDto(
    Guid Id,
    string ReferenceNumber,
    string ClaimType,
    string Status,
    DateOnly IncidentDate,
    string IncidentLocation,
    decimal EstimatedLossAmount,
    decimal? ApprovedSettlementAmount,
    string ClaimantName,
    string? AssignedOfficerName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record StatusHistoryDto(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    string ChangedByName,
    string? Notes,
    DateTime CreatedAt);

public sealed record AssignmentDto(
    Guid Id,
    string OfficerName,
    string AssignedByName,
    string? Notes,
    DateTime CreatedAt);

public sealed record AssessmentDto(
    Guid Id,
    string OfficerName,
    string Notes,
    DateTime CreatedAt);

public sealed record InformationRequestDto(
    Guid Id,
    string Message,
    string RequestedByName,
    string? Response,
    DateTime? RespondedAt,
    DateTime CreatedAt);

public sealed record DocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string UploadedByName,
    DateTime CreatedAt);

public sealed record ClaimDetailDto(
    Guid Id,
    string ReferenceNumber,
    string ClaimType,
    string Status,
    DateOnly IncidentDate,
    string IncidentLocation,
    string IncidentDescription,
    decimal EstimatedLossAmount,
    decimal? ApprovedSettlementAmount,
    string? RejectionReason,
    Guid ClaimantId,
    string ClaimantName,
    Guid? AssignedOfficerId,
    string? AssignedOfficerName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<string> AllowedNextStatuses,
    IReadOnlyList<StatusHistoryDto> StatusHistory,
    IReadOnlyList<AssignmentDto> Assignments,
    IReadOnlyList<AssessmentDto> Assessments,
    IReadOnlyList<InformationRequestDto> InformationRequests,
    IReadOnlyList<DocumentDto> Documents);

public sealed record ClaimListQuery(
    int Page = 1,
    int PageSize = 20,
    ClaimStatus? Status = null,
    ClaimType? ClaimType = null,
    string? Search = null,
    Guid? AssignedOfficerId = null,
    bool UnassignedOnly = false,
    bool AssignedToMe = false);
