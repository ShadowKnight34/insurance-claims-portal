using ClaimsPortal.Claims.Application.Abstractions;
using ClaimsPortal.Claims.Domain.Entities;
using ClaimsPortal.Claims.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClaimsPortal.Claims.Infrastructure.Persistence;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue("DemoSeed:Enabled", false))
        {
            return;
        }

        var db = services.GetRequiredService<ClaimsDbContext>();
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DemoDataSeeder");
        var hasher = services.GetRequiredService<IPasswordHasher>();
        var now = DateTime.UtcNow;
        var users = configuration.GetSection("DemoSeed:Users").Get<List<DemoUserSeed>>() ?? [];
        if (users.Count == 0)
        {
            logger.LogWarning("Demo seed is enabled but DemoSeed:Users is empty.");
            return;
        }

        var created = new List<User>();
        foreach (var seed in users)
        {
            if (!Enum.TryParse<UserRole>(seed.Role, ignoreCase: true, out var role))
            {
                throw new InvalidOperationException($"Demo user {seed.Email} has an unknown role '{seed.Role}'.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = seed.Email.Trim().ToLowerInvariant(),
                PasswordHash = hasher.Hash(seed.Password),
                FullName = seed.FullName.Trim(),
                Role = role,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            created.Add(user);
        }

        db.Users.AddRange(created);
        await db.SaveChangesAsync(cancellationToken);

        if (configuration.GetValue("DemoSeed:SeedSampleClaims", true))
        {
            await SeedClaimsAsync(db, created, now, cancellationToken);
        }

        logger.LogInformation("Seeded {UserCount} demo users.", created.Count);
    }

    private static async Task SeedClaimsAsync(
        ClaimsDbContext db,
        IReadOnlyList<User> users,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var aisha = users.Single(user => user.Email == "claimant@claims.demo");
        var ben = users.Single(user => user.Email == "claimant2@claims.demo");
        var mei = users.Single(user => user.Email == "officer@claims.demo");
        var daniel = users.Single(user => user.Email == "officer2@claims.demo");
        var year = now.Year;
        var incident = DateOnly.FromDateTime(now.AddDays(-12));

        var submitted = Claim.Submit(
            aisha.Id,
            $"CLM-{year}-000001",
            ClaimType.Motor,
            incident,
            "PIE, Singapore",
            "Rear-end collision in slow traffic. The boot and rear bumper are damaged and the car is still drivable.",
            4800m,
            now.AddDays(-2));

        var underReview = Claim.Submit(
            aisha.Id,
            $"CLM-{year}-000002",
            ClaimType.Property,
            incident.AddDays(-3),
            "Tanjong Pagar, Singapore",
            "Water leaked through the ceiling during a storm and damaged the living room floor and a television.",
            9200m,
            now.AddDays(-6));
        underReview.AssignTo(mei.Id, mei.Id, "Picked up from the new queue.", now.AddDays(-5));
        underReview.AddAssessment(mei.Id, "Photos requested from the claimant. Initial reserve matches the estimate.", now.AddDays(-5));

        var waiting = Claim.Submit(
            ben.Id,
            $"CLM-{year}-000003",
            ClaimType.Motor,
            incident.AddDays(-8),
            "Orchard Road, Singapore",
            "A shopping-centre pillar scraped the passenger doors while the car was being parked.",
            3100m,
            now.AddDays(-9));
        waiting.AssignTo(daniel.Id, mei.Id, "Assigned to Daniel for motor assessment.", now.AddDays(-8));
        waiting.RequestInformation(daniel.Id, "Please upload the repair quotation and a photograph of the damaged doors.", now.AddDays(-7));

        var approved = Claim.Submit(
            ben.Id,
            $"CLM-{year}-000004",
            ClaimType.Property,
            incident.AddDays(-20),
            "Petaling Jaya, Malaysia",
            "A burst kitchen pipe flooded two rooms. Flooring and lower kitchen cabinets need replacement.",
            15000m,
            now.AddDays(-18));
        approved.AssignTo(mei.Id, mei.Id, "Property claim assigned.", now.AddDays(-17));
        approved.Approve(12600m, mei.Id, "Approved for the quoted repair cost, less the policy excess.", now.AddDays(-3));

        var rejected = Claim.Submit(
            aisha.Id,
            $"CLM-{year}-000005",
            ClaimType.Motor,
            incident.AddDays(-30),
            "Bangkok, Thailand",
            "The windscreen cracked overnight while the car was parked. No other vehicle was involved.",
            900m,
            now.AddDays(-28));
        rejected.AssignTo(daniel.Id, daniel.Id, "Reviewing windscreen claim.", now.AddDays(-27));
        rejected.Reject("The policy excess is higher than the loss, and this glass damage is excluded under the selected cover.", daniel.Id, now.AddDays(-20));

        var settled = Claim.Submit(
            ben.Id,
            $"CLM-{year}-000006",
            ClaimType.Property,
            incident.AddDays(-40),
            "Jakarta, Indonesia",
            "A fallen tree branch broke roof tiles and let rain into the bedroom during the last storm.",
            6400m,
            now.AddDays(-35));
        settled.AssignTo(mei.Id, mei.Id, "Storm damage claim.", now.AddDays(-34));
        settled.Approve(5800m, mei.Id, "Roof repair quotation accepted.", now.AddDays(-15));
        settled.Settle(mei.Id, now.AddDays(-4));

        db.Claims.AddRange(submitted, underReview, waiting, approved, rejected, settled);
        await db.SaveChangesAsync(cancellationToken);
    }

    private sealed class DemoUserSeed
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
