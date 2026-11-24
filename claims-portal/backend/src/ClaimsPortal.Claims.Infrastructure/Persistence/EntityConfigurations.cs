using ClaimsPortal.Claims.Domain.Entities;
using ClaimsPortal.Claims.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsPortal.Claims.Infrastructure.Persistence;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(user => user.FullName).HasMaxLength(200).IsRequired();
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.UpdatedAt).IsRequired();
    }
}

public sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("claims");
        builder.HasKey(claim => claim.Id);
        builder.Property(claim => claim.ReferenceNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(claim => claim.ReferenceNumber).IsUnique();
        builder.Property(claim => claim.ClaimType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(claim => claim.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(claim => claim.IncidentLocation).HasMaxLength(200).IsRequired();
        builder.Property(claim => claim.IncidentDescription).HasMaxLength(4000).IsRequired();
        builder.Property(claim => claim.EstimatedLossAmount).HasPrecision(18, 2);
        builder.Property(claim => claim.ApprovedSettlementAmount).HasPrecision(18, 2);
        builder.Property(claim => claim.RejectionReason).HasMaxLength(2000);
        builder.HasIndex(claim => claim.Status);
        builder.HasIndex(claim => claim.ClaimantId);
        builder.HasIndex(claim => claim.AssignedOfficerId);
        builder.HasIndex(claim => claim.CreatedAt);

        builder.HasOne(claim => claim.Claimant)
            .WithMany()
            .HasForeignKey(claim => claim.ClaimantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(claim => claim.AssignedOfficer)
            .WithMany()
            .HasForeignKey(claim => claim.AssignedOfficerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ClaimDocumentConfiguration : IEntityTypeConfiguration<ClaimDocument>
{
    public void Configure(EntityTypeBuilder<ClaimDocument> builder)
    {
        builder.ToTable("claim_documents");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.FileName).HasMaxLength(260).IsRequired();
        builder.Property(document => document.StoredFileName).HasMaxLength(500).IsRequired();
        builder.Property(document => document.ContentType).HasMaxLength(120).IsRequired();
        builder.HasOne(document => document.Claim)
            .WithMany(claim => claim.Documents)
            .HasForeignKey(document => document.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(document => document.UploadedBy)
            .WithMany()
            .HasForeignKey(document => document.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ClaimAssessmentConfiguration : IEntityTypeConfiguration<ClaimAssessment>
{
    public void Configure(EntityTypeBuilder<ClaimAssessment> builder)
    {
        builder.ToTable("claim_assessments");
        builder.HasKey(assessment => assessment.Id);
        builder.Property(assessment => assessment.Notes).HasMaxLength(4000).IsRequired();
        builder.HasOne(assessment => assessment.Claim)
            .WithMany(claim => claim.Assessments)
            .HasForeignKey(assessment => assessment.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(assessment => assessment.Officer)
            .WithMany()
            .HasForeignKey(assessment => assessment.OfficerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ClaimStatusHistoryConfiguration : IEntityTypeConfiguration<ClaimStatusHistory>
{
    public void Configure(EntityTypeBuilder<ClaimStatusHistory> builder)
    {
        builder.ToTable("claim_status_history");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.FromStatus).HasConversion<string>().HasMaxLength(40);
        builder.Property(history => history.ToStatus).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(history => history.Notes).HasMaxLength(2000);
        builder.HasOne(history => history.Claim)
            .WithMany(claim => claim.StatusHistory)
            .HasForeignKey(history => history.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(history => history.ChangedBy)
            .WithMany()
            .HasForeignKey(history => history.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ClaimAssignmentConfiguration : IEntityTypeConfiguration<ClaimAssignment>
{
    public void Configure(EntityTypeBuilder<ClaimAssignment> builder)
    {
        builder.ToTable("claim_assignments");
        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Notes).HasMaxLength(1000);
        builder.HasOne(assignment => assignment.Claim)
            .WithMany(claim => claim.Assignments)
            .HasForeignKey(assignment => assignment.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(assignment => assignment.Officer)
            .WithMany()
            .HasForeignKey(assignment => assignment.OfficerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(assignment => assignment.AssignedBy)
            .WithMany()
            .HasForeignKey(assignment => assignment.AssignedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ClaimInformationRequestConfiguration : IEntityTypeConfiguration<ClaimInformationRequest>
{
    public void Configure(EntityTypeBuilder<ClaimInformationRequest> builder)
    {
        builder.ToTable("claim_information_requests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Message).HasMaxLength(2000).IsRequired();
        builder.Property(request => request.Response).HasMaxLength(4000);
        builder.HasOne(request => request.Claim)
            .WithMany(claim => claim.InformationRequests)
            .HasForeignKey(request => request.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(request => request.RequestedBy)
            .WithMany()
            .HasForeignKey(request => request.RequestedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
