using ClaimsPortal.Claims.Domain.Enums;
using ClaimsPortal.Claims.Domain.Exceptions;
using ClaimsPortal.Claims.Domain.Workflow;

namespace ClaimsPortal.Claims.Domain.Entities;

public sealed class Claim : Entity
{
    public const int DescriptionMinLength = 20;
    public const int DescriptionMaxLength = 4000;
    public const decimal MaxEstimatedLoss = 50_000_000m;

    public string ReferenceNumber { get; private set; } = string.Empty;
    public Guid ClaimantId { get; private set; }
    public User Claimant { get; set; } = null!;
    public ClaimType ClaimType { get; private set; }
    public DateOnly IncidentDate { get; private set; }
    public string IncidentLocation { get; private set; } = string.Empty;
    public string IncidentDescription { get; private set; } = string.Empty;
    public decimal EstimatedLossAmount { get; private set; }
    public decimal? ApprovedSettlementAmount { get; private set; }
    public ClaimStatus Status { get; private set; }
    public Guid? AssignedOfficerId { get; private set; }
    public User? AssignedOfficer { get; set; }
    public string? RejectionReason { get; private set; }

    public ICollection<ClaimDocument> Documents { get; set; } = new List<ClaimDocument>();
    public ICollection<ClaimAssessment> Assessments { get; set; } = new List<ClaimAssessment>();
    public ICollection<ClaimStatusHistory> StatusHistory { get; set; } = new List<ClaimStatusHistory>();
    public ICollection<ClaimAssignment> Assignments { get; set; } = new List<ClaimAssignment>();
    public ICollection<ClaimInformationRequest> InformationRequests { get; set; } = new List<ClaimInformationRequest>();

    public static Claim Submit(
        Guid claimantId,
        string referenceNumber,
        ClaimType claimType,
        DateOnly incidentDate,
        string incidentLocation,
        string incidentDescription,
        decimal estimatedLossAmount,
        DateTime utcNow)
    {
        ValidateIncident(incidentDate, incidentLocation, incidentDescription, estimatedLossAmount, utcNow);

        var claim = new Claim
        {
            Id = Guid.NewGuid(),
            ClaimantId = claimantId,
            ReferenceNumber = referenceNumber.Trim(),
            ClaimType = claimType,
            IncidentDate = incidentDate,
            IncidentLocation = incidentLocation.Trim(),
            IncidentDescription = incidentDescription.Trim(),
            EstimatedLossAmount = RoundMoney(estimatedLossAmount),
            Status = ClaimStatus.Submitted,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        claim.StatusHistory.Add(new ClaimStatusHistory
        {
            Id = Guid.NewGuid(),
            ClaimId = claim.Id,
            FromStatus = null,
            ToStatus = ClaimStatus.Submitted,
            ChangedById = claimantId,
            Notes = "Claim submitted.",
            CreatedAt = utcNow
        });

        return claim;
    }

    public void UpdateIncidentDetails(
        ClaimType claimType,
        DateOnly incidentDate,
        string incidentLocation,
        string incidentDescription,
        decimal estimatedLossAmount,
        DateTime utcNow)
    {
        if (Status != ClaimStatus.Submitted)
        {
            throw new DomainValidationException("Claim details can only be edited while the claim is Submitted.");
        }

        ValidateIncident(incidentDate, incidentLocation, incidentDescription, estimatedLossAmount, utcNow);
        ClaimType = claimType;
        IncidentDate = incidentDate;
        IncidentLocation = incidentLocation.Trim();
        IncidentDescription = incidentDescription.Trim();
        EstimatedLossAmount = RoundMoney(estimatedLossAmount);
        UpdatedAt = utcNow;
    }

    public void AssignTo(Guid officerId, Guid assignedById, string? notes, DateTime utcNow)
    {
        if (Status is ClaimStatus.Approved or ClaimStatus.Rejected or ClaimStatus.Settled)
        {
            throw new DomainValidationException("Closed claims cannot be reassigned.");
        }

        if (officerId == Guid.Empty)
        {
            throw new DomainValidationException("An officer must be selected.");
        }

        AssignedOfficerId = officerId;
        UpdatedAt = utcNow;
        Assignments.Add(new ClaimAssignment
        {
            Id = Guid.NewGuid(),
            ClaimId = Id,
            OfficerId = officerId,
            AssignedById = assignedById,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = utcNow
        });

        if (Status == ClaimStatus.Submitted)
        {
            TransitionTo(ClaimStatus.UnderReview, assignedById, "Assigned for review.", utcNow);
        }
    }

    public ClaimInformationRequest RequestInformation(Guid officerId, string message, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new DomainValidationException("Tell the claimant what information is required.");
        }

        ClaimWorkflow.EnsureTransition(Status, ClaimStatus.AdditionalInformationRequired);

        var request = new ClaimInformationRequest
        {
            Id = Guid.NewGuid(),
            ClaimId = Id,
            RequestedById = officerId,
            Message = message.Trim(),
            CreatedAt = utcNow
        };
        InformationRequests.Add(request);
        TransitionTo(ClaimStatus.AdditionalInformationRequired, officerId, message.Trim(), utcNow);
        return request;
    }

