using ClaimsPortal.Claims.Application.Contracts;
using ClaimsPortal.Claims.Application.Exceptions;
using ClaimsPortal.Claims.Application.Services;
using ClaimsPortal.Claims.Domain.Entities;
using ClaimsPortal.Claims.Domain.Enums;
using ClaimsPortal.Claims.Domain.Exceptions;
using ClaimsPortal.Claims.Domain.Workflow;
using ClaimsPortal.Claims.Tests.Support;

namespace ClaimsPortal.Claims.Tests;

public sealed class ClaimLifecycleTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc);
    private const string Description =
        "Rear bumper and boot lid were damaged in slow traffic. The car remains drivable.";

    [Fact]
    public void Submit_CreatesSubmittedClaimWithOpeningHistory()
    {
        var claimantId = Guid.NewGuid();
        var claim = Claim.Submit(
            claimantId,
            "CLM-2026-000001",
            ClaimType.Motor,
            new DateOnly(2026, 6, 1),
            "PIE, Singapore",
            Description,
            4800.456m,
            Now);

        Assert.Equal(ClaimStatus.Submitted, claim.Status);
        Assert.Equal(4800.46m, claim.EstimatedLossAmount);
        Assert.StartsWith("CLM-", claim.ReferenceNumber);
        var opening = Assert.Single(claim.StatusHistory);
        Assert.Null(opening.FromStatus);
        Assert.Equal(ClaimStatus.Submitted, opening.ToStatus);
        Assert.Equal(claimantId, opening.ChangedById);
        Assert.Equal(Now, opening.CreatedAt);
    }

    [Fact]
    public void Submit_RejectsFutureIncidentDate()
    {
        var error = Assert.Throws<DomainValidationException>(() => Claim.Submit(
            Guid.NewGuid(),
            "CLM-2026-000002",
            ClaimType.Property,
            new DateOnly(2026, 6, 16),
            "Orchard Road",
            Description,
            1000m,
            Now));

        Assert.Contains("future", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ClaimStatus.Submitted, ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Submitted, ClaimStatus.Rejected)]
    [InlineData(ClaimStatus.AdditionalInformationRequired, ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Rejected, ClaimStatus.UnderReview)]
    [InlineData(ClaimStatus.Settled, ClaimStatus.Approved)]
    public void Workflow_BlocksInvalidTransitions(ClaimStatus from, ClaimStatus to)
    {
        var error = Assert.Throws<InvalidClaimTransitionException>(() => ClaimWorkflow.EnsureTransition(from, to));
        Assert.Contains(from.ToString(), error.Message);
        Assert.Contains(to.ToString(), error.Message);
    }

    [Fact]
    public void Assign_MovesSubmittedClaimToUnderReviewAndRecordsAssignment()
    {
        var claim = NewSubmittedClaim();
        var officerId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();

        claim.AssignTo(officerId, assignedBy, "Queue pickup", Now.AddHours(1));

        Assert.Equal(ClaimStatus.UnderReview, claim.Status);
        Assert.Equal(officerId, claim.AssignedOfficerId);
        var assignment = Assert.Single(claim.Assignments);
        Assert.Equal(officerId, assignment.OfficerId);
        Assert.Equal(assignedBy, assignment.AssignedById);
        Assert.Contains(claim.StatusHistory, item =>
            item.FromStatus == ClaimStatus.Submitted && item.ToStatus == ClaimStatus.UnderReview);
    }

    [Fact]
    public void InformationRequest_ReturnsClaimToReviewAfterResponse()
    {
        var claim = NewSubmittedClaim();
        var officerId = Guid.NewGuid();
        claim.AssignTo(officerId, officerId, null, Now.AddHours(1));
        var request = claim.RequestInformation(officerId, "Please upload the repair quotation.", Now.AddHours(2));

        Assert.Equal(ClaimStatus.AdditionalInformationRequired, claim.Status);

        claim.RespondToInformation(request.Id, "The quotation is attached to the claim.", claim.ClaimantId, Now.AddHours(3));

        Assert.Equal(ClaimStatus.UnderReview, claim.Status);
        Assert.Equal("The quotation is attached to the claim.", request.Response);
        Assert.Equal(4, claim.StatusHistory.Count);
    }

    [Fact]
    public void Approve_RecordsSettlementAndPreservesPreviousStatus()
    {
        var claim = NewSubmittedClaim();
        var officerId = Guid.NewGuid();
        claim.AssignTo(officerId, officerId, null, Now.AddHours(1));

        claim.Approve(3200.5m, officerId, "Approved less the policy excess.", Now.AddHours(2));

        Assert.Equal(ClaimStatus.Approved, claim.Status);
        Assert.Equal(3200.50m, claim.ApprovedSettlementAmount);
        var change = claim.StatusHistory.Last();
        Assert.Equal(ClaimStatus.UnderReview, change.FromStatus);
        Assert.Equal(ClaimStatus.Approved, change.ToStatus);
        Assert.Equal(officerId, change.ChangedById);
    }

    [Fact]
    public void Reject_RequiresReasonAndClearsSettlement()
    {
        var claim = NewSubmittedClaim();
        var officerId = Guid.NewGuid();
        claim.AssignTo(officerId, officerId, null, Now.AddHours(1));

        Assert.Throws<DomainValidationException>(() => claim.Reject("  ", officerId, Now));
        claim.Reject("Glass damage is excluded under the selected cover.", officerId, Now.AddHours(2));

        Assert.Equal(ClaimStatus.Rejected, claim.Status);
        Assert.Null(claim.ApprovedSettlementAmount);
        Assert.Contains("excluded", claim.RejectionReason);
        Assert.Throws<InvalidClaimTransitionException>(() => claim.Settle(officerId, Now.AddHours(3)));
    }

    [Fact]
    public void ClosedClaim_CannotBeReassigned()
    {
        var claim = NewSubmittedClaim();
        var officerId = Guid.NewGuid();
        claim.AssignTo(officerId, officerId, null, Now.AddHours(1));
        claim.Reject("The loss is below the policy excess and is not covered.", officerId, Now.AddHours(2));

        Assert.Throws<DomainValidationException>(() =>
            claim.AssignTo(Guid.NewGuid(), officerId, null, Now.AddHours(3)));
    }

    private static Claim NewSubmittedClaim()
    {
        return Claim.Submit(
            Guid.NewGuid(),
            "CLM-2026-000010",
            ClaimType.Motor,
            new DateOnly(2026, 6, 2),
            "PIE, Singapore",
            Description,
            4800m,
            Now);
    }
}

public sealed class ClaimServiceAuthorizationTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Create_IsLimitedToClaimants()
    {
        var store = new InMemoryClaimsStore();
        var officer = store.AddUser("Mei Chen", UserRole.ClaimsOfficer);
        var service = ServiceFor(store, officer.Id, claimant: false, officer: true);

        await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            service.CreateAsync(ValidRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAndRetrieve_ReturnsOnlyTheClaimantsClaims()
    {
        var store = new InMemoryClaimsStore();
        var aisha = store.AddUser("Aisha Rahman", UserRole.Claimant);
        var ben = store.AddUser("Ben Lim", UserRole.Claimant);
        var aishaService = ServiceFor(store, aisha.Id, claimant: true, officer: false);
        var benService = ServiceFor(store, ben.Id, claimant: true, officer: false);

        var created = await aishaService.CreateAsync(ValidRequest(), CancellationToken.None);
        var aishaList = await aishaService.SearchAsync(new ClaimListQuery(), CancellationToken.None);
        var benList = await benService.SearchAsync(new ClaimListQuery(), CancellationToken.None);

        Assert.Equal("CLM-2026-000001", created.ReferenceNumber);
        Assert.Equal(ClaimStatus.Submitted.ToString(), created.Status);
        Assert.Single(aishaList.Items);
        Assert.Empty(benList.Items);
        await Assert.ThrowsAsync<ForbiddenAppException>(() => benService.GetAsync(created.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Officer_CanAssignApproveAndSeeWorkload()
    {
        var store = new InMemoryClaimsStore();
        var claimant = store.AddUser("Aisha Rahman", UserRole.Claimant);
        var officer = store.AddUser("Mei Chen", UserRole.ClaimsOfficer);
        var claimantService = ServiceFor(store, claimant.Id, claimant: true, officer: false);
        var officerService = ServiceFor(store, officer.Id, claimant: false, officer: true);
        var created = await claimantService.CreateAsync(ValidRequest(), CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            claimantService.ApproveAsync(created.Id, new ApproveClaimRequest(1000m, null), CancellationToken.None));

        var assigned = await officerService.AssignAsync(
            created.Id,
            new AssignClaimRequest(officer.Id, "Picked up from the queue."),
            CancellationToken.None);
        var approved = await officerService.ApproveAsync(
            created.Id,
            new ApproveClaimRequest(4100m, "Repair quotation accepted."),
            CancellationToken.None);

        Assert.Equal(ClaimStatus.UnderReview.ToString(), assigned.Status);
        Assert.Equal(ClaimStatus.Approved.ToString(), approved.Status);
        Assert.Equal(4100m, approved.ApprovedSettlementAmount);
        Assert.Equal(officer.FullName, approved.AssignedOfficerName);

        var dashboard = new DashboardService(store, new TestCurrentUser
        {
            UserId = officer.Id,
            IsClaimsOfficer = true
        });
        var statistics = await dashboard.GetAsync(CancellationToken.None);
        Assert.Equal(4100m, statistics.OutstandingLiability);
        Assert.Equal(4100m, statistics.TotalApprovedSettlement);
        var workload = Assert.Single(statistics.OfficerWorkload);
        Assert.Equal(1, workload.OpenClaims);
    }

    [Fact]
    public async Task Claimant_CannotViewManagementStatistics()
    {
        var store = new InMemoryClaimsStore();
        var claimant = store.AddUser("Aisha Rahman", UserRole.Claimant);
        var dashboard = new DashboardService(store, new TestCurrentUser
        {
            UserId = claimant.Id,
            IsClaimant = true
        });

        await Assert.ThrowsAsync<ForbiddenAppException>(() => dashboard.GetAsync(CancellationToken.None));
    }

    private static ClaimService ServiceFor(InMemoryClaimsStore store, Guid userId, bool claimant, bool officer)
    {
        return new ClaimService(
            store,
            new TestCurrentUser { UserId = userId, IsClaimant = claimant, IsClaimsOfficer = officer },
            new FixedClock(Now),
            new NoOpEvents());
    }

    private static CreateClaimRequest ValidRequest()
    {
        return new CreateClaimRequest(
            ClaimType.Motor,
            new DateOnly(2026, 6, 1),
            "PIE, Singapore",
            "Rear bumper and boot lid were damaged in slow traffic. The car remains drivable.",
            4800m);
    }
}
