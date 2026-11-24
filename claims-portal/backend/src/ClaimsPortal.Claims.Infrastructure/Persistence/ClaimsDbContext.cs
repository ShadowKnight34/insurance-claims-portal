using ClaimsPortal.Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClaimsPortal.Claims.Infrastructure.Persistence;

public sealed class ClaimsDbContext : DbContext
{
    public ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimDocument> ClaimDocuments => Set<ClaimDocument>();
    public DbSet<ClaimAssessment> ClaimAssessments => Set<ClaimAssessment>();
    public DbSet<ClaimStatusHistory> ClaimStatusHistory => Set<ClaimStatusHistory>();
    public DbSet<ClaimAssignment> ClaimAssignments => Set<ClaimAssignment>();
    public DbSet<ClaimInformationRequest> ClaimInformationRequests => Set<ClaimInformationRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClaimsDbContext).Assembly);
    }
}