    public void RespondToInformation(Guid requestId, string response, Guid respondedById, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            throw new DomainValidationException("A response is required.");
        }

        var request = InformationRequests.FirstOrDefault(item => item.Id == requestId)
            ?? throw new DomainValidationException("That information request does not belong to this claim.");

        if (request.Response is not null)
        {
            throw new DomainValidationException("This information request has already been answered.");
        }

        ClaimWorkflow.EnsureTransition(Status, ClaimStatus.UnderReview);

        request.Response = response.Trim();
        request.RespondedAt = utcNow;
        TransitionTo(ClaimStatus.UnderReview, respondedById, "Claimant submitted additional information.", utcNow);
    }

    public void AddAssessment(Guid officerId, string notes, DateTime utcNow)
    {
        if (Status is ClaimStatus.Rejected or ClaimStatus.Settled)
        {
            throw new DomainValidationException("Assessment notes cannot be added to a closed claim.");
        }

        if (string.IsNullOrWhiteSpace(notes))
        {
            throw new DomainValidationException("Assessment notes are required.");
        }

        Assessments.Add(new ClaimAssessment
        {
            Id = Guid.NewGuid(),
            ClaimId = Id,
            OfficerId = officerId,
            Notes = notes.Trim(),
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        });
        UpdatedAt = utcNow;
    }

    public void Approve(decimal settlementAmount, Guid officerId, string? notes, DateTime utcNow)
    {
        if (settlementAmount <= 0)
        {
            throw new DomainValidationException("Approved settlement amount must be greater than zero.");
        }

        ClaimWorkflow.EnsureTransition(Status, ClaimStatus.Approved);
        ApprovedSettlementAmount = RoundMoney(settlementAmount);
        RejectionReason = null;
        TransitionTo(
            ClaimStatus.Approved,
            officerId,
            string.IsNullOrWhiteSpace(notes) ? "Claim approved." : notes.Trim(),
            utcNow);
    }

    public void Reject(string reason, Guid officerId, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException("A rejection reason is required.");
        }

        ClaimWorkflow.EnsureTransition(Status, ClaimStatus.Rejected);
        RejectionReason = reason.Trim();
        ApprovedSettlementAmount = null;
        TransitionTo(ClaimStatus.Rejected, officerId, RejectionReason, utcNow);
    }

    public void Settle(Guid officerId, DateTime utcNow)
    {
        TransitionTo(ClaimStatus.Settled, officerId, "Settlement recorded.", utcNow);
    }

    private void TransitionTo(ClaimStatus to, Guid changedById, string? notes, DateTime utcNow)
    {
        ClaimWorkflow.EnsureTransition(Status, to);
        var from = Status;
        Status = to;
        UpdatedAt = utcNow;
        StatusHistory.Add(new ClaimStatusHistory
        {
            Id = Guid.NewGuid(),
            ClaimId = Id,
            FromStatus = from,
            ToStatus = to,
            ChangedById = changedById,
            Notes = notes,
            CreatedAt = utcNow
        });
    }

    private static void ValidateIncident(
        DateOnly incidentDate,
        string incidentLocation,
        string incidentDescription,
        decimal estimatedLossAmount,
        DateTime utcNow)
    {
        var today = DateOnly.FromDateTime(utcNow);
        if (incidentDate > today)
        {
            throw new DomainValidationException("Incident date cannot be in the future.");
        }

        if (incidentDate < today.AddYears(-2))
        {
            throw new DomainValidationException("Incident date must be within the last two years.");
        }

        if (string.IsNullOrWhiteSpace(incidentLocation) || incidentLocation.Trim().Length > 200)
        {
            throw new DomainValidationException("Incident location is required and must be 200 characters or fewer.");
        }

        var description = incidentDescription?.Trim() ?? string.Empty;
        if (description.Length < DescriptionMinLength || description.Length > DescriptionMaxLength)
        {
            throw new DomainValidationException(
                $"Incident description must be between {DescriptionMinLength} and {DescriptionMaxLength} characters.");
        }

        if (estimatedLossAmount <= 0 || estimatedLossAmount > MaxEstimatedLoss)
        {
            throw new DomainValidationException("Estimated loss must be greater than zero and no more than 50,000,000.");
        }
    }

    private static decimal RoundMoney(decimal amount)
    {
        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }
}
